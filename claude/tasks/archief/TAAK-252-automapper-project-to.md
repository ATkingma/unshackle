# TAAK-252 — automapper.project-to

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 14 of `claude/tasks/automapper-queue.md`: `query.ProjectTo<B>(mapper.ConfigurationProvider)` (189 hits / 7 projects, mostly kavita, cats, neozhu) → `query.Select(<Profile>Extensions.AToBProjection)`, an `Expression<Func<A, B>>` EF Core can translate, written next to `ToB`.

## Context for the next agent

- The projection is written by the shared profile rewrite (`CreateMapPattern.Rewrite`, parameter `projectTo`), like `MapInto`; the call rewrite is `ProjectToPattern`. `Projections` builds the expression from `ProjectionMember`s (what AutoMapper reads per member, kept next to the `ToB` values).
- The projection is a property so `MapCallPattern.GeneratedAsync` (i-th method ↔ i-th `CreateMap`) is unaffected.
- `CreateMapPattern.Find` no longer reports `ProjectTo` calls; `ProjectToPattern` reports the forms it cannot rewrite.

## Evidence (AutoMapper 14.0.0, `dotnet run proj.cs` in `<scratch>/amcheck-proj`)

| Case | AutoMapper 14 `ProjectTo` expression / result |
| --- | --- |
| same-type members | `Id = dtoOrder.Id`, also `List<int>` → `List<int>`: `Nums = dtoOrder.Nums` (no copy) |
| nested class with own map | `Customer = IIF(dtoOrder.Customer == default(Object), default(CustomerDto), new CustomerDto() {Name = dtoOrder.Customer.Name})` |
| `List<Line>` → `List<LineDto>` | `dtoOrder.Lines.Select(dtoLine => new LineDto() {Qty = dtoLine.Qty}).ToList()` (no null check) |
| `List<Line>` → `LineDto[]` | `....Select(...).ToArray()` |
| null list, run in memory | `ArgumentNullException` (also with AutoMapper) |
| `int` → `long` member | `AutoMapperMappingException: Unable to create a map expression` |

## Done

- [x] `ProjectToPattern`, `Projections`, `ProjectionMember`; `CreateMapPattern` (`Find`, `RewriteAsync`, `Rewrite`, `Problem`, `MapFromProblem`, `Extensions`); `MemberValues.TryElement` internal; registered in `AutoMapperFix`.
- [x] Proofs `ProjectTo/` (existing-projection, parameters, generic, unknown-map), `CreateMap/project-to`, `CreateMap/project-to-conversion`, `_Combined/project-to-query`; unit tests `Core/ProjectToPatternTests.cs`.
- [x] Plan/models/verify `docs/projectinfo/patterns/automapper-project-to-*`.

## Open

- Inline nested maps of other profiles (now Manual).
- The `IMapper` stays (same choice as `automapper.map-call`).

## See also

- `docs/projectinfo/patterns/automapper-project-to-plan.md`
- `claude/tasks/archief/TAAK-245-automapper-nested-maps.md`
- `claude/tasks/automapper-queue.md`
