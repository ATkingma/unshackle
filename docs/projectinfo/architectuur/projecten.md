# Projects and dependencies

## Projects

```
src/
├─ Unshackle.Interface/     ← what the user works with (CLI: fix, scan, list), console output
├─ Unshackle.Core/          ← main project: the Fixer, fixes per library, patterns per fix, contracts
├─ Unshackle.DataAccess/    ← everything that reads or writes: solutions (MSBuild), reports (CSV/JSON/Markdown)
└─ Unshackle.Tests/         ← all tests: unit, proofs per pattern, Integration/ (samples/)
```

## Dependencies

```
Interface ──► Core ◄── DataAccess
    └─────────────────────►┘
```
- **Core knows no other project.** Whatever Core needs from outside, Core defines as an
  interface (`ISolutionStore`, `IReportWriter`).
- **DataAccess implements those interfaces.**
- **Interface wires everything together** in `Cli.cs` (composition root; `Program.cs` only calls it). A second interface, such as
  web or a VS Code extension, can be added later without changing Core or DataAccess.

## Build settings

- .NET 10 (`global.json`), solution in `.slnx`
- `Directory.Build.props`: nullable on, warnings count as errors, analyzers at `latest-recommended`
- `Directory.Packages.props`: all package versions in one place
- Interface is published as a `dotnet tool` with the command `unshackle`

## See also

- `docs/projectinfo/README.md` — overview and C4 diagrams
- `docs/projectinfo/architectuur/core.md` — inside Core
