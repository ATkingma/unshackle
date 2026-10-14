# TAAK-265 — automapper kavita: duplicate map and ProjectTo parameters

**Status**: ✅ done (2026-10-14)
**Created**: 2026-10-14
**Goal**: after TAAK-263 kavita is blocked by (1) `CreateMap<Series, SeriesDto>` in two profiles and (2) `int userId = 0;` in four profile constructors, filled per call through `ProjectTo<T>(config, new { userId })`. Establish AutoMapper 14's behaviour and rewrite only what it provably pins down.

## Context for the next agent

- Corpus: `<corpus>` = `<disk>/unshackle-corpus/corpus-automapper/`, clone `src/kavita` (commit `3233c26dd5b6`); reports `<corpus>/_results/265/kavita-voor.json` / `kavita-na.json`.
- Code: new `CapturedLocal.cs`; `CreateMapPattern.Rewrite` (statement loop, `Extensions`, `RewriteProjectionAsync`, `MapFromProblem`); `ProjectToPattern` (`Check`, `TryParameters`, `Arguments`, `ParametersAsync`, `Existing`); `MapBlockers.Several`.
- Probe: file-based console project with AutoMapper 14.0.0 (scratch folder, not in the repo).

## Evidence (AutoMapper 14.0.0)

Profiles: `Plain` = `CreateMap<P, D>()`; `WithUser` = `int userId = 0;` + `ForMember(Read, MapFrom(s => s.Marks.Count(m => m == userId)))`; `WithUser2` = `ForMember(Extra, MapFrom(s => s.Id + userId))`.

| Registration order  | `Map`           | `ProjectTo(cfg)` | `ProjectTo(cfg, new { userId = 2 })` | `AssertConfigurationIsValid`             |
| ------------------- | --------------- | ---------------- | ------------------------------------ | ---------------------------------------- |
| Plain, WithUser     | Read 0          | Read 0           | Read 2 (WithUser wins)               | `DuplicateTypeMapConfigurationException` |
| WithUser, Plain     | Read 0          | Read 0           | Read 0 (Plain wins)                  | `DuplicateTypeMapConfigurationException` |
| WithUser, WithUser2 | Read 0, Extra 5 | Read 0, Extra 5  | Read 0, Extra 7 (only WithUser2)     | `DuplicateTypeMapConfigurationException` |
| WithUser only       | Read 0          | Read 0           | Read 2                               | (unmapped member, unrelated)             |

Conclusions: (1) two maps for one pair do **not** throw at `Map`/`ProjectTo`: the one registered last replaces the earlier ones whole (no merging of members), only `AssertConfigurationIsValid` rejects the duplicate. (2) `Map` and a `ProjectTo` without parameters use the literal of the captured local (`0`); `ProjectTo(cfg, new { userId = 2 })` replaces it in the projection.

## Analysis

### Current situation

`CreateMap<Series, SeriesDto>` is in `AutoMapperProfiles` (plain) and `AutoMapperSeriesProfile` (six `ForMember`s over `userId`); the reasons said "Several profiles map Series to SeriesDto; choose by hand". The four `userId` profiles were Manual because the statement `int userId = 0;` is "not a CreateMap", and `ProjectTo` with a second argument was Manual in every case.

### Problem

1. Which of the two Series maps AutoMapper runs depends on the order in which the profiles are added (kavita adds them through assembly scanning), which the fix cannot read. In kavita the definitions differ (the plain one has no `userId` members), so one generated `ToSeriesDto` could be the wrong one. Kavita evidently relies on the `userId` one winning.
2. A MapFrom that reads a profile-constructor local cannot move to a static class as is, and `ProjectTo(.., new { userId })` needs a projection that takes the value per call.

### New situation

1. **Duplicate map**: stays Manual, but with the measured reason (`MapBlockers.Several`): "AutoMapper uses the one registered last (its order is not known to this fix, and AssertConfigurationIsValid rejects the duplicate); keep one definition and run the fix again". One extension method per pair is not generated: not provable (decided, see Choices).
2. **Captured locals**: `CapturedLocal` recognises `T name = <literal>;` in the profile constructor. A MapFrom may read it (`MapFromProblem`); the generated `ToB` and the projection get it as an optional parameter with the literal as default:
   `ToSeriesDto(this Series source, int userId = 0)` and `Expression<Func<Series, SeriesDto>> SeriesToSeriesDtoProjection(int userId = 0)` (a method instead of a property only for such a map). `Map(x)` becomes `x.ToSeriesDto()` = AutoMapper's `Map` (0). `ProjectTo<SeriesDto>(config, new { userId })` becomes `query.Select(SeriesProfileExtensions.SeriesToSeriesDtoProjection(userId))`, `ProjectTo<SeriesDto>(config)` becomes `...Projection()`. The argument expression is evaluated once, outside the expression tree (a captured variable of the lambda, like AutoMapper's parameter).

### Improvement

Profiles with a placeholder variable and their `ProjectTo(.., new { name })` calls are migrated instead of Manual, with the values AutoMapper gives (default for `Map`/no parameters, the argument for ProjectTo); every other form of parameters stays Manual with a precise reason.

## Choices

