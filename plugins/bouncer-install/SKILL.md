# Bouncer Install Skill

## Goal
Install Bouncer and wire it as a PreToolUse hook for Claude Code or Copilot CLI.

## Requirements
- .NET 10 runtime

## Install
1. Install the tool:
   - `dotnet tool install --global bouncer`
   - or `dotnet tool update --global bouncer`
2. Create a default config in the current directory:
   - `bouncer init`
3. Edit `.bouncer.json` if you need to customize rule groups or the LLM fallback.

## Hook wiring
Configure your agent's PreToolUse hook to invoke `bouncer` with the tool JSON on stdin.

- Command: `bouncer`
- Exit codes: `0` allow, `2` deny
- Output: JSON containing `permissionDecision` and `permissionDecisionReason`

Claude Code and Copilot CLI both support PreToolUse hooks, but the config file location varies by version. Wire the hook command to `bouncer` in the agent's hook settings for your workspace.

## Verification
- `bouncer check` to inspect resolved configuration and available providers.
- `bouncer test bash "rm -rf /"` should deny with a Tier 1 match.
