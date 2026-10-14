# TAAK-249 — automapper.reverse-map

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 10 of `claude/tasks/automapper-queue.md`: a profile with `CreateMap<A, B>().ReverseMap()` gets, next to `B ToB(this A source)`, an `A ToA(this B source)` that does exactly what AutoMapper 14's reverse map does; unflattening and anything unclear stays Manual with a precise reason.

## Context for the next agent

- Before this task every `.ReverseMap()` made its whole profile Manual ("is not a CreateMap<A, B>() without options").
- Like nested maps (TAAK-245) and the member list (TAAK-246) this is a capability of the shared profile rewrite in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, not a pattern class. Two other places count generated methods and needed the reverse pair: `NestedMaps.AddProfiles` (a member can call `ToA`) and `MapCallPattern.GeneratedAsync` (pairs the i-th method with the i-th map).
- Corpus (TAAK-244 survey clones): 186 `ReverseMap()` in 10 projects. Forms: plain `CreateMap<A, B>().ReverseMap();` (bitwarden ~60, ombi 37, jhipster 10, neozhu/cats in nested profiles), with `MemberList` (skoruba, cats), forward `ForMember(... Ignore())` before it (bitwarden `AccessPolicy`), `ReverseMap().ForMember(... MapFrom(src => src.OrganizationUser.User))` (bitwarden), `ReverseMap().ForMember(... MapFrom(src => src))` on a `CreateMap<X, string>().ConstructUsing(...)` (skoruba), `ReverseMap().ForPath(...)` (jhipster `User`), `ReverseMap().ConstructUsing(...)` after `MaxDepth` (letterbook), `ReverseMap().ForMember(... MapFrom((dest, src) => ...))` (cats).

## Evidence (AutoMapper 14.0.0, `dotnet run rev.cs` in `<scratch>/amcheck-rev`, TEMP/TMP = `<temp-dir>`)

Each line maps with one profile; JSON of the result.

| # | Configuration | Result `Map<A>(b)` (or as noted) |
| --- | --- | --- |
| 1 | `CreateMap<Simple, SimpleDto>().ReverseMap()` | same-named members; `OnlySimple` (no source) keeps its initializer `"init"` |
| 1b | same, `SimpleDto.lower` vs `Simple.Lower` | `Lower = "low"`: case-insensitive match |
| 2, 17 | forward `ForMember(d => d.Note, Ignore())`, both have `Note` | reverse still writes `Note` (forward Ignore does not carry over) |
| 2 | forward Ignore of `Address`/`Lines` (no reverse map for them) | reverse throws "Missing type map AddressDto -> Address": a forward Ignore is not an Ignore in reverse |
| 3, 13 | forward `MapFrom(d.Title ← s.Name)`, `OrderDto` also has `Name` | reverse `Name = dto.Title` (reversed MapFrom wins over same name), `Title = dto.Title` |
| 3b, 18 | forward `MapFrom(d.Label ← s.Extra)` | reverse `Extra = dto.Label`; also into an existing object |
| 4, r7 | forward `MapFrom(d.Label ← s.Customer.Name)` | reverse creates `Customer` and sets `Customer.Name = dto.Label` (unflattening) |
| 4c, r3 | forward `MapFrom(s.Name + "!")`, `MapFrom(s.Other.ToUpper())` | not reversed |
| 5, u2, u6, u8 | forward flattening `CustomerName ← Customer.Name` (also `CustomerId`, three levels) | reverse unflattens: creates `Customer` (and `Customer.Address`) and sets the leaf |
| u1, u7 | forward `Ignore()` on the flattened member | not unflattened |
| u5 | `B.CustomerId` matched by name to `A.CustomerId`, `A.Customer.Id` exists | no unflattening (`Customer` stays null) |
| u2 (`NameLength`) | `B.NameLength`, `A.Name` string | reverse sets `Name = ""` (unflattening into a string) |
| u3 | `B.GetCode` property, `A.Code` | `Code = B.GetCode` (`Get` prefix); `B.CustomerX` does not touch `A.Customer` (string) |
| u4 | `B.GetCode()` method, `A.Code` | `Code = "fromMethod"` |
| 5c, 10, 10b | `Map(dto, existing)` | unmapped members keep their value; nested object mapped into the existing one (same reference) |
| 6, 6b | `ReverseMap().ForMember(d => d.B, Ignore())` | reverse leaves `B` alone; forward unaffected |
| 7, 7b, 19 | `ReverseMap().ForMember(d => d.B, MapFrom(s => s.OnlyDto))`, `MapFrom(s => s.A.ToString())` | reverse uses it; forward unaffected |
| r1, r2 | forward `MapFrom(d.Note ← s.Other)` + `ReverseMap().ForMember(d => d.Other, Ignore()/MapFrom(...))` | the reverse option wins |
| r4 | reversed MapFrom into a get-only member | skipped, no error |
| r5 | reversed MapFrom into a field | written |
| r6 | forward `MapFrom(d.Inner ← s)` (whole source) | reverse maps `Inner` into the destination itself |
| 8, 8c | nested `Address` with `CreateMap<Address, AddressDto>().ReverseMap()` | reverse maps `Address` through the reverse map; null → null |
| 8b | nested without `ReverseMap` on the inner map | reverse throws "Missing type map" |
| 9, 9b | `List<Line>` with reverse inner map | copied element by element; null → `[]` |
| 11 | forward Ignore of a member `A` does not have | no effect |
| 12 | `CreateMap<A, B>(MemberList.Destination).ReverseMap()` | reverse `ConfiguredMemberList = None` (as TAAK-246) |
| 14 | `int? → int` in reverse | null gives `0` (conversion, row 11) |
| 16 | `ReverseMap()` and an own `CreateMap<SimpleDto, Simple>().ForMember(B, Ignore())` | the own map wins (`B` null) |

