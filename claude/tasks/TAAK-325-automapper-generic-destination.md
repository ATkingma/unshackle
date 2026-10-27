# TAAK-325 — AutoMapper generic destination (`automapper.generic-destination`)

**Status**: not started (scoped in TAAK-332)
**Created**: 2026-10-27
**Goal**: close `ProjectTo<T>` (and `Map` with a type-parameter destination) per type argument, as council round 13 A12 decides, so generic paging/query helpers stop holding every projection on AutoMapper.

## Context for the next agent

- Decision: `claude/tasks/COUNCIL-round-13.md` A12. `Map` with a type-parameter destination already follows A11 (`GenericMapPattern`, TAAK-324). What is left is `ProjectTo<T>`.
- Rule: Fixed only when the closed set is proven (A11 closure: `TypeArgumentClosure`, R1/R2 packability via `IProjectProperties`) and each closed `T` already has a Fixed projection (`ProjectToPattern`); the generic method selects a closed `Expression<Func<S, T>>` by `typeof(T)` **before** building the query, never a type switch inside the `IQueryable` pipeline. Otherwise Manual `GenericProjectionOpenSet`.
- Today `ProjectToPattern` returns Manual "has no destination type of its own" for a generic destination (`Proofs/AutoMapper/ProjectTo/generic.*`, the cats `ProjectToPaginatedDataAsync` shape).
- Corpus expectation is low until R1 changes: TAAK-332 found every generic map in the corpus sits in a public member of an SDK-default packable library (see the council question there). Measure the row delta anyway.

## Steps

- [ ] 1. `GenericDestinationPattern` (or an extension of `ProjectToPattern`) in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/`, reusing `TypeArgumentClosure` and the per-pair projection of `ProjectToPattern`
- [ ] 2. Emitted shape: a private static `Expression<Func<S, T>>` selector chosen by `typeof(T) == typeof(X) ? (Expression<Func<S, T>>)(object)XProjection : throw`, then `query.Select(selector)`
- [ ] 3. Proofs in `src/Unshackle.Tests/Proofs/AutoMapper/GenericDestination/`: two closing callers (after), open set (manual), member without a Fixed projection (manual); move `ProjectTo/generic.*` there or update its reason
- [ ] 4. Test that the selected expression translates identically on EF Core sqlite in-memory (`ToQueryString()` equal to the closed `ProjectTo` rewrite)
- [ ] 5. KingmaDoc plan/models/verify, README row, `docs/patterns.md`
- [ ] 6. Corpus re-measure (cats, kavita, neozhu have `project-to` rows)

## See also

- `claude/tasks/COUNCIL-round-13.md` (A12)
- `claude/tasks/archief/TAAK-324-automapper-generic-map.md`
- `claude/tasks/TAAK-332-automapper-remeasure.md`
- `docs/projectinfo/patterns/automapper-project-to-plan.md`
