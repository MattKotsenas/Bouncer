# Contributing

## Local development

### Building and testing

```bash
dotnet build
dotnet test
```

### Installing a local build

To test your changes end-to-end inside a Copilot CLI or Claude Code session:

```bash
# Pack and install the CLI as a global tool from your local build
dotnet pack src/Bouncer/Bouncer.csproj
dotnet tool update --global bouncer --add-source artifacts/package/release

# Install the plugin from your local clone (hooks + scripts)
copilot plugin install /path/to/your/bouncer/clone    # Copilot CLI
claude plugin install /path/to/your/bouncer/clone      # Claude Code
```

After installing, start a new agent session and verify with:

```bash
bouncer --version        # confirm the CLI version matches your build
copilot plugin list      # confirm bouncer appears in the plugin list
```

## Versioning

Bouncer uses Nerdbank.GitVersioning (NBGV). The version shown to agents in `.claude-plugin/plugin.json` and the NuGet
package version are both stamped from NBGV's `SimpleVersion`, which comes directly from `version.json`.

If you need to bump the plugin version that agents see, update `version.json` and include it in your PR. Do not edit
`plugin.json` by hand; it is stamped during `dotnet pack`.

PRs that change shipping paths (src/, hooks/, .claude-plugin/, or packaging files) must include a `version.json` bump.
CI enforces this to keep the NuGet tool and plugin version in sync and to avoid the "always behind" recursion that happens
when using git height-based versions.

NBGV still calculates `VersionHeight`, but we do not use it for the plugin version. It is not the patch number unless
you explicitly reconfigure NBGV to use height-based versions.

## Plugin hooks

The plugin has two hook files that must be kept in sync — one for Claude Code and one for Copilot CLI. If you change
hook behavior, update both. See the "Plugin hook wiring" section in [ARCHITECTURE.md](docs/ARCHITECTURE.md) for why.
