# TAAK-329 — Removing a package drops its transitive dependencies

**Status**: step 1 decided (G10), step 2 open (found by TAAK-326 corpus after-fix round, 2026-10-26)
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
- [ ] 1b. Writer: add the pinned `PackageReference` (and CPM entry) via the existing project-file writer; report row per addition
- [ ] 2. Detector: packages in the old package's closure, not in the new package's closure nor declared, whose types the project (or a project referencing it) uses
- [ ] 3. Unit tests with assets fixtures; corpus salesapi repro with the per-project rule disabled
- [ ] 4. Docs (`core.md`), archive

## See also

- `claude/tasks/archief/TAAK-326-epplus-per-project.md`, `claude/tasks/TAAK-319-assets-file-reader.md`
