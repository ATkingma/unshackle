# TAAK-251 — automapper.constructor

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 12 of `claude/tasks/automapper-queue.md`: a `CreateMap<A, B>()` whose destination has no public parameterless constructor (positional records, classes with one constructor) is rewritten to `new(source.X, ...) { Y = ... }` where that reproduces AutoMapper 14 exactly; the rest stays Manual with a precise reason.

## Context for the next agent

- Before this task `CreateMapPattern.Problem` returned "has no public parameterless constructor" for every such destination (proof `CreateMap/constructor` was Manual).
- Like TAAK-245/246 this is a capability of the shared profile rewrite, not a pattern class; the profile patterns `CreateMap`, `ForMemberIgnore` and `ForMemberMapFrom` all benefit.
- Corpus (TAAK-244 survey): 63 destination pairs without a parameterless constructor in 8 projects (positional records such as altinnauth's `AccessListInfoDto(Guid Id, string Identifier, string Name, string Description)`, bitwarden's single-parameter response records), `ConstructUsing` 56 / 5 (letterbook 40), `ForCtorParam` 211 / 3 (xipona 197, test code).

## Evidence (AutoMapper 14.0.0, file-based `dotnet run` in a scratch folder)

| Case | Result |
| --- | --- |
| `record Rec(int Id, string? Name)` | `{Id:7, Name:"n"}`, the source `Id` getter read once (the property is not mapped again after the constructor) |
| record with one parameter + `set`/`init` properties | constructor parameter filled, then `Name` (set) and `Extra` (init) mapped too |
| class with `(int id)` and `(int id, string name)` | the two-parameter constructor (most parameters it can resolve) |
| two one-parameter constructors `(int id)` / `(string name)` | the first declared (`id`) |
| private or protected parameterless + public `(int id)` | the public `(int id)` constructor; `Id` read once |
| `record CaseRec(int ID, string NAME)` from `Id`/`Name` | filled: names match in any case |
| optional parameter without a source member (`string other = "def"`, `int Missing = 5`) | default value passed |
| required parameter without a source member | `Map` throws `ArgumentException` ("needs to have a constructor with 0 args or only optional args") |
| `int` source → `string` parameter | converted (`"7"`) |
| nested `Inner` → `InnerDto` and `List<Inner>` → `List<InnerDto>` parameters | mapped; a null source gives `null` and `[]` (same as members, TAAK-245) |
| null `string` source member → `string` parameter | `null` passed to the constructor |
| `ConstructUsing(s => new Cu(s.Id * 10))`, `Id` settable | `Id` = 7: member mapping after `ConstructUsing` overwrites it (source read twice); get-only `Id` keeps 70 |
| `ForCtorParam("id", ...)` and `ForCtorParam("Id", ...)` for parameter `id` | both applied: the name matches in any case; the property is not mapped again |
| `ForMember(d => d.Name, o => o.Ignore())` on a non-constructor member | left at its initializer value |

## Choices

- **Exactly one constructor** (the implicit record copy constructor excluded), and it is public. AutoMapper's choice between several depends on which parameters resolve and on declaration order; freezing that in a hand-written method is not sound without a full resolver model → Manual.
- **Parameter → source member**: the single readable member with the same name ignoring case; values through `MemberValues.Problem` (copy, nested map, collection copy). No member → Manual (AutoMapper may flatten, uses a default, or throws); two members differing in case → Manual.
- **Members after the constructor**: a member named like a parameter (any case) is skipped (measured: not mapped again); the rest use the existing rules, so `Ignore()`/`MapFrom` on them work.
- **`ForMember` on a member a parameter fills** → Manual: not measured whether AutoMapper applies it to the parameter.
- **`ConstructUsing`, `ForCtorParam`** → Manual through the existing "not a CreateMap<A, B>() without options" reason. `ConstructUsing` needs the measured overwrite semantics; `ForCtorParam` is almost only test code.
- **`MapInto`** for a constructor map → Manual: an existing object cannot get new constructor arguments.
- **Output** `new(args)` target-typed, with `{ ... }` only when members remain.

## Steps

- [x] 1. Research: corpus shapes, AutoMapper 14 behaviour measured.
- [x] 2. Implementation in `CreateMapPattern` (`Problem`, new `ConstructorProblem`, `Rewrite`, `Extensions`).
- [x] 3. Proofs + unit tests.
- [x] 4. Plan, models, verify.
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.

## Result

- Changed: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (`Problem` gets `out string creation` and the constructor check, new `ConstructorProblem`, `Rewrite` threads `Creation` through the map tuple and guards `MapInto`, `Extensions` writes the creation expression, doc comment).
- Proofs (Fixed): `CreateMap/constructor` (was Manual), `CreateMap/constructor-record`, `CreateMap/constructor-conversion` (built-in conversion on an argument, after the merge with TAAK-247), `ForMemberIgnore/constructor`, `_Combined/constructor-map-call`. Proofs (Manual): `CreateMap/constructor-several`, `CreateMap/constructor-default`, `CreateMap/construct-using`, `CreateMap/for-ctor-param`, `ForMemberMapFrom/constructor`.
- Unit tests in `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`: three new Manual cases, `Rewrite_ConstructorParameterOtherCase_PassesMember`.
- Test results: see the verify document.
- Open: `ConstructUsing`, `ForCtorParam`, several constructors, optional parameters without a source member.

## See also

- `claude/tasks/automapper-queue.md` — row 12
- `docs/projectinfo/patterns/automapper-constructor-plan.md`
- `docs/projectinfo/patterns/automapper-constructor-verify.md`
- `claude/tasks/archief/TAAK-246-automapper-member-list.md`

- After merging main (TAAK-247/249/250): constructor arguments get built-in conversions through `MemberValues`; a `ReverseMap` whose reverse destination needs a constructor is rewritten only without reversed MapFrom and MapInto, otherwise Manual (`Reverse`).
