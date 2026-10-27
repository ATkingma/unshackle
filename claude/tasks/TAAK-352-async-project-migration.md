# TAAK-352 — Async `IProjectFileMigration`

**Status**: open
**Created**: 2026-10-27
**Goal**: Council round 20 X8: remove sync-over-async from project-file migrations.

## Context for the next agent

- Decision: `claude/tasks/COUNCIL-round-20.md` (X8); rule `claude/docs/code-standaarden.md` §6.
- Interface: `src/Unshackle.Core/Abstractions/IProjectFileMigration.cs`; callers `src/Unshackle.Core/Fixer.cs`,
  `src/Unshackle.Core/ProjectFileMigrations.cs`; implementers in `src/Unshackle.Core/Fixes/{EPPlus,Moq,Polly,SpecFlow,XunitV3}`.
- Offender: `src/Unshackle.Core/Fixes/XunitV3/XunitV3Assemblies.cs` (`GetAwaiter().GetResult()` with `CancellationToken.None`).

## Steps

- [ ] 1. `EditsForAsync(Project, CancellationToken)` and `ProjectBlockersAsync(Project, CancellationToken)` (default: empty) replace the sync members
- [ ] 2. Move all implementers and `Fixer` to the async members, passing the run's token; `XunitV3Assemblies` awaits
- [ ] 3. Update tests and `docs/projectinfo/architectuur/core.md`; `dotnet build`, `dotnet test`; archive

## See also

- `claude/tasks/COUNCIL-round-20.md`
- `claude/tasks/TAAK-341-xunit-v3.md`
