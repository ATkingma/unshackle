# TAAK-246 — automapper.member-list

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 17 of `claude/tasks/automapper-queue.md`: a profile with `CreateMap<A, B>(MemberList.Destination | MemberList.Source | MemberList.None)` is migrated exactly like one with `CreateMap<A, B>()`; the argument is dropped.

## Context for the next agent

- Before this task every `CreateMap<A, B>(MemberList.X)` made its whole profile Manual: `CreateMapPattern.TryParseMap` only accepted a `CreateMap` call with zero arguments ("'...' is not a CreateMap<A, B>() without options").
- Like nested maps (TAAK-245) this is a capability of the shared profile rewrite in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, not a pattern class: the argument occurs in profiles that any of the three profile patterns (`CreateMap`, `ForMemberIgnore`, `ForMemberMapFrom`) claims. `MapCallPattern` (through `RewriteMethodsAsync`) and `NestedMaps` (which ignores arguments) need no change.
- Corpus (TAAK-244 survey clones): 267 calls in 6 projects. Forms: `CreateMap<A, B>(MemberList.Destination)` 140, `(MemberList.None)` 89, `(MemberList.Source)` 34, 4× on tuple sources in test configurations (`cfg.CreateMap<(A, B), C>(...)`, not a profile). Combined with `.ForMember(... Ignore/MapFrom/Condition)`, `.ReverseMap()`, `.MaxDepth(64)`, `.ConstructUsing(...)`, and in nested `private class Mapping : Profile` with an expression-bodied constructor (cats). Named `memberList:` and a non-constant argument do not occur in the corpus (only xipona passes it inside a generic helper `cfg.CreateMap<TSource, TDest>(MemberList.Destination)`, not a profile).

## Evidence (AutoMapper 14.0.0, `dotnet run ml.cs` in `<scratch>/amcheck-ml`, TEMP/TMP = `<temp-dir>`)

One profile with `CreateMap<Order, OrderDto>(ml).ForMember(Ignore).ForMember(MapFrom).ReverseMap()`, `CreateMap<Line, LineDto>(ml).ReverseMap()` (collection), `CreateMap<Address, AddressDto>(memberList: ml).ReverseMap()` (nested), `CreateMap<Simple, SimpleDto>(ml)`; the destination also has a flattened member (`CustomerName`), a `GetCount()` → `Count`, a public field source → property, an unmatched member with an initializer, a case-different unmatched member. Run once per `MemberList`:

| Check | Destination | Source | None |
| --- | --- | --- | --- |
| `Map<OrderDto>(src)` (JSON) | identical | identical | identical |
| `Map(src, existing)` (unmatched member keeps its value) | identical | identical | identical |
| `ReverseMap` result `Map<Order>(dto)` | identical | identical | identical |
| plain map | identical | identical | identical |
| `AssertConfigurationIsValid` | invalid: `Missing, NAME2, Only` (unmapped destination members) | invalid: `Extra` (unmapped source member) | valid |
| `TypeMap.ConfiguredMemberList` / of the reverse map | Destination / None | Source / None | None / None |

IL scan of `AutoMapper.dll` 14.0.0 (System.Reflection.Metadata, calls/field loads):

- `TypeMap.ConfiguredMemberList` is read only by `AutoMapperConfigurationException.get_Message`, `TypeMap.ShouldCheckForValid`, `TypeMap.GetUnmappedPropertyNames` and the `MappingExpression(TypeMap)` constructor (which copies it); it is set by `TypeMapConfiguration.Configure` (from the `memberList` constructor argument, its only reader of `_memberList`) and `ValidateMemberList`.
- `ShouldCheckForValid` and `GetUnmappedPropertyNames` are called only from `ConfigurationValidator.AssertConfigurationIsValid`.

So the member list never reaches the execution plan: the mapping is the same for all three values, only validation differs. `MemberList.Source` does not change which members a hand-written mapping writes. `ReverseMap()` always gets `MemberList.None`.

## Choices

- **Accepted forms**: exactly one argument, positional or named `memberList:`, of AutoMapper's enum `MemberList`, and a compile-time constant (`MemberList.X`, `AutoMapper.MemberList.X`, a cast constant). Reason: the value never changes the mapping (Evidence), so dropping it is sound.
- **Non-constant argument** (a parameter, method call, field): stays Manual with the existing reason. Dropping an arbitrary expression could drop a side effect, and the corpus has no such profile.
- **Which members are written**: unchanged; the same rule as without the argument (same-named members, Ignore, MapFrom, nested). `MemberList.None`/`Source` profiles often have unmatched destination members; those stay Manual as before ("has no member with the same name": AutoMapper may flatten or call `GetX()`), not silently left out.
- **`.ReverseMap()`**: unchanged (Manual until row 10); proof `CreateMap/member-list-reverse-map`.
- **Nested profile with an expression-bodied constructor** (the cats form): unchanged, Manual ("nested, generic, partial or has several constructors"); proof `CreateMap/member-list-nested-profile`.
- **No new pattern class / ID**: there is no node that only this capability claims; the queue ID `automapper.member-list` names the docs slug (as `automapper.nested-maps` did).

## Steps

- [x] 1. Research: current behaviour (Manual), corpus forms, AutoMapper 14 behaviour measured and IL-scanned.
- [x] 2. Implementation: `CreateMapPattern.TryParseMap` accepts one constant `MemberList` argument (`IsMemberList`).
- [x] 3. Proofs + unit tests.
- [x] 4. Plan, models, verify, README row, INDEX row, `proofs-doc.py`.
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.
- [x] 6. Queue row `done`, task to archive.

## Result

- Changed: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (`TryParseMap`, new `IsMemberList`, doc comment).
- Proofs (Fixed): `CreateMap/member-list` (Destination with an extra source member, Source, None), `CreateMap/member-list-named` (`memberList:`, `AutoMapper.MemberList.Source`), `ForMemberIgnore/member-list` (cats/neozhu form), `ForMemberMapFrom/member-list` (skoruba form, MapFrom + Ignore, named Source), `_Combined/member-list-map-call` (`Map<B>(a)` → `a.ToB()`). Proofs (Manual): `CreateMap/member-list-reverse-map`, `CreateMap/member-list-variable`, `CreateMap/member-list-nested-profile`.
- Unit tests in `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`: `Rewrite_ConstantMemberList_SameAsWithout` (5 forms, output equal to `CreateMap<A, B>()`), `Rewrite_MemberListParameter_ReportsManual`.
- Test results: see the verify document.
- Open: nested profiles and expression-bodied profile constructors (most cats/neozhu profiles) stay Manual; `MaxDepth`, `ConstructUsing`, `Condition` and `ReverseMap` next to the member list stay Manual through their own rows (15, 12, 13, 10).

## See also

- `claude/tasks/automapper-queue.md` — row 17
- `docs/projectinfo/patterns/automapper-member-list-plan.md`
- `docs/projectinfo/patterns/automapper-member-list-verify.md`
- `claude/tasks/archief/TAAK-245-automapper-nested-maps.md`
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`
