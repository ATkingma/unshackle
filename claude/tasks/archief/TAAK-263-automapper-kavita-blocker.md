# TAAK-263 — automapper kavita blocker

**Status**: ✅ done (2026-10-14)
**Created**: 2026-10-14
**Goal**: understand why `CreateMap<BookmarkSeriesPair, BookmarkDto>` makes the kavita profile `AutoMapperProfiles` Manual (146 Map/ProjectTo calls) and fix what can be fixed without changing semantics.

## Context for the next agent

- Corpus clone: `<corpus>/corpus-automapper/src/kavita` (shallow, commit `3233c26dd5b6`); reports `<corpus>/corpus-automapper/_results/263/kavita-263-before.json` / `kavita-263.json`.
- Code: `MapFromProblem` and the new `NullSafeChain` in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`; `ProjectionMember.Problem` + `Projections`.

## Analysis

### Current situation

The map is `.ForMember(d => d.Id, o => o.MapFrom(src => src.Bookmark.Id))` (and Page, VolumeId, SeriesId, ChapterId). `Kavita.Models.csproj` has `<Nullable>disable</Nullable>`, so Roslyn gives every reference receiver the flow state "not applicable", and `MayBeNull` (not NotNull) is true for `src.Bookmark`. `MapFromProblem` returned Manual: "MapFrom for Id dereferences src.Bookmark, which may be null". One Manual map makes the whole profile Manual (a profile is replaced as a whole), and every `Map`/`ProjectTo` that needs a map of that profile becomes Manual with it (146).

### Problem

The rule is right (AutoMapper runs a MapFrom null-safe: a null in the chain gives the default value, a plain `X = source.Bookmark.Id` throws), but too coarse: for a plain chain of properties there is a hand-written form with the same behaviour.

### New situation

A MapFrom that is a plain chain of properties/fields from the lambda parameter (no calls, no indexers) is written null-safe: `Id = source.Bookmark?.Id ?? default` (value type: `?? default`, the default AutoMapper returns), `Name = source.A?.Name` (reference type), with a trailing `!` for a non-nullable reference member in a nullable context (`source.A?.Name!`, like the `null!` the fix already writes). Anything else that dereferences a possibly-null receiver stays Manual. Not applied to `Condition` and `ForPath` expressions (a condition that dereferences null throws in AutoMapper, measured TAAK-255).

### Improvement

The reverse-map bitwarden case (`ReverseMap().ForMember(... MapFrom(src => src.OrganizationUser.Id))`, proof was Manual) is Fixed too. For kavita the BookmarkSeriesPair map no longer stops at its own null dereference.

### Result for kavita (scan with the tool from this branch)

|                                         | Fixed |                                                               Manual |
| --------------------------------------- | ----: | -------------------------------------------------------------------: |
| Before (`3233c26dd5b6`, main `6d7e3c9`) |     0 | 164 (for-member-map-from 5, map-call 52, map-into 2, project-to 105) |
| After                                   |     0 |                                                    164 (same counts) |

The count does not move because the next blocker of the same map is behind the first: `MapFrom(src => src.Series)` needs `CreateMap<Series, SeriesDto>`, which is defined in two profiles (`AutoMapperProfiles`, and `AutoMapperSeriesProfile` with `int userId`): "several profiles map Series to SeriesDto". The reasons at 145 calls changed accordingly (they name this blocker now). The remaining real blockers in kavita:

1. `AutoMapperChapterProfile`, `ReadingList`, `Series`, `Volume` profiles: `int userId = 0;` in the constructor, captured by the MapFrom lambdas and filled per call through `ProjectTo<T>(config, new { userId })` (AutoMapper parameters). A migration needs the projection to take `userId` as a parameter: its own design (like `ProjectTo with parameters`, already Manual).
2. The duplicate `CreateMap<Series, SeriesDto>` (one with the userId parameters, one plain): AutoMapper's behaviour for two maps of the same pair depends on registration order; not measured, Manual.

kavita cannot reach Fixed through this task; item 1 is the next lever (also the 2 `ProjectTo with parameters` findings).

## Choices

- **Null-safe chain instead of Manual** (chosen): same behaviour as AutoMapper, no new Manual surface. Limit: only plain member chains; `?.` after every receiver that may be null (in a nullable-oblivious project that is every reference receiver, which is what AutoMapper's null check does).
- **Only when the result is an assignment of the value itself**: if the value then goes through a nested map, collection copy or built-in conversion, the old Manual stays (not measured how AutoMapper's null default and these interact).
- **ProjectTo stays Manual for such a member**: an expression tree cannot contain `?.`, and a plain `source.A.B` would throw in memory where AutoMapper returns the default. `ProjectionMember.Problem` carries this to `Projections.Problem` / `Inline`.
- **Design question: should one Manual map block the whole profile, or may the rest be rewritten with only that type pair left Manual? Chosen: keep all-or-nothing per profile (and per solution).** Reasons: (1) AutoMapper runs a profile as one configuration; a map left behind needs the maps its members use (nested maps, `Include`, resolvers), so removing them from the profile makes the remaining map fail at run time (`AssertConfigurationIsValid`/missing map) while a Map call that still depends on it is not rewritten; (2) the fix replaces the profile constructor wholesale and decides `Map` calls from "is the map in a rewritten profile": a half-rewritten profile would need per-pair tracking in `NestedMaps`, `MapBlockers` and the Map patterns, with a wrong guess producing a run-time failure instead of a Manual; (3) in kavita it would not help here, since the next blockers are other profiles. Splitting is possible later, per map, but only where no remaining map reads a removed pair; not done.

## Steps

- [x] Clone kavita (shallow), build, scan (before)
- [x] Cause: nullable disabled, `MayBeNull` true for `src.Bookmark`
- [x] `NullSafeChain` in `MapFromProblem`; `ProjectionMember.Problem` for ProjectTo
- [x] Proofs: `ForMemberMapFrom/may-be-null` (before/after), `reverse-map-may-be-null` (before/after), `may-be-null-call` (manual), `_Combined/may-be-null-profile` (cascade: pair map + sibling map + two Map calls); old `null-deref` and `reverse-map-null-deref` Manual proofs replaced
- [x] Unit tests: `Rewrite_NullableAnnotatedReceiver_UsesNullConditional`, `FixAsync_MapFromThroughMaybeNullMember_ProjectionIsManual`
- [x] KingmaDoc: for-member-map-from plan (REQ-9) / verify / modellen table, reverse-map references
- [x] `dotnet build`, `dotnet test` (1433), SampleShop (38) green; rescan kavita

## Risks & edge cases

| Risk                                                   | Mitigation                                                                                                                        |
| ------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------- |
| `?.` on a receiver that is a struct-like `Nullable<T>` | `MayBeNull` already covers `Nullable<T>`; the chain type stays the member's type (`?? default` only for non-nullable value types) |
| Destination `int?` fed from a non-nullable `int` chain | value then is `source.A?.B ?? default` (int), converted as before; AutoMapper also takes default(int)                             |
| Nullable-enabled project, non-annotated target         | trailing `!`, checked in the unit test                                                                                            |

## Changed files

- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (`MapFromProblem`, `NullSafeChain`)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ProjectionMember.cs`, `Projections.cs`
- `src/Unshackle.Tests/Core/ForMemberMapFromPatternTests.cs`, `ProjectToPatternTests.cs`
- proofs under `src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/` and `_Combined/`
- `docs/projectinfo/patterns/automapper-for-member-map-from-{plan,verify,modellen}.md`, `automapper-reverse-map-{plan,verify,modellen}.md`, `docs/projectinfo/architectuur/proofs.md`

## See also

- `claude/tasks/archief/TAAK-260-automapper-profile-cascade.md`
- `claude/tasks/CORPUSRUN-automapper.md`
