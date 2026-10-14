# TAAK-158 — automapper.map-into

**Status**: ✅ completed
**Created**: 2026-10-12
**Goal**: row 5 of `claude/tasks/automapper-queue.md`: `mapper.Map(src, dest)` → `src.MapInto(dest)` or Manual.

## Research

- AutoMapper 14 `IMapper`: `Map<TSource, TDestination>(TSource, TDestination)` is the map-into form; `Map<TSource, TDestination>(TSource, Action<...>)` also has two arguments (options) and does not belong to it. Recognition: the second parameter has the type of `TDestination`.
- `Map(a, b)` returns `b`; AutoMapper creates a new object if `b` is null and returns null for a null source.
- `CreateMapPattern` reported `Map(a, b)` as Manual ("map-into"); the Fixer fixes every file against the original solution, so a `MapInto` from another file does not exist yet for the semantic model.
- SampleShop has no `Map(a, b)`.

## Choices

- `MapInto` is written by `CreateMapPattern`, but only for maps that a `Map(a, b)` in the solution uses: `CreateMapPattern` became an `ISolutionPattern` (`RewriteAsync` → `MapIntoPattern.TargetsAsync`). That way all existing proofs stay the same. `Rewrite` without a solution writes no `MapInto`.
- `MapInto` comes after all `ToB` methods: `MapCallPattern.GeneratedAsync` links method i to CreateMap i (and uses the sync `Rewrite`, so it does not see `MapInto`).
- `MapInto` assigns the same members as `ToB` and returns `destination`, so a used result works.
- `MapIntoPattern` follows `MapCallPattern` (existing → generated); `GeneratedAsync`, `HasNoSideEffects`, `NeedsParentheses`, `IsCollection` became `internal` for reuse.
- Manual: a receiver with side effects, `object`/`dynamic`, collection, source or destination MaybeNull, no map, several profiles, internal in another project, an own member `MapInto`.
- Proof `CreateMap/map-into` removed (CreateMapPattern no longer finds that call); Manual proofs in `Proofs/AutoMapper/MapInto/`, Fixed with a profile in `_Combined/map-into-profile`; profile in another file: `MapIntoPatternTests`.

## Steps

- [x] 1. Research.
- [x] 2. `MapIntoPattern`, `CreateMapPattern.RewriteAsync`/MapInto, registration.
- [x] 3. Proofs, unit test, `proofs-doc.py`.
- [x] 4. Plan, models, verify, README row.

## See also

- `claude/tasks/automapper-queue.md`
- `claude/tasks/archief/TAAK-157-automapper-map-call.md`
- `docs/projectinfo/patterns/automapper-map-into-plan.md`
