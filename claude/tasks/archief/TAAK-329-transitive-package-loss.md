# TAAK-329 — Removing a package drops its transitive dependencies

**Status**: done (2026-10-27)
**Created**: 2026-10-26
**Goal**: a package swap never breaks code that only compiled because the removed package pulled in another package transitively.

## Context for the next agent

- Corpus salesapi (`<corpus>/corpus-epplus/src/salesapi`, first TAAK-326 tool `tool-326b`): `Domain.csproj` declares EPPlus 7.1.3 and
  has no EPPlus code. EPPlus 7 brings `Microsoft.Extensions.Configuration(.Json)` transitively. `Infrastructure` (references Domain, no
  EPPlus code) uses `IConfiguration`. After EPPlus → ClosedXML in Domain: 16 errors CS0234/CS0246 on `Microsoft.Extensions.Configuration`.
- TAAK-326 now keeps Domain on EPPlus in that repo (a kept project references it), which hides the case there; the bug is general and
  also hits a full migration (any fix, any library) where the old package's dependency closure is used directly by user code.
- Data: `obj/project.assets.json` (TAAK-319 `IPackageAssetsReader`) has the resolved closure of the old package and of the new one.

## Steps

- [x] 1. Council question: Manual row on the project, or add the lost package as a direct `PackageReference` (pinned to the resolved version)?
  **Decision (council round 16 G10)**: add it as a direct `PackageReference` pinned to the resolved version, only when the project (or a
  project referencing it) uses its types; under CPM add the version to `Directory.Packages.props` only when absent, a different version
  there → Manual. Each addition is its own Fixed row. See `claude/tasks/COUNCIL-round-16.md`.
- [x] 1b. Writer: `PackageAddEdit.KeepsTransitiveOf`; `XmlProjectFileStore` adds the central version only when absent, another version → Manual on `Directory.Packages.props`
- [x] 2. Detector: `Core/TransitivePackages.cs` (assets graph + Roslyn symbol use in the project and its dependents, assembly → package by packages-folder path); wired in `Fixer` after the fix's own project-file edits via `ProjectFileMigrations.ApplyAsync`; `Fixer(fixes, projectFiles, assetsReader)` from `Cli`
- [x] 3. `TransitivePackagesTests`, `ProjectFileEditTests.ApplyAsync_TransitivePinCentral_*`. Corpus repro: copy of salesapi in `D:/tmp329/repro` with the EPPlus code of `UploadService` stubbed (so the per-project keep does not apply): `fix --fix epplus` adds `Microsoft.Extensions.Configuration.Abstractions 8.0.0` to Domain, solution builds with 0 errors
- [x] 4. Docs (`core.md` "Lost transitive packages"), archived

## Council questions

- Q1. The new package's closure is unknown before a restore (no nuspec reader), so a pin is not checked against it. If the new package requires a higher version of a pinned package, NuGet reports NU1605 (downgrade). Conservative now: pin anyway (the exact pre-migration resolution). Option: read `<id>.nuspec` from the global packages folder via a DataAccess reader and skip/raise the pin.
- Q2. A dependent that itself declares the lost package still counts as a user, so the migrated project gets the pin (harmless, an extra reference). Should it be skipped?
- Q3. A package that reached the project through another project reference (not a package) is not seen as "kept" (the assets reader keeps package libraries only); it is then pinned too (same version, harmless).

## See also

- `claude/tasks/archief/TAAK-326-epplus-per-project.md`, `claude/tasks/TAAK-319-assets-file-reader.md`
