# TAAK-257 — automapper.unsupported

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 15 of `claude/tasks/automapper-queue.md`: AutoMapper options the profile rewrite does not migrate get a specific, actionable Manual reason naming the API and what to write by hand, instead of the generic "is not a CreateMap<A, B>() without options"; the options that provably change nothing are dropped.

## Context for the next agent

- Like TAAK-245/246/251 this is a capability of the shared profile rewrite (`CreateMapPattern`, also used by `ForMemberIgnorePattern` and `ForMemberMapFromPattern`), not a pattern class. To keep `CreateMapPattern.cs` small for parallel work, the knowledge lives in `UnsupportedOptions.cs`; `CreateMapPattern` has three small hooks (option skip in `TryParseOptions`, the depth check after `Problem`, the reason in the generic Manual).
- Corpus (TAAK-244 survey): `MaxDepth` 97 / 4, `AfterMap`/`BeforeMap` 18 / 4, `Include`/`IncludeBase`/`IncludeAllDerived` 15 / 2, `ForSourceMember` 2, `PreserveReferences`, `ForAllMembers`, profile settings (naming conventions, `ShouldMapProperty`, `AllowNullCollections`).

## Evidence (AutoMapper 14.0.0, file-based `dotnet run` in a scratch folder)

| Case | Result |
| --- | --- |
| `CreateMap<Src, Dst>().MaxDepth(1)`, flat members | `Id`, `Name` copied, same as without |
| `MaxDepth(0)`, flat members | same as without |
| `PreserveReferences()`, flat members | same as without |
| `CreateMap<Src, Dst>(MemberList.Source).ForSourceMember(x => x.Extra, o => o.DoNotValidate())` | map same as without; `AssertConfigurationIsValid` passes (the option only steers validation) |
| `AfterMap((s, d) => d.Name = d.Name + "!")` | runs after the members are set (`"a!"`) |

## Choices

- **Dropped**: `MaxDepth(constant)` and `PreserveReferences()` when the map copies no nested object or collection (`copied` from `Problem` is null) and has no `ReverseMap` (the reverse map's copies are not tracked separately; conservative). `ForSourceMember(s => s.X, o => o.DoNotValidate())` anywhere in the chain. Non-constant `MaxDepth` arguments are not dropped (a side effect could be lost).
- **Not rewritten: `AfterMap`**. A block-bodied `ToB` is possible, but it also has to run in `MapInto`, interacts with `ReverseMap` and nested profiles, and needs the same "can move outside the profile" checks as `MapFrom`; the Manual reason spells out the block-bodied form instead. Candidate for a row of its own.
- **Manual reasons** per API in `UnsupportedOptions.s_advice` (chain calls innermost first, only the chain spine, not calls inside lambda arguments), per `ForMember` option (`Condition`, `NullSubstitute`, resolver `MapFrom`, `ConvertUsing`, ...), and profile settings written as assignments (`AllowNullCollections = true`). Supported `ForMember` forms (`Ignore`, `MapFrom(s => expr)`) give no reason, so a map that fails for another reason (a member configured twice) keeps the generic text.
- **`IMapFrom<T>` + `Mapping(Profile)`** (jason): not a profile constructor, so this rewrite never sees it; left to the queue.

## Steps

- [x] Measure the options against AutoMapper 14.0.0
- [x] `UnsupportedOptions.cs` + hooks in `CreateMapPattern.cs`
- [x] Proofs `CreateMap/max-depth`, `preserve-references`, `for-source-member-do-not-validate` (Fixed), `max-depth-nested`, `after-map`, `before-map`, `for-all-members`, `for-member-condition`, `profile-setting` (Manual); updated reasons in `construct-using`, `for-ctor-param`, `reverse-map-options`, `ForMemberIgnore/string-name`, `ForMemberMapFrom/resolver-form`; `MapCall/manual-profile` now uses `BeforeMap`; `options` became `max-depth`
- [x] Unit tests `src/Unshackle.Tests/Core/UnsupportedOptionsTests.cs`
- [x] Plan, models, verify in `docs/projectinfo/patterns/automapper-unsupported-*`

## See also

- `docs/projectinfo/patterns/automapper-unsupported-plan.md`
- `docs/projectinfo/patterns/automapper-unsupported-modellen.md`
- `docs/projectinfo/patterns/automapper-unsupported-verify.md`
- `claude/tasks/automapper-queue.md`
