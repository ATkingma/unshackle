# TAAK-343 — Transitive pin follow-ups (council round 17 T1-T3)

**Status**: open
**Created**: 2026-10-27
**Goal**: make the transitive pins of TAAK-329 predictable in the report: NU1605 hint, user project in the reason, documented limits.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-17.md` T1-T3. Background: `claude/tasks/archief/TAAK-329-transitive-package-loss.md`.
- Code: `src/Unshackle.Core/TransitivePackages.cs` (detector), `PackageAddEdit.KeepsTransitiveOf`, `ProjectFileMigrations.ApplyAsync`,
  writer `XmlProjectFileStore` (DataAccess). Tests: `TransitivePackagesTests`, `ProjectFileEditTests.ApplyAsync_TransitivePinCentral_*`.
- Docs: `docs/projectinfo/architectuur/core.md`, section "Lost transitive packages".

## Steps

- [ ] 1. Row reason for a pin: starts with "may conflict (NU1605)", names the pinned version, gives "raise the pin" as remedy, and
  "kept for `<project>`" (the project whose code uses the types; the first one if several). Pin the rendered text in a test.
- [ ] 2. Where a post-fix build/verify reports NU1605 for a pinned package, add the same hint to that output (find the step with a grep
  for `NU1605` and the verify/build code in `src/`; if no such step exists, note it here and skip).
- [ ] 3. T3: a lost package whose version differs from the one a project reference brings → Manual instead of a pin; unit test.
- [ ] 4. `core.md`: document T2 (a dependent declaring the package still counts), T3 ("kept = package libraries in assets only") and T1
  (no nuspec check; a reader is built on the first corpus NU1605, as a Core interface with a DataAccess implementation).
- [ ] 5. `dotnet build`, `dotnet test`; archive.

## See also

- `claude/tasks/COUNCIL-round-17.md`
- `claude/tasks/TAAK-319-assets-file-reader.md`
