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

## Usage

Unshackle is a `dotnet tool`. Restore your solution first (`dotnet restore`), so the library types are known.
Exit codes: `0` ok (also when some findings need manual work), `1` load or write error (or `fix` refused on a dirty
git working tree), `2` invalid arguments.

PowerShell:

```powershell
# Install from a locally built package
dotnet pack src/Unshackle.Interface -o .\nupkg
dotnet tool install --global --add-source .\nupkg Unshackle --prerelease

unshackle list
unshackle scan '.\My App\MyApp.sln' --fix fluentassertions --report report.csv
unshackle fix '.\My App\MyApp.sln' --fix fluentassertions --dry-run   # show the diff, write nothing
unshackle fix '.\My App\MyApp.sln' --fix fluentassertions --report report.md
if ($LASTEXITCODE -ne 0) { Write-Error "unshackle failed with exit code $LASTEXITCODE" }
```

bash:

```bash
dotnet pack src/Unshackle.Interface -o ./nupkg
dotnet tool install --global --add-source ./nupkg Unshackle --prerelease

unshackle list
unshackle scan "My App/MyApp.sln" --fix fluentassertions --report report.csv
unshackle fix "My App/MyApp.sln" --fix fluentassertions --dry-run
unshackle fix "My App/MyApp.sln" --fix fluentassertions --report report.json || echo "exit code $?"
```

`fix` refuses to write when the solution's git working tree has uncommitted changes; commit or stash first, or pass
`--force`. Reports: `.csv`, `.json` or `.md`, with the columns `File,Line,Fix,Pattern,Status,Reason`. Project files
(package references, `<Using Include="FluentAssertions" />`) are not migrated yet.
