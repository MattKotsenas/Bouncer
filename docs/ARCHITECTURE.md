# Bouncer Architecture

## The constraint
Bouncer runs as a PreToolUse hook. That means it executes as a subprocess on every tool call. A typical coding session can invoke 50-200 tools, so even small delays add up quickly. The design is driven by a single requirement: keep the common path fast enough that users do not feel it.

## Performance budget

| Phase | Target | Notes |
| --- | --- | --- |
| Process startup (AOT) | < 15ms | No JIT warmup |
| DI container build | < 3ms | Few singleton registrations |
| stdin read + JSON parse | < 2ms | System.Text.Json source gen |
| Config load + validation | < 3ms | Options + source-gen binding |
| Regex rule evaluation | < 1ms | Source-generated regex, short-circuit |
| Total (Tier 1) | < 25ms | 99% of invocations |
| LLM fallback (Tier 2) | 500-2000ms | Only when ambiguous |

## Why Native AOT
A normal .NET CLI tool often spends 150-300ms on startup (JIT, assembly loading). Native AOT compiles ahead of time, bringing cold start down to ~10-15ms. This is the single biggest performance win and the reason Bouncer is viable as a hook.

Tradeoff: no runtime reflection. This is why Bouncer relies on source generators for JSON and configuration binding.

## Current packaging: dotnet tool (framework-dependent)
Bouncer is currently distributed as a `dotnet tool` via `dotnet tool install`. This makes installation and updates trivial, but it is framework-dependent — users need the .NET 10 runtime installed, and `PublishAot` has no effect on tool packages. In practice, startup is ~150-300ms (JIT) rather than the ~15ms AOT target above.

The codebase is designed for AOT from day one (source-generated JSON, source-generated regex, no reflection). When the project moves to native AOT binary distribution (e.g. per-RID binaries on GitHub Releases), the performance budget targets above will apply with no code changes required.

Because `dotnet tool install` respects the project-level `global.json`, Bouncer should be installed as a **global tool** (`dotnet tool install --global`) to avoid conflicts with projects that pin to an older SDK version.

## Two-tier decision engine
Tier 1 is a source-generated regex rule engine. It is deterministic and sub-millisecond. Tier 2 is an LLM-as-judge for ambiguous inputs.

Tier 1 includes both deny rules and allow rules (known-safe commands). Allow rules keep routine calls from reaching Tier 2 and reduce LLM usage.

We considered a bloom filter as a Tier 0 optimization. At the current rule set size (~50-100 patterns), a bloom filter saves <0.1ms. The complexity is not worth it today. If rule sets grow to thousands, revisit.

## Why DI in a 25ms process
Dependency injection enables clean swapping of rule engines, judges, and loggers in tests. With AOT, container build cost is ~1-3ms. That tradeoff is worth the testability and composability.

Logging uses the standard `ILogger` pipeline with a JSON file logger. Logs are written to `~/.bouncer/logs/YYYY-MM-DD.log` by default (one file per day, all projects). Each audit entry includes a `Cwd` field for per-project filtering. The path is configurable via `Logging:File:Path`.

## User-level dotfiles

All configuration and logs live under `~/.bouncer/`:

```
~/.bouncer/
  config.json                              # shared config (loaded on every invocation)
  logs/
    2026-02-09.log                         # date-based audit log
    2026-02-10.log
```

`bouncer init` creates `config.json` from the embedded example.

## Default action (fail-open)
Bouncer is a safety net for catastrophic commands, not a security boundary. If Bouncer crashes, fails to parse input, or has no provider available, blocking the tool call can be worse than letting it through. The default is fail-open (`defaultAction: allow`), but it is configurable to fail-closed (`defaultAction: deny`) for CI or shared infra.

## Hook contract
- Input: JSON on stdin (format auto-detected; see Hook Adapters below)
- Output: JSON on stdout (format matches detected input format)
- Exit code: `0` allow, `2` deny
- Malformed input: handled via `defaultAction` (fail-open/closed), logged at Warning level

## Hook adapters

Claude Code and Copilot CLI send different JSON formats. Bouncer uses an adapter pattern (`IHookAdapter`) to normalize both into a canonical `HookInput` and convert `EvaluationResult` back to the correct output format.

| | Claude Code | Copilot CLI |
|---|---|---|
| Tool name | `"tool_name": "Bash"` | `"toolName": "bash"` |
| Tool args | `"tool_input": { ... }` (object) | `"toolArgs": "{...}"` (JSON string) |
| Output | `{"hookSpecificOutput":{...}}` | `{"permissionDecision":"..."}` |
| Exit code | 0 allow, 2 deny | 0 allow, 2 deny |

Auto-detection: the pipeline peeks at the parsed JSON for `"toolName"` (Copilot) vs `"tool_name"` (Claude) and selects the matching adapter. Unknown formats default to the Claude adapter.

The pipeline, rule engine, and LLM judge all operate on canonical `HookInput` — they have no knowledge of wire formats. Adding a new platform means adding one `IHookAdapter` implementation.

## Plugin hook wiring

Bouncer ships as a plugin for both Claude Code and GitHub Copilot CLI. Both support plugins with hooks, but their hook formats are incompatible — a single hooks file cannot work for both.

**Claude Code** uses PascalCase event names, matcher groups, and a `command` key. It strictly validates the hooks object and rejects the entire file if it encounters an unknown event name.

**Copilot CLI** (as of v0.0.406) uses camelCase event names, a flat hook array, and separate `bash`/`powershell` keys. It ignores unknown keys. Plugin hooks require the `--experimental` flag.

Because Claude Code rejects unknown keys, a combined file is not possible. Instead, the plugin uses two files:

| File | Format | Consumer |
| --- | --- | --- |
| `.claude-plugin/plugin.json` | Inline hooks (PascalCase, `command` key) | Claude Code |
| `hooks/hooks.json` | Separate file (camelCase, `bash`/`powershell` keys) | Copilot CLI |

Claude Code reads inline hooks from `plugin.json` and ignores `hooks/hooks.json` when inline hooks are present. Copilot CLI reads `hooks/hooks.json` and ignores inline hooks in `plugin.json`. Both files call the same shim scripts in `scripts/`.

The shims (`bouncer-hook.sh`, `bouncer-hook.ps1`) check if `bouncer` is on PATH. If found, stdin is forwarded and the exit code is passed through. If missing, the shim exits 0 (allow) and writes a warning to stderr. This lets the plugin be installed before the CLI tool without breaking sessions.

## LLM fallback strategy
A configurable provider chain is used. The first available provider wins.

Availability checks:
- Anthropic: `ANTHROPIC_API_KEY` or `apiKeyCommand`
- GitHub Models: `GITHUB_TOKEN` or `apiKeyCommand`
- OpenAI: `OPENAI_API_KEY` or `apiKeyCommand`
- Ollama: successful GET to `http://localhost:11434/api/tags`

API keys resolve via env var first, then an optional `apiKeyCommand`. This keeps secret handling outside Bouncer while enabling existing vault tooling.

ONNX/local inference is deferred to v2. The `IChatClient` abstraction keeps the door open without impacting v1.

## What Bouncer does NOT do
- It does not sandbox execution. It is a gate, not a jail.
- It does not learn or adapt. Rules and prompts are static.
- It does not replace RBAC, CI/CD protections, or code review.
