# TAAK-352 — Async `IProjectFileMigration`

**Status**: done (2026-10-27)
**Created**: 2026-10-27
**Goal**: Council round 20 X8: remove sync-over-async from project-file migrations.

## Context for the next agent

- Decision: `claude/tasks/COUNCIL-round-20.md` (X8); rule `claude/docs/code-standaarden.md` §6.
- Interface: `src/Unshackle.Core/Abstractions/IProjectFileMigration.cs`; callers `src/Unshackle.Core/Fixer.cs`,
  `src/Unshackle.Core/ProjectFileMigrations.cs`; implementers in `src/Unshackle.Core/Fixes/{EPPlus,Moq,Polly,SpecFlow,XunitV3}`.
- Offender: `src/Unshackle.Core/Fixes/XunitV3/XunitV3Assemblies.cs` (`GetAwaiter().GetResult()` with `CancellationToken.None`).

## Steps

- [x] 1. `EditsForAsync(Project, CancellationToken)` and `ProjectBlockersAsync(Project, CancellationToken)` (default: empty) replace the sync members
- [x] 2. Move all implementers and `Fixer` to the async members, passing the run's token; `XunitV3Assemblies` awaits
- [x] 3. Update tests and `docs/projectinfo/architectuur/core.md`; `dotnet build`, `dotnet test`; archive

## Result

- `IProjectFileMigration.EditsForAsync` / `ProjectBlockersAsync` (required token, no sync overload); every implementer and `TransitivePackages` moved.
- `IFix.ProjectInfo` became `ProjectInfoAsync` too: `XunitV3Fix.ProjectInfo` needs the test-attribute check, which is now async.
- `XunitV3Assemblies.HasTestAttributesAsync` / `IsTestProjectAsync` await the syntax root and compilation with the run's token
  (cache per project kept); no `GetAwaiter().GetResult()` left. Output unchanged; full `dotnet test` green.

## See also

- `claude/tasks/COUNCIL-round-20.md`
- `claude/tasks/TAAK-341-xunit-v3.md`
