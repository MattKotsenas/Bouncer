# Bouncer

Bouncer is a PreToolUse hook that blocks dangerous tool calls in Claude Code or Copilot CLI. It uses fast regex rules for the common path and an optional LLM fallback for ambiguous cases.

## Install

```bash
dotnet tool install --global bouncer --add-source https://f.feedz.io/matt-kotsenas/bouncer/nuget/index.json
```

## Quick start

```bash
bouncer init
bouncer test bash "rm -rf /"
```

Configure your agent's PreToolUse hook to run `bouncer` (it reads the tool JSON from stdin and writes a decision to stdout).

## Plugin install (Claude Code / Copilot CLI)

Bouncer ships a plugin manifest so you can install it from a marketplace. The plugin runs `bouncer` on every tool call, so the CLI must be on your PATH.

```bash
dotnet tool install --global bouncer --add-source https://f.feedz.io/matt-kotsenas/bouncer/nuget/index.json
```

### Claude Code

```bash
claude plugin marketplace add <agent-plugins-source>
claude plugin install bouncer@agent-plugins
```

### Copilot CLI

```bash
copilot plugin marketplace add <agent-plugins-source>
copilot plugin install bouncer@agent-plugins
```

Restart the CLI after install and run `bouncer init` in each project to create `.bouncer.json`.

## Commands

- `bouncer` - hook mode (reads stdin, writes JSON, exits 0/2)
- `bouncer init` - write `.bouncer.json` with defaults
- `bouncer check` - show resolved config and provider availability
- `bouncer test <tool> <input>` - dry-run evaluation

## Configuration

Bouncer loads `.bouncer.json` from the current directory. See `.bouncer.json.example` for the default schema.

Key settings:
- `defaultAction`: what to do when no rule or LLM decision is available (`allow` or `deny`).
- `ruleGroups`: enable/disable default rule sets by category (bash, powershell, builtins, git, secrets-exposure, production-risk, web).
- `customRules`: add project-specific patterns (each with its own allow/deny action).
- `llmFallback`: enable LLM-as-judge and configure providers.
- `Logging:File:Path`: file log output path.
- `Logging:LogLevel`: standard `Microsoft.Extensions.Logging` section that controls categories (`Bouncer.Audit.Deny`, `Bouncer.Audit.Allow`).

Tier 1 includes allow rules for known-safe commands to keep routine calls out of the LLM fallback.

Default filters are Error-only everywhere, `Bouncer.Audit.Deny` at Information, and `Bouncer.Audit.Allow` disabled. Override them in the `Logging` section of `.bouncer.json` if you want allow logs or different levels.

File logs are JSON lines with these fields: `timestamp`, `level`, `category`, `message`, `state`, `scopes`, `eventId`, and `exception`.

`bouncer init` writes a fully expanded config based on the embedded example so you can edit in place.

## LLM providers

Providers are resolved from env vars first, then an optional `apiKeyCommand`:
- Anthropic: `ANTHROPIC_API_KEY`
- GitHub Models: `GITHUB_TOKEN`
- OpenAI: `OPENAI_API_KEY`
- Ollama: local endpoint `http://localhost:11434`

If `GITHUB_TOKEN` is missing and no `apiKeyCommand` is configured for GitHub Models, Bouncer falls back to `gh auth token`.

## Exit codes

- `0` - allow
- `2` - deny
