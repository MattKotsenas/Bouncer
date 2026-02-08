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

## Two-tier decision engine
Tier 1 is a source-generated-regex rule engine. It is deterministic and sub-millisecond. Tier 2 is an LLM-as-judge for ambiguous inputs.

Tier 1 includes both deny rules and allow rules (known-safe commands). Allow rules keep routine calls from reaching Tier 2 and reduce LLM usage.

We considered a bloom filter as a Tier 0 optimization. At the current rule set size (~50-100 patterns), a bloom filter saves <0.1ms. The complexity is not worth it today. If rule sets grow to thousands, revisit.

## Why DI in a 25ms process
Dependency injection enables clean swapping of rule engines, judges, and loggers in tests. With AOT, container build cost is ~1-3ms. That tradeoff is worth the testability and composability.

## Default action (fail-open)
Bouncer is a safety net for catastrophic commands, not a security boundary. If Bouncer crashes, fails to parse input, or has no provider available, blocking the tool call can be worse than letting it through. The default is fail-open (`defaultAction: allow`), but it is configurable to fail-closed (`defaultAction: deny`) for CI or shared infra.

## Hook contract
- Input: JSON on stdin with `tool_name`, `tool_input`, and `cwd`
- Output: JSON on stdout with `permissionDecision` and `permissionDecisionReason`
- Exit code: `0` allow, `2` deny
- Malformed input: handled via `defaultAction` (fail-open/closed)

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
