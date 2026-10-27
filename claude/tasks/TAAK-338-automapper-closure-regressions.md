# TAAK-338 — Regression proofs for extension callers in `TypeArgumentClosure` (council round 16 A15-A18)

**Status**: open
**Created**: 2026-10-27
**Goal**: prove that following reduced extension calls (TAAK-325) is correct for every user of `TypeArgumentClosure`, and record A16/A18 as out of scope.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-16.md` A15-A18. A15 (inline selector kept), A16 (no projection feeding) and A18 (parameters stay Manual) need no code.
- TAAK-325 changed `TypeArgumentClosure` to follow reduced extension calls. Users: `automapper.generic-map` (`GenericMapPattern`),
  `automapper.generic-destination`, `automapper.generic-profile`, `mediatr.generic-request`. Find them with a grep on `TypeArgumentClosure`.

## Steps

- [ ] 1. Proof `Proofs/AutoMapper/GenericMap/extension-callers` (after): a generic extension helper with a type-parameter destination called only in reduced form
- [ ] 2. Proof `Proofs/AutoMapper/GenericMap/extension-mixed-open` (manual `GenericMapOpenSet`): one closed static caller plus one reduced extension caller with an open type argument
- [ ] 3. The same two cases for `mediatr.generic-request` in its proofs folder
- [ ] 4. Unit test for `TypeArgumentClosure`: an extension-only caller set yields the closed type arguments
- [ ] 5. `docs/projectinfo/patterns/automapper-generic-destination-plan.md`: note A16 and A18 as out of scope with the reason; `dotnet build`, `dotnet test`; archive

## See also

- `claude/tasks/COUNCIL-round-16.md`
- `claude/tasks/TAAK-325-automapper-generic-destination.md`
- `claude/tasks/archief/TAAK-324-automapper-generic-map.md`
