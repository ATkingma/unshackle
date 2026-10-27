# TAAK-334 — Council round 15 follow-ups

**Status**: not started
**Created**: 2026-10-27
**Goal**: implement A13 (reason text), A14 (generic profiles) and H1 (harness rule) from council round 15.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-15.md`. M14 needs no work.
- A11 machinery: `TypeArgumentClosure` and `GenericMapPattern` (TAAK-324); packability via `IProjectProperties` (TAAK-332).
- Corpus target for A14: skoruba (45 rows on `CreateMap<TRole, TRole>`); expect little delta because skoruba is packable (A13).

## Steps

- [ ] 1. A13: append "Set `<IsPackable>false</IsPackable>` in <project> if this library is not published." to every Manual reason
      caused by R1/R2 packability. Update `*.manual.txt` proofs and unit tests.
- [ ] 2. A14: extend `TypeArgumentClosure` to class type parameters; collect closed constructions of a generic `Profile`
      (closed subclasses, `new P<X>()`, `AddProfile<P<X>>`); open use → Manual `GenericProfileOpenSet`.
- [ ] 3. A14: feed each closed pair to the existing map patterns; Fixed only when every pair is Fixed.
- [ ] 4. Proofs in `src/Unshackle.Tests/Proofs/AutoMapper/GenericProfile/`: two closed subclasses (after), open set (manual),
      packable library (manual with the A13 text).
- [ ] 5. KingmaDoc plan/models/verify for `automapper.generic-profile`, README row, `docs/patterns.md`.
- [ ] 6. H1: record the free-disk rule in the CORPUSRUN notes and apply it in the harness (outside the repo): 20 GB threshold, stop
      and mark run incomplete, clean `obj/bin` when JOBS > 1; mark full-disk results in `CORPUSRUN-automapper.md` suspect.
- [ ] 7. `dotnet build`, `dotnet test`, sample tests; corpus re-measure (skoruba, letterbook, personalasst).

## See also

- `claude/tasks/COUNCIL-round-15.md`
- `claude/tasks/TAAK-332-automapper-remeasure.md`
- `claude/tasks/TAAK-325-automapper-generic-destination.md`
- `claude/tasks/archief/TAAK-324-automapper-generic-map.md`
