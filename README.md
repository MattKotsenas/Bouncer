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

## Plugin install

Bouncer ships as a plugin for both Claude Code and GitHub Copilot CLI. Installing the plugin automatically wires `bouncer` as a PreToolUse hook on every tool call. If `bouncer` isn't on your PATH, the hook degrades gracefully — tool calls are allowed with a warning on stderr.

First, install the bouncer CLI as a global tool:

```bash
dotnet tool install --global bouncer --add-source https://f.feedz.io/matt-kotsenas/bouncer/nuget/index.json
```

### Claude Code

```bash
claude plugin marketplace add <agent-plugins-source>
claude plugin install bouncer@agent-plugins
```

Restart Claude Code after install and run `bouncer init` to create `~/.bouncer/config.json`.

### Copilot CLI

As of Copilot CLI v0.0.406, plugin hooks require the `--experimental` flag.

```bash
copilot plugin install <plugin-source>
copilot --experimental  # hooks only fire with this flag
```

Alternatively, you can wire hooks manually via `.github/hooks/` in your repository:

```bash
mkdir -p .github/hooks
```

Create `.github/hooks/bouncer.json`:

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

Run `bouncer init` to create `~/.bouncer/config.json`.

## Commands

- `bouncer` - hook mode (reads stdin, writes JSON, exits 0/2)
- `bouncer init` - write `~/.bouncer/config.json` with defaults
- `bouncer check` - show resolved config and provider availability
- `bouncer test <tool> <input>` - dry-run evaluation

## Configuration

Bouncer loads `~/.bouncer/config.json` from the user's home directory. Run `bouncer init` to create it from the embedded defaults, or see `.bouncer.json.example` for the full schema.

Audit logs are written to `~/.bouncer/logs/{repo}-{hash}/audit.log`, where `{repo}` is the working directory name and `{hash}` is an 8-character SHA-256 of the absolute path. This keeps logs separated per project without ambiguity when two repos share the same name.

Key settings:
- `defaultAction`: what to do when no rule or LLM decision is available (`allow` or `deny`).
- `ruleGroups`: enable/disable default rule sets by category (bash, powershell, builtins, git, secrets-exposure, production-risk, web).
- `customRules`: add project-specific patterns (each with its own allow/deny action).
- `llmFallback`: enable LLM-as-judge and configure providers.
- `Logging:File:Path`: file log output path (default: `~/.bouncer/logs/{repo}-{hash}/audit.log`).
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
