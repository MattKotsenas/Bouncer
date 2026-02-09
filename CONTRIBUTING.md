# Contributing

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
