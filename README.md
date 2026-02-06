# Bouncer

Bouncer is a PreToolUse hook that blocks dangerous tool calls in Claude Code or Copilot CLI. It uses fast regex rules for the common path and an optional LLM fallback for ambiguous cases.

## Install

```bash
dotnet tool install --global bouncer
```

## Quick start

```bash
bouncer init
bouncer test bash "rm -rf /"
```

Configure your agent's PreToolUse hook to run `bouncer` (it reads the tool JSON from stdin and writes a decision to stdout).

## Commands

- `bouncer` - hook mode (reads stdin, writes JSON, exits 0/2)
- `bouncer init` - write `.bouncer.json` with defaults
- `bouncer check` - show resolved config and provider availability
- `bouncer test <tool> <input>` - dry-run evaluation

## Configuration

Bouncer loads `.bouncer.json` from the current directory. See `.bouncer.json.example` for the default schema.

Key settings:
- `defaultAction`: what to do when no rule or LLM decision is available (`allow` or `deny`).
- `ruleGroups`: enable/disable default rule sets.
- `customRules`: add project-specific patterns.
- `llmFallback`: enable LLM-as-judge and configure providers.
- `logging`: optional audit log.

## LLM providers

Providers are resolved from env vars first, then an optional `apiKeyCommand`:
- Anthropic: `ANTHROPIC_API_KEY`
- GitHub Models: `GITHUB_TOKEN`
- OpenAI: `OPENAI_API_KEY`
- Ollama: local endpoint `http://localhost:11434`

## Exit codes

- `0` - allow
- `2` - deny
