# Unshackle

Migrate .NET code away from libraries that went commercial (FluentAssertions, MediatR, AutoMapper).
Roslyn-powered, with a report and dry-run before anything changes.

> Work in progress. See [docs/projectinfo](docs/projectinfo/README.md).

## Projects

| Project | Role |
|---|---|
| `src/Unshackle.Interface` | CLI (`unshackle fix`, `scan`, `list`), console output |
| `src/Unshackle.Core` | The `Fixer`, fixes per library, patterns per fix, contracts |
| `src/Unshackle.DataAccess` | Loading/saving solutions (MSBuild), report writers (CSV, JSON, Markdown) |
| `src/Unshackle.Tests` | Unit tests, proofs (`*.before.cs` / `*.after.cs`) per pattern, and `Integration/` tests on real projects in `samples/` |

Dependencies: `Interface → Core ← DataAccess`, `Interface → DataAccess`. Core depends on none of the others.

## Issues and requests

Missing a pattern, want another library supported, or got a wrong rewrite?
[Open an issue](https://github.com/ATkingma/unshackle/issues/new/choose) with one of the forms.
[docs/issues.md](docs/issues.md) explains what to include.
