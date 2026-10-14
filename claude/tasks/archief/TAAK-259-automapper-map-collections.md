# TAAK-259 — automapper.map-call: collections and maybe-null sources

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: the two largest Manual groups of `automapper.map-call` in the corpus scan: "maps a collection" (129 hits) and "source of Map may be null" (54 hits) become rewrites where AutoMapper 14's behaviour can be written exactly.

## Evidence (AutoMapper 14.0.0, file-based `dotnet run` in a scratch folder)

| Case | Result |
| --- | --- |
| `Map<List<B>>(null)` | empty `List<B>` |
| `Map<B[]>(null)` | empty array |
| `Map<IEnumerable<B>>(null)`, `Map<IList<B>>(null)` | empty `List<B>` |
| `Map<IEnumerable<B>>(items)`, `Map<IReadOnlyList<B>>(items)` | a `List<B>` |
| `Map<List<B>>([null, a])` | 2 elements, the first null |
| `Map<B>(null)`, `Map<A, B>(null)` | null |

The same as nested collection members (TAAK-245), so `MapCallPattern.Collection` reuses `MemberValues.Problem`.

## Choices

- **Collections**: `items?.Select(x => x?.ToB()).ToList() ?? []` (`ToArray` for arrays, `new List<B>()` / `Array.Empty` before C# 12). When the nullable flow says the source is not null the `?.` and `?? []` go; when the elements are declared non-null (nullable context on) `x?.` and the `!` go. Parentheses when the result is used further (`(…).Count`). Element maps come from `NestedMaps` (profiles and rewritten `…Extensions` classes); a hand-written `ToB` elsewhere is not used for collections.
- **Maybe-null source**: `a?.ToB()` only where the target is declared nullable: `Map<B?>`, a `return`/expression body of a method or property returning `B?` (async `Task<B?>` unwrapped), an explicitly typed local, a field, property or assignment of type `B?`. A `var` local stays Manual (it would turn `B?` and warn at its uses); a struct destination stays Manual. No `!` and no new `?` annotations are written.
- Collection proofs live in `_Combined` because they need the profile rewrite of the same file.

## Steps

- [x] Measure AutoMapper 14 behaviour
- [x] `MapCallPattern`: collection rewrite and conditional call (`Kind`, `TargetAllowsNull`, `Collection`)
- [x] Proofs `_Combined/collection-map-call`, `_Combined/collection-nullable-map-call`, `MapCall/maybe-null-target`, `MapCall/maybe-null` (var: Manual)
- [x] Unit tests in `MapCallPatternTests`
- [x] Plan/verify REQ-9, REQ-10

## See also

- [automapper-map-call-plan.md](../../../docs/projectinfo/patterns/automapper-map-call-plan.md)
- [TAAK-245](TAAK-245-automapper-nested-maps.md)