So: the reverse map is an ordinary map `B → A` with `MemberList.None`, plus the reversal of the forward map's member paths (MapFrom paths and flattened members). Forward `Ignore()` only stops unflattening.

## Choices

- **What is rewritten**: per member of `A`, in AutoMapper's order: a reverse `ForMember` option (Ignore/MapFrom, same rules as forward); a reversed one-member `MapFrom(s => s.X)` for `Y` → `X = source.Y`; a same-named member (also nested/list through maps, including reverse maps); else left out when nothing in `B` could feed it. Reason: these are exactly the measured cases 1, 3, 6, 7, 8, 9, 13, 17, r1, r2.
- **A member without source left out** (only in reverse): the reverse map has `MemberList.None` and AutoMapper leaves the member alone (cases 1, 10, 2 `Extra`). Forward this stays Manual as before, because there the corpus profiles validate the configuration and the case was not part of this row. Before leaving a member out, `MayFlatten` checks `B` for another case, a `Get` prefix, a parameterless method or a flattening prefix (cases 1b, u3, u4); any hit → Manual.
- **Unflattening → Manual**: forward `MapFrom` path of 2+ members or `s => s` (cases 4, r6, r7), and any `B` member that is not `Ignore()`d forward, has no same-named `A` member and starts with an `A` member name (cases 5, u1, u2, u5, u6, u8). Because the forward rewrite only writes same-named members, that check only fires for members the forward map leaves alone (get-only); conservative by design. A cast in a forward `MapFrom` → Manual (not measured).
- **Reversed MapFrom into a get-only member, or two MapFroms reading one member** → Manual (r4 skips silently; keeping it Manual is simpler and safe).
- **Other options after `ReverseMap()`** (`ForPath`, `ConstructUsing`, ...) → Manual as before; own rows (13, 12).
- **Two maps for one pair in one profile** (`ReverseMap` + `CreateMap<B, A>`) → Manual: AutoMapper takes the later one (case 16) and two `ToA(this B)` methods would not compile.
- **`MapInto`**: a `Map(b, a)` call gets `MapInto(this B source, A destination)` with the reverse members; with a nested copy it stays Manual as forward (case 10b, TAAK-245).
- **Method order**: the reverse method directly after its forward one, so `MapCallPattern.GeneratedAsync` can keep pairing by position (`HasReverseMap`).
- **Existing proofs**: `ForMemberIgnore/reverse-map` and `CreateMap/member-list-reverse-map` became Fixed (output matches cases 1, 11, 12, 17); `CreateMap/options` and `MapCall/manual-profile` used `ReverseMap()` as "another option" and now use `.MaxDepth(1)`.
- **No new pattern class / ID**: `automapper.reverse-map` names the docs slug, as for `automapper.nested-maps` and `automapper.member-list`.

## Steps

- [x] 1. Research: current behaviour (Manual), corpus forms, AutoMapper 14 reverse behaviour measured (table above).
- [x] 2. Implementation: `TryParseMap` (reverse options), `ReverseMapCall`, `TryParseOptions`, `HasReverseMap`, `Reverse`, `MemberPath`, `Unwrap`, `MayFlatten`, `Problem(..., reversed)`, `Rewrite`; `NestedMaps.AddProfiles`; `MapCallPattern.GeneratedAsync`.
- [x] 3. Proofs + unit tests.
- [x] 4. Plan, models, verify (README/INDEX rows and `proofs-doc.py` left to the main session).
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.
- [x] 6. Task to archive (queue row left to the main session).

## Result

- Changed: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, `NestedMaps.cs`, `MapCallPattern.cs`.
- Proofs (Fixed): `CreateMap/reverse-map`, `CreateMap/reverse-map-nested`, `CreateMap/member-list-reverse-map`, `ForMemberIgnore/reverse-map`, `ForMemberIgnore/reverse-map-options`, `ForMemberMapFrom/reverse-map`, `_Combined/reverse-map-call`. Proofs (Manual): `CreateMap/reverse-map-get-method`, `CreateMap/reverse-map-duplicate`, `CreateMap/reverse-map-options`, `ForMemberMapFrom/reverse-map-unflattening`, `ForMemberMapFrom/reverse-map-null-deref`.
- Unit tests: `CreateMapPatternTests.Rewrite_ReverseMapAutoMapperDoesMoreFor_ReportsManual` (7 cases), `Rewrite_ReverseOptionAndReversedMapFrom_OptionWins`; `MapCallPatternTests.FixAsync_ReverseMapInOtherFile_CallsReverseMethod`.
- Test results: `dotnet build` 0 warnings; `dotnet test` 1199/1199; SampleShop 38/38.
- Open: nested profiles (neozhu/cats) stay Manual as before; skoruba's `CreateMap<X, string>().ConstructUsing(...).ReverseMap()` waits for row 12; jhipster `User` (`ForPath`) for row 13.

## See also

- `claude/tasks/automapper-queue.md` — row 10
- `docs/projectinfo/patterns/automapper-reverse-map-plan.md`
- `docs/projectinfo/patterns/automapper-reverse-map-verify.md`
- `docs/projectinfo/patterns/automapper-reverse-map-modellen.md`
- `claude/tasks/archief/TAAK-245-automapper-nested-maps.md`
- `claude/tasks/archief/TAAK-246-automapper-member-list.md`
