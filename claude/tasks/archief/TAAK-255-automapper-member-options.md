# TAAK-255 — automapper.member-options

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 13 of `claude/tasks/automapper-queue.md`: `ForMember` with `Condition`/`PreCondition` and `ForPath` are rewritten where that reproduces AutoMapper 14 exactly; the rest stays Manual with a precise reason.

## Context for the next agent

- Corpus (TAAK-244 survey): `Condition` 26 / 4 (letterbook `opt.Condition(dto => dto.Id != null)`, cats `src.DocumentId != Guid.Empty`, skoruba `srs != null`, letterbook AstMapper block `{ opt.Condition(...); opt.MapFrom(...); }`), `ForPath` 22 / 3 (cats single-member `dest => dest.Template` with `src as X`, jhipster on the reverse map, personalasst `x => x.Ingredient.Name`). `PreCondition`, `NullSubstitute` 0 hits.
- Like TAAK-251 this is a capability of the shared profile rewrite; `CreateMapPattern` claims profiles with these options (`ForMemberOption` does not know them), or `ForMemberMapFrom`/`ForMemberIgnore` when the profile also has those.

## Evidence (AutoMapper 14.0.0, file-based `dotnet run` in a scratch folder)

| Case | Result |
| --- | --- |
| `Condition(s => s.Flag)`, flag false, new object | member keeps its initializer (`"init"`) or default (`0`) |
| same, `Map(s, existing)` | member keeps the existing value |
| `{ Condition(s => s.Flag); MapFrom(s => Count(s)); }`, flag false | `MapFrom` still ran (counter 1): Condition is evaluated after the value |
| `{ PreCondition(s => s.Flag); MapFrom(...); }`, flag false | `MapFrom` not run |
| `Condition(s => s.Inner!.Ok)` with `Inner` null | `Map` throws (the condition is not null-safe) |
| `Condition((s, d) => d.N == 0)` | new object: `d.N` is the default, mapped; existing `N = 3`: kept |
| `ForPath(d => d.Inner.Name, ...)` + `ForPath(d => d.Inner.Count, ...)` | `Inner` created once, its initializer kept (`"pinit"`) |
| `ForPath(d => d.Other!.Name, o => o.MapFrom(s => s.Inner!.Name))`, `Inner` null | `Other` created anyway with `Name` null (MapFrom null-safe) |
| `ForPath(d => d.Top, o => o.MapFrom(s => s.Inner!.Name))`, `Inner` null | `Top` null: like `ForMember` MapFrom |
| `ForPath` with `Map(s, existing)` | the existing `Inner` is reused (same reference, its other members kept) |
| `AssertConfigurationIsValid` on these maps | passes |

## Choices

- **Condition → `X = cond ? value : default`** only when a new destination holds `default` in `X` before mapping: no initializer on the member, no declared constructor on the destination or its base types, created by `new()`. Otherwise Manual: an object initializer cannot keep the initializer value without repeating it (and its side effects).
- **Condition and PreCondition give the same code**: the moved values follow the `MapFrom` rules, so evaluating the value only when the condition holds does not change the result.
- **The condition passes `MapFromProblem`** (target `bool`): no `this`, no profile members, no maybe-null dereference (AutoMapper throws there, but the rule is shared and conservative).
- **`default!`** for a non-nullable reference member (AutoMapper writes null there too), as the conversions do with `!`.
- **`Condition((s, d) => ...)`** and other overloads → Manual: reading the destination inside an object initializer is not possible.
- **ForPath of one member** = `MapFrom` (measured null-safe the same way). **ForPath `d.A.B`** → `A = new() { B = ... }` when `A` is settable, a class with a public parameterless constructor, and AutoMapper does not also map `A` (source member of that name or a flattening prefix, a `ForMember` on it). Deeper paths → Manual.
- **MapInto and ReverseMap** with a Condition or ForPath → Manual (existing value kept / existing object reused; reversing a ForPath not measured). The jhipster `ReverseMap().ForPath(...)` proof now gets that precise reason.
- **NullSubstitute, UseDestinationValue, ConvertUsing, AllowNull, ...** → Manual "`<option>` for X is not migrated" (0 hits; not worth measuring now).

## Steps

- [x] 1. Research: corpus shapes, AutoMapper 14 behaviour measured.
- [x] 2. Implementation: new `MemberOptions`, `CreateMapPattern` (`TryParseMap`, `TryParseOptions`, `Rewrite`, `Problem`, new `PathProblem`, `ConditionProblem`, `KeepsDefault`, `Grouped`).
- [x] 3. Proofs + unit tests.
- [x] 4. Plan, models, verify.
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.

## Result

- Proofs (Fixed): `CreateMap/condition`, `CreateMap/condition-map-from`, `CreateMap/pre-condition`, `CreateMap/for-path`, `CreateMap/for-path-member`, `_Combined/condition-map-call`. Proofs (Manual): `CreateMap/condition-initializer`, `CreateMap/condition-destination`, `CreateMap/for-path-deep`, `CreateMap/null-substitute`; `CreateMap/reverse-map-options` reason updated.
- Unit tests in `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`: `Rewrite_MemberOptionAutoMapperDoesMoreFor_ReportsManual` (6 cases), `Rewrite_ConditionOnConditionalValue_GroupsTheValue`.
- Test results: see the verify document.
- Open: `NullSubstitute`, `UseDestinationValue`, conditions with `MapInto` (`destination.X = cond ? value : destination.X` would work), `ForPath` with `MapInto` (`destination.A ??= new()`), deeper `ForPath`.

## See also

- `claude/tasks/automapper-queue.md` — row 13
- `docs/projectinfo/patterns/automapper-member-options-plan.md`
- `docs/projectinfo/patterns/automapper-member-options-verify.md`
- `claude/tasks/archief/TAAK-251-automapper-constructor.md`
