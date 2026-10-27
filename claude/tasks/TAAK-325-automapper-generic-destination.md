# TAAK-325 — AutoMapper generic destination (`automapper.generic-destination`)

**Status**: steps 1-5 done (branch `taak-325`); step 6 corpus re-measure open
**Created**: 2026-10-27
**Goal**: close `ProjectTo<T>` (and `Map` with a type-parameter destination) per type argument, as council round 13 A12 decides, so generic paging/query helpers stop holding every projection on AutoMapper.

## Context for the next agent

- Decision: `claude/tasks/COUNCIL-round-13.md` A12. `Map` with a type-parameter destination already follows A11 (`GenericMapPattern`, TAAK-324). What is left is `ProjectTo<T>`.
- Rule: Fixed only when the closed set is proven (A11 closure: `TypeArgumentClosure`, R1/R2 packability via `IProjectProperties`) and each closed `T` already has a Fixed projection (`ProjectToPattern`); the generic method selects a closed `Expression<Func<S, T>>` by `typeof(T)` **before** building the query, never a type switch inside the `IQueryable` pipeline. Otherwise Manual `GenericProjectionOpenSet`.
- Corpus expectation is low until R1 changes: TAAK-332 found every generic map in the corpus sits in a public member of an SDK-default packable library (see the council question there). Measure the row delta anyway.

## Choices

| Choice | Reason |
| ------ | ------ |
| Separate `GenericDestinationPattern`; `ProjectToPattern.Find` skips generic destinations | Own id and reason code, like `generic-map` next to `map-call` |
| `ProjectToPattern.ProjectionAsync` extracted from `RewriteAsync` | Both patterns pick the same projection per pair (A12 "already has a Fixed projection") |
| `requireTarget: true` for the generic pattern | Only a projection the code has, or one the profile writes because a non-generic `ProjectTo` of the pair exists, counts; no feedback from the closure into `TargetsAsync` (conservative) |
| Inline conditional selector inside `Select(...)` | Evaluated in C# before `Queryable.Select`, so outside the query (A12); no member insertion needed. Translation proven equal on SQLite |
| `ProjectTo<T>` with parameters / members to expand → Manual | Not measured over a type parameter (conservative) |
| `TypeArgumentClosure` follows reduced extension calls | Their id did not match the declaration's, so callers like `orders.ProjectToPage<Order, OrderDto>(...)` were skipped silently (latent A11 soundness gap); the constructed unreduced method has the same type arguments |
| EF Core Sqlite 10.0.13 as test package | Step 4 translation test; no license check |

## Steps

- [x] 1. `GenericDestinationPattern` in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/`, reusing `TypeArgumentClosure` and `ProjectToPattern.ProjectionAsync`; wired in `AutoMapperFix` and the `MapOwners` plan
- [x] 2. Emitted shape: `query.Select(typeof(T) == typeof(X) ? (Expression<Func<S, T>>)(object)XProjection : … : throw)` (inline, see Choices)
- [x] 3. Proofs in `src/Unshackle.Tests/Proofs/AutoMapper/GenericDestination/`: `two-callers`, `paging-helper` (after); `packable-paging-helper` (moved from `ProjectTo/generic`), `no-projection`, `parameters` (moved from `ProjectTo/parameters-generic`) (manual); unit tests `GenericDestinationPatternTests`
- [x] 4. `GenericDestinationTranslationTests`: `ToQueryString()` of the selector equals the closed rewrite on EF Core SQLite in-memory
- [x] 5. KingmaDoc plan/models/verify, README row, `docs/patterns.md`
- [ ] 6. Corpus re-measure (cats, kavita, neozhu have `project-to` rows)

## Council questions

1. **Selector as an inline conditional instead of a private static member** (task step 2 suggested a member). Same evaluation order (outside the query); chosen because a rewrite of one invocation cannot add a member. Keep, or require a named member for readability?
2. **Should a generic `ProjectTo<T>` make the profile write projections for its closed pairs?** Now only pairs that already have a projection (existing, or written for a non-generic `ProjectTo`) count; others make the call Manual. Feeding closed pairs into `ProjectToPattern.TargetsAsync` would lift more rows but couples the closure to the profile rewrite.
3. **Reduced extension calls in `TypeArgumentClosure`** were silently skipped (so an extension-only caller set was "no caller", and a mixed set could miss a caller — a soundness gap in A11). Now followed like static calls. Confirm this is acceptable for `generic-map` and `mediatr.generic-request` too.
4. **`ProjectTo<T>` with parameters over a type parameter** (Kavita `ProjectToWithProgress`) stays Manual. Worth a follow-up?

## See also

- `claude/tasks/COUNCIL-round-13.md` (A12)
- `claude/tasks/archief/TAAK-324-automapper-generic-map.md`
- `claude/tasks/TAAK-332-automapper-remeasure.md`
- `docs/projectinfo/patterns/automapper-generic-destination-plan.md`
- `docs/projectinfo/patterns/automapper-project-to-plan.md`
