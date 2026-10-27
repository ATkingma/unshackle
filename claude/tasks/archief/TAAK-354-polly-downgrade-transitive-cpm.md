# TAAK-354 — Polly package downgrade across project references and central pins

**Status**: done
**Created**: 2026-10-27
**Goal**: Fix Polly corpus bugs B5 and B6 (`claude/tasks/CORPUSRUN-polly.md`, round 3): the `PackageDowngrade` blocker also looks at
projects that get Microsoft.Extensions.Resilience through `ProjectReference`, and at the project's declared (central) versions.

## Context for the next agent

- Code: `src/Unshackle.Core/Fixes/Polly/PollyPackageDowngrade.cs`, `PollyFix.cs` (new constructor with `IPackageAssetsReader`),
  `src/Unshackle.Interface/Cli.cs`. Tests: `src/Unshackle.Tests/Core/Polly/PollyPackageDowngradeTests.cs`.
- Conservative outcome as in council B1/B4: Manual `PackageDowngrade` blocker, no version bump. `IProjectFileMigration` is unchanged (TAAK-352).

## Steps

- [x] 1. B5: a Polly v7 project is blocked when any project that references it (transitively) resolves an older Microsoft.Extensions.* package;
  the reason names each consumer and its packages (unit test: babylon-gateway shape)
- [x] 2. B6: with current assets, the project's own requirements (under CPM the `PackageVersion` pins) count too, so a pin that binds from a
  framework reference pack is still seen (unit test: listenarr shape; stale assets are ignored)
- [x] 3. Investigate the babylon-gateway "project-file edits not applied" row for `RadixDlt.CoreApiSdk.csproj`
- [x] 4. `dotnet build`, `dotnet test`, verify-head; archive

## Result

- Reproduced babylon-gateway with the r3 tool on a copy: `RadixDlt.CoreApiSdk.csproj` gets `<PackageReference Include="Microsoft.Extensions.Resilience" />`
  although its row says "Project-file edits not applied". Cause: two edit sources. That row belongs to `PollyFix.EditsFor`
  (`IProjectFileMigration`), which the manual findings blocked; the package migration, by design, keeps Polly in a project with manual
  findings and adds the new package next to it ("covered by its kept row", `Fixer.MigrateProjectFilesAsync`). Not a logic bug, but the kept row
  does not say that the new package was added. With the B5 fix the project is blocked by `PackageDowngrade` (its consumer
  `RadixDlt.NetworkGateway.Abstractions` resolves 8.0.x), so nothing is added; restore of the fixed copy succeeds.
- Known limit: with `CentralPackageTransitivePinningEnabled`, a pin of a package the project does not reference directly is not in the
  project's requirements; not seen in the corpus.

## Council questions (decided in council round 22, `claude/tasks/COUNCIL-round-22.md`)

1. The kept row of a project with manual findings ("Polly is still used ... stay") does not mention the new package added next to the old one,
   which made the CoreApiSdk rows look contradictory. Add "; Microsoft.Extensions.Resilience was added next to it" to that row?
   **Decision (council 22 P11: yes, only when the package was actually added, via the shared `Fixer` text; TAAK-355.)**

## See also

- `claude/tasks/CORPUSRUN-polly.md` (B5, B6)
- `claude/docs/council.md` (Polly B1/B4 decisions)
- `claude/tasks/TAAK-355-polly-kept-row-added-package.md` — P11 follow-up