- **Optional parameter with the literal as default** (chosen) instead of an overload or a field: the MapFrom text is kept as is (also inside nested inlined maps of the same profile, whose members name the same variable), `ToB()` equals `Map` (measured 0), one method per map. Limit: only a literal initializer; `var` with a literal works (type from the symbol).
- **Method vs property**: a projection with parameters is a method; maps without captured variables keep the property (no change for existing code). `ProjectToPattern.Existing` accepts both, so the projection "just written in the same file" is found.
- **Call arguments**: `new { userId }` / `new { userId = expr }`; positional while the call passes the leading parameters in order, else named; a parameter left out keeps its default. Manual when: another name (not measured what AutoMapper does with an unused parameter), another type than the variable (AutoMapper converts, we do not), a non-anonymous object, `membersToExpand`.
- **MapInto and ReverseMap together with a captured variable** → Manual (not measured; `MapInto`/reverse methods would not carry the parameter).
- **Existing `ToB`** (`MapCallPattern.Existing`) may have extra parameters if all are optional.
- **Duplicate map → Manual** (not one method): measured "last registered wins", but the order is unknowable statically (assembly scanning, several `AddProfile` calls) and the definitions need not be equal (kavita: they differ). Generating one method and dropping the other profile's map would need cross-profile edits; for identical definitions it would be provable, but no corpus repo needs it. Not done.
- `ProofTests` now shows the Manual reasons when no Fixed finding is found.

## Result for kavita

See table below (scan with the tool from this branch, same clone `3233c26dd5b6`, main `6d7e3c9` + TAAK-263/264).

|        | Fixed | Manual |
| ------ | ----: | -----: |
| Before | 0 | 164 (project-to 105, map-call 52, for-member-map-from 5, map-into 2) |
| After  | 0 | 164 (same counts) |

The counts do not move: 146 findings still name the duplicate `CreateMap<Series, SeriesDto>` (now with the measured reason: left to the user, see Choices), the rest is the cascade (4), `.MapChapterBase(userId)` in `AutoMapperChapterProfile` (4: a `CreateMap` chain through a user extension method `MapChapterBase<TDest>(this IMappingExpression<Chapter, TDest>, int userId)` outside the constructor: "not a CreateMap<A, B>() without options"), the generic wrapper `ProjectToWithProgress<TSource, TDestination>` (2: `ProjectTo<TDestination>(config, new { userId })` with a type parameter; its ~20 call sites are not AutoMapper calls and are never seen), and a few Map findings. Remaining blockers for kavita, in order: (1) remove one of the two `CreateMap<Series, SeriesDto>` (the plain one in `AutoMapperProfiles` is dead anyway if the `userId` profile is registered last; kavita should keep the `AutoMapperSeriesProfile` one); (2) `MapChapterBase` helper (a map built by an extension method, like gnomeshade's `CreateMapsForV1_0`); (3) the user wrapper `ProjectToWithProgress`: with TDestination generic the fix cannot choose a projection, the wrapper and its call sites have to be replaced by hand (or by `Select(SeriesProfileExtensions.SeriesToSeriesDtoProjection(userId))`).

## Steps

- [x] Probe AutoMapper 14 (duplicate maps, parameters)
- [x] Scan kavita before (`kavita-voor.json`)
- [x] `CapturedLocal`, `MapFromProblem`, `Extensions` (method form), `ProjectToPattern` (parameters, `Existing` for methods), `MapCallPattern.Existing` (optional parameters), `MapBlockers.Several`
- [x] Proofs: `_Combined/projection-parameters`, `ProjectTo/parameters` (reason), `ProjectTo/parameters-generic`, `MapCall/several-profiles`, `CreateMap/nested-several` (reason)
- [x] Unit tests: `FixAsync_CapturedVariable_ProjectionTakesItAsParameter`, `FixAsync_ParametersThatDoNotFitTheCapturedVariable_AreManual` (3), `FixAsync_SameMapInTwoProfiles_NamesTheMeasuredAutoMapperBehaviour`
- [x] KingmaDoc: project-to plan (REQ-10/11)/verify/modellen, for-member-map-from plan (REQ-10)/verify/modellen (D2 rerendered), map-call plan (REQ-11)/verify
- [x] `dotnet build`, `dotnet test`, SampleShop green; rescan kavita (after)

## Risks & edge cases

| Risk                                                             | Mitigation                                                                          |
| ---------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| A captured variable of another type than the call's argument     | Manual (type compared as written at the call)                                       |
| A captured variable used by a Condition/resolver in the same map | those paths still reject locals (`IsAvailableOutsideProfile`): Manual               |
| `userId` also a lambda-local name inside a MapFrom               | C# forbids shadowing a local with a lambda parameter, the profile would not compile |
| Optional parameter on `ToB` seen by callers via nested maps      | nested `source.X?.ToY()` takes the default, like AutoMapper's nested `Map` (0)      |

## Changed files

- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CapturedLocal.cs` (new), `CreateMapPattern.cs`, `ProjectToPattern.cs`, `MapCallPattern.cs`, `MapIntoPattern.cs`, `MapBlockers.cs`, `MemberValues.cs`
- `src/Unshackle.Tests/Core/ProjectToPatternTests.cs`, `src/Unshackle.Tests/ProofTests.cs`
- proofs: `Proofs/AutoMapper/_Combined/projection-parameters.*`, `ProjectTo/parameters*`, `MapCall/several-profiles.*`, `CreateMap/nested-several.manual.txt`
- `docs/projectinfo/patterns/automapper-{project-to,for-member-map-from,map-call}-{plan,verify}.md`, `-modellen.md` (project-to, for-member-map-from + img), `docs/projectinfo/architectuur/proofs.md`

## See also

- `claude/tasks/archief/TAAK-263-automapper-kavita-blocker.md`
- `claude/tasks/archief/TAAK-252-automapper-project-to.md`
- `claude/tasks/CORPUSRUN-automapper.md`
