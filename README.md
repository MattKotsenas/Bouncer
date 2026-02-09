# Bouncer

Bouncer is a PreToolUse hook that blocks dangerous tool calls in Claude Code or Copilot CLI. It uses fast regex rules for the common path and an optional LLM fallback for ambiguous cases.

## Install

Bouncer has two parts: a CLI tool (the decision engine) and a plugin (wires the CLI into your agent's hook system).

### 1. Install the CLI

```bash
dotnet tool install --global bouncer --add-source https://f.feedz.io/matt-kotsenas/bouncer/nuget/index.json
```

### 2. Install the plugin

#### Copilot CLI

```bash
copilot plugin install MattKotsenas/Bouncer
```

As of Copilot CLI v0.0.406, plugin hooks require the `--experimental` flag (or `"experimental": true` in `~/.copilot/config.json`).

#### Claude Code

```bash
claude plugin install MattKotsenas/Bouncer
```

#### Manual hook wiring

If you prefer not to use the plugin system, you can wire bouncer as a repo-level hook. Create `.github/hooks/bouncer.json`:

```json
{
  "version": 1,
  "hooks": {
    "preToolUse": [
      {
        "type": "command",
        "bash": "bouncer",
        "powershell": "bouncer"
      }
    ]
  }
}
```

### 3. Initialize config

```bash
bouncer init
```

This creates `~/.bouncer/config.json` with sensible defaults. See `.bouncer.json.example` for the full schema.

## Quick start

```bash
bouncer test bash "rm -rf /"    # expect: deny
bouncer test bash "echo hello"  # expect: allow
```

## Commands

- `bouncer` - hook mode (reads stdin, writes JSON, exits 0/2)
- `bouncer init` - write `~/.bouncer/config.json` with defaults
- `bouncer check` - show resolved config and provider availability
- `bouncer test <tool> <input>` - dry-run evaluation

## Configuration

Bouncer loads `~/.bouncer/config.json` from the user's home directory. Run `bouncer init` to create it from the embedded defaults, or see `.bouncer.json.example` for the full schema.

Audit logs are written to `~/.bouncer/logs/YYYY-MM-DD.log` (one file per day, all projects). Each JSON entry includes a `Cwd` field so you can filter by project:

```bash
# Show only denials for a specific project
jq 'select(.state.Cwd == "C:\\Projects\\my-app")' ~/.bouncer/logs/2026-02-09.log
```

Key settings:
- `defaultAction`: what to do when no rule or LLM decision is available (`allow` or `deny`).
- `ruleGroups`: enable/disable default rule sets by category (bash, powershell, builtins, git, secrets-exposure, production-risk, web).
- `customRules`: add project-specific patterns (each with its own allow/deny action).
- `llmFallback`: enable LLM-as-judge and configure providers.
- `Logging:File:Path`: file log output path (default: `~/.bouncer/logs/YYYY-MM-DD.log`).
- `Logging:LogLevel`: standard `Microsoft.Extensions.Logging` section that controls categories (`Bouncer.Audit.Deny`, `Bouncer.Audit.Allow`).

Tier 1 includes allow rules for known-safe commands to keep routine calls out of the LLM fallback.

Default filters are Error-only everywhere, `Bouncer.Audit.Deny` at Information, and `Bouncer.Audit.Allow` disabled. Override them in the `Logging` section of `.bouncer.json` if you want allow logs or different levels.

File logs are JSON lines with these fields: `timestamp`, `level`, `category`, `message`, `state`, `scopes`, `eventId`, and `exception`.

`bouncer init` writes a fully expanded config based on the embedded example so you can edit in place. The config file is shared across all projects.

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
