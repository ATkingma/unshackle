# TAAK-267 — AutoMapper: `Map<T>` used as a method group

**Status**: open
**Created**: 2026-10-14
**Found by**: TAAK-258 after-fix round (eshoponweb, 5 test regressions)

## Problem

eShopOnWeb `src/PublicApi/*Endpoints/*ListEndpoint.cs` call `items.Select(_mapper.Map<CatalogTypeDto>)`: `Map<T>` passed as a
method group, not invoked. The fix does not see these as map calls, so it reports 1 Fixed / 0 Manual, removes all three
`CreateMap`s from `MappingProfile` and leaves the method groups in place. The code still builds (the `IMapper` is still
registered), but at runtime AutoMapper throws "missing type map" → HTTP 500:
`CatalogItemListPagedEndpoint.ReturnsFirst10CatalogItems`, `...GivenPageIndex1`, `SuccessFullMutipleParallelCall` (3 cases).

This is the worst failure mode (silent runtime break): a `Map` reference that is not an invocation must never be ignored.

## Do

- Find `IMapper.Map<T>` / `IMapperBase.Map<T>` method-group references (any `IMethodReferenceOperation` to `Map`), not only invocations.
- Rewrite a single-argument method group to `source => source.ToT()` when the source type is known (proof below), otherwise Manual.
  If that is too much for now, Manual (which then blocks the CreateMap rewrites per the all-or-nothing rule) is an acceptable first step.
- Proof: `src/Unshackle.Tests/Proofs/_Pending/AutoMapper/MapCall/method-group.{before,after}.cs`; move it to `Proofs/AutoMapper/MapCall/` when done.
- Rerun eshoponweb with `claude/tools/corpus-am.sh one after eshoponweb eShopOnWeb.sln` (base is in `<corpus>/corpus-automapper/_results/base/eshoponweb`).

## See also

- `claude/tasks/CORPUSRUN-automapper.md` (after-fix round TAAK-258)
- `claude/tasks/automapper-queue.md` (row 16)
