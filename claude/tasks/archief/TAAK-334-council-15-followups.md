# TAAK-334 — Council round 15 follow-ups

**Status**: done (corpus re-measure open, see step 7)
**Created**: 2026-10-27
**Goal**: implement A13 (reason text), A14 (generic profiles) and H1 (harness rule) from council round 15.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-15.md`. M14 needs no work.
- A11 machinery: `TypeArgumentClosure` and `GenericMapPattern` (TAAK-324); packability via `IProjectProperties` (TAAK-332).
- Corpus target for A14: skoruba (45 rows on `CreateMap<TRole, TRole>`); expect little delta because skoruba is packable (A13).

## Steps

- [x] 1. A13: `TypeArgumentClosure.ReachableOutside` ends the R1 reason with "Set `<IsPackable>false</IsPackable>` in <project file>
      if this library is not published" (project file name, else assembly name). Shared by `generic-map` and `generic-profile`.
      Proof `GenericMap/packable-public-base.manual.txt` and `GenericMapPatternTests` updated.
- [x] 2. A14: `TypeArgumentClosure` needed no change: `CloseByUses` already closes class type parameters (closed subclasses, `new P<X>()`,
      written type arguments such as `AddProfile<P<X>>`; `typeof(P<>)` → open). New `GenericProfilePattern` (`automapper.generic-profile`,
      code `GenericProfileOpenSet`); `CreateMapPattern` and subclasses and the `MapOwners` plan skip top-level generic profiles.
- [x] 3. A14: each closed pair goes through `CreateMapProblems.Problem` like a `CreateMap<A, B>()` without options; Fixed only when every
      pair passes (constructor emptied, `<Profile>Extensions` written).
- [x] 4. Proofs `src/Unshackle.Tests/Proofs/AutoMapper/GenericProfile/`: `closed-subclasses` (after), `open-registration` (manual),
      `packable-library` (manual, A13 text), `skoruba-shape` (moved from `CreateMap/generic`, manual, A13 text).
      Unit tests `src/Unshackle.Tests/Core/AutoMapper/GenericProfilePatternTests.cs`.
- [x] 5. KingmaDoc plan/models/verify `docs/projectinfo/patterns/automapper-generic-profile-*.md`, README row, `docs/patterns.md`;
      profile-shapes docs point to the moved proof.
- [x] 6. H1 in the in-repo harness: `claude/tools/corpus-am.sh`, `corpus-moq-am.sh`, `corpus-polly-am.sh`, `corpus-regression.sh`
      (`MIN_FREE_GB`, default 20; `INCOMPLETE.txt`; exit 255 stops `list`; work copy and, with JOBS > 1, source `obj/bin` removed),
      drivers `corpus-moq-run.sh`/`corpus-polly-run.sh` stop on exit 255. Notes in `CORPUSRUN-automapper.md` (TAAK-332 round marked
      suspect), `CORPUSRUN-moq.md`, `CORPUSRUN-polly.md`. The harness copies outside the repo (`<corpus>/corpus-automapper/tools/`,
      the TAAK-332 copy) are not changed.
- [ ] 7. `dotnet build`, `dotnet test` (3822/3822), sample tests (38/38) done; corpus re-measure (skoruba, letterbook, personalasst)
      NOT done: the corpus disk had about 3 GB free, below the H1 threshold.

## Council questions (new)

- A14-Q1: `mapper.Map<Role>(role)` on a closed pair of a generic profile still finds no map (`MapCallPattern.GeneratedAsync` and
  `NestedMaps` only index non-generic profiles), so with such a call the fix stays Manual (all-or-nothing). Index the closed pairs of
  generic profiles there too, or keep generic profiles Fixed only when no call uses their pairs?
- A14-Q2: only `CreateMap<A, B>();` without options is closed. Should options (`ForMember`, `ReverseMap`) on a generic profile be closed
  per pair through the full `CreateMapPattern` statement path?

## See also

- `claude/tasks/COUNCIL-round-15.md`
- `claude/tasks/TAAK-332-automapper-remeasure.md`
- `claude/tasks/TAAK-325-automapper-generic-destination.md`
- `claude/tasks/archief/TAAK-324-automapper-generic-map.md`
- `docs/projectinfo/patterns/automapper-generic-profile-plan.md`
