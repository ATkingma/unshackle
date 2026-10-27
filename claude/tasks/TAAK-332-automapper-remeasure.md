# TAAK-332 — AutoMapper corpus re-measure after `IProjectProperties` wiring

**Status**: done (cats and kavita measured later at 07552c5, see `CORPUSRUN-automapper.md`)
**Created**: 2026-10-27
**Goal**: re-measure the AutoMapper corpus with main now that `AutoMapperFix` gets `IProjectProperties` (TAAK-324 follow-up), confirm 0 test regressions, count Manual→Fixed, and close TAAK-324 step 6.

## Context for the next agent

- TAAK-324 (`automapper.generic-map`) measured 0 row delta because packability was guessed without `IProjectProperties`; `Cli.cs` now wires `XmlProjectProperties` into `AutoMapperFix`.
- Harness: `claude/tasks/CORPUSRUN-automapper.md` (Windows variant in `<corpus>/corpus-automapper/tools/corpus-am.sh`). This round uses a copy with its own result dir (`<work>/cam332/res`) and work root, so `_results/after` from TAAK-258 stays intact; base results are reused from `_results/base`.
- Env: `NUGET_PACKAGES`/`NUGET_HTTP_CACHE_PATH` on the corpus disk, TEMP/TMP on the data disk outside the user temp dir.

## Steps

- [x] 1. Publish tool from 7c0db29, run `after` on the 14 repos with a baseline (letterbook, personalasst, skoruba first)
- [x] 2. Fill the corpus table in `automapper-generic-map-verify.md`, add the round to `CORPUSRUN-automapper.md`
- [x] 3. TAAK-324 step 6: README row and INDEX line (already present), archive TAAK-324
- [x] 4. Fix regressions contained in AutoMapper code, otherwise document (none found)
- [x] 5. TAAK-325 scoped: task file `claude/tasks/TAAK-325-automapper-generic-destination.md` (not implemented)
- [x] 6. Re-measure cats and kavita (and neozhu) on a disk above the H1 threshold: 0 / 139, 0 / 169, 0 / 64, 0 regressions (07552c5)

## Results

12 of 14 repos measured (cats and kavita could not restore: the data disk ran full). **0 test regressions, Manual→Fixed 0.**
Tables in `CORPUSRUN-automapper.md` (after-fix round 7c0db29) and `automapper-generic-map-verify.md`.

- letterbook (2 rows), personalasst (1): still R1, now for the right reason: the generic member is public in a class library
  without `IsPackable`; the SDK default makes it packable. No repo in the corpus sets `IsPackable=false` on libraries.
- skoruba (45): blocker moved from R1 to a generic profile `CreateMap<TRole, TRole>` (`IdentityRepository.cs:164`); A11 does not
  close generic profiles.
- xipona (2): a closed type argument is not visible from `Xipona.Api.Core.Tests`.
- jhipster, nop (1 each): `Map<T>(object)`, destination only known at run time.
- eshoponweb 7 Fixed (was 4 at d496488), 74/74 green.

## Council questions

1. **SDK-default packability (R1).** Should a class library without explicit `IsPackable`, `PackageId` or
   `GeneratePackageOnBuild` in an application solution (letterbook, personalasst) count as not reachable from outside? Today:
   packable (SDK default), so Manual. Conservative choice kept; a heuristic (e.g. solution contains an executable that
   references the library and no project sets package metadata) would unblock letterbook/personalasst but risks breaking
   external callers of real NuGet libraries (skoruba ships its libraries as packages).
2. **Generic profiles.** skoruba's 45 rows hang on `CreateMap<TRole, TRole>` inside a generic profile. Close generic profiles
   per type argument (like A11 for `Map<T>`), or leave them Manual? Not done here (new scope).
3. **Harness on a full disk.** JOBS=2 on the corpus disk filled it; should the harness check free space before each repo and
   stop instead of producing restore/pdb failures that look like results? (Harness lives outside the repo; not changed.)

## See also

- `claude/tasks/archief/TAAK-324-automapper-generic-map.md`
- `claude/tasks/CORPUSRUN-automapper.md`
- `docs/projectinfo/patterns/automapper-generic-map-verify.md`
- `claude/tasks/COUNCIL-round-13.md` (A11, A12)
