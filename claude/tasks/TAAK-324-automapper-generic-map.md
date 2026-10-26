# TAAK-324 — AutoMapper generic Map closure (`automapper.generic-map`)

**Status**: done (README row and INDEX lines pending in the main session)
**Created**: 2026-10-26
**Goal**: close `Map<T>` on a type parameter per type argument collected from callers and derived types (council round 13, A11), so one generic repository/service base no longer keeps every map on AutoMapper.

## Context for the next agent

- Decision: `claude/tasks/COUNCIL-round-13.md` A11 (R1/R2, depth 3, Manual `GenericMapOpenSet` naming the unresolved caller).
- Root A of `claude/tasks/ANALYSE-manual-round-2.md`: ~756 rows in 8 repos (bitwarden, gnomeshade, lancommander, letterbook, ombi, personalasst, skoruba, xipona).
- Follow-up: TAAK-325 (`automapper.generic-destination`, `ProjectTo<T>`, A12) uses the same closure.

## Choices

| Choice | Reason |
| ------ | ------ |
| Shared service `src/Unshackle.Core/Generics/` (`GenericUses`, `TypeArgumentClosure`) | A11 C: extract TAAK-193's collection; MediatR's `GenericRequestPattern` now uses `GenericUses.Find` |
| Separate pattern, `MapCallPattern.Find` skips generic calls | Own id and reason code; per pair the same `SingleAsync` as `map-call`, so branches call exactly what the plain call would |
| Dispatch `typeof(T) == typeof(X) ? (T)(object)… : throw` | Total over the proven set; outside it AutoMapper throws too |
| Packability guessed from the compilation | `AutoMapperFix` has no `IProjectProperties` (wiring it means touching `Cli.cs`, out of scope); errs towards Manual |
| Uses inside the generic type's own declaration count | `new C<int>()` inside `C` closes `C` too (`GenericUses.Find(..., insideDeclaration: true)`) |

## Steps

- [x] 1. Shared Core service, MediatR refactor
- [x] 2. `GenericMapPattern`, `MapCallPattern`/`MapOwners`/`AutoMapperFix` wiring
- [x] 3. Proofs (3 after, 3 manual), unit tests
- [x] 4. Docs plan/models/verify, `docs/patterns.md`
- [x] 5. Corpus re-measure (8 repos), after-build/test where they build
- [ ] 6. README row + INDEX lines (reported to the main session), archive

## See also

- `docs/projectinfo/patterns/automapper-generic-map-plan.md`
- `claude/tasks/COUNCIL-round-13.md`
- `claude/tasks/ANALYSE-manual-round-2.md`
- `claude/docs/INDEX.md`
