# TAAK-260 — automapper profile cascade

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: the biggest Manual group of the corpus scan (684 map-call + 144 project-to + 14 map-into, 842 in all): "No CreateMap<A, B> in a profile this fix rewrites". Find out per repo why the map's profile is not rewritten, fix what is a bug or a simple convention, and make the reason name the real blocker otherwise.

## Context for the next agent

- `MapCallPattern.GeneratedAsync` only returns maps of profiles whose rewrite (`CreateMapPattern.RewriteMethodsAsync`) is Fixed. When it finds nothing, the three call patterns (`MapCallPattern`, `MapIntoPattern`, `ProjectToPattern`) used to report the generic "No CreateMap<A, B> in a profile this fix rewrites".
- New: `MapBlockers.ReasonAsync` (in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/`) runs only on that Manual path. It looks for the CreateMap<A, B> (generic, `CreateMap(typeof(A), typeof(B))`, or the reverse with `ReverseMap()`) in the calling project and the projects it depends on, and returns:
  - `CreateMap<A, B> is in profile P, which this fix does not rewrite: <the profile's own Manual reason>`;
  - `CreateMap<A, B> is in T.Method, outside a Profile constructor; ...` (the `IMapFrom<T>.Mapping(Profile)` convention, static configuration extensions);
  - `CreateMap<A, B> maps a type parameter ...` when A or B is a type parameter;
  - null otherwise (the generic text stays).

## Root causes (scan of 2026-10-14, heuristic: the CreateMap found by text in the clone, matched to the profile findings of the report)

| Repo | Count | Root cause |
| --- | --- | --- |
| kavita | 149 | profile Manual: one map (`BookmarkSeriesPair` → `BookmarkDto`) blocks the whole AutoMapperProfiles (146); 3 a profile with a non-CreateMap statement |
| letterbook | 126 | profiles Manual (`MaxDepth`/`IncludeBase`/`ConstructUsing`, member mismatches) ~93; type not found by text 17 (custom converters, `Linkable` → `Uri`) |
| jhipster | 90 | profile Manual: `Condition`/`ForPath` on a reverse map (89, of which 23 through `ReverseMap`) |
| bitwarden | 85 | profiles Manual (self-maps `X` → `X` with navigation members not copyable) 76; generic `Map<T, TEntity>` / not found 9 |
| skoruba | 85 | generic profiles and generic services (`TUserDto`, `TRole`, `PagedList<T>`) 55; profiles Manual (`MemberList`, `MapFrom` forms) 30 |
| ombi | 59 | profiles Manual (member mismatches, `ReverseMap` with options) 55; generic `Map<T, ...>` 4 |
| cats | 55 | profiles Manual (expression-bodied constructor maps, member mismatches) |
| gnomeshade | 53 | maps registered in extension methods on `IMapperConfigurationExpression` (`CreateMapsForV1_0()`), not in a profile (49); not found 4 |
| dasblog | 44 | profiles Manual: several constructors (35), member mismatches (7); not found 2 |
| personalasst | 29 | `IMapFrom<T>` + `Mapping(Profile)` with explicit `profile.CreateMap` (16); profiles Manual (`MapFrom` forms) 12; generic 1 |
| lancommander | 27 | profile Manual: one self-map blocks the profile (26); not found 1 |
| neozhu | 20 | profiles Manual (member mismatches) |
| xipona | 10 | maps of tuples / interfaces with `ConvertUsing` outside profiles, not found by text |
| altinnauth | 7 | profile Manual: `AllowNullCollections = true` |
| devarch | 2 | profile Manual: `UserDto.Password` has no source |
| nop | 1 | not found (map registered in a static mapper configuration) |

Summary: ~670 of 842 (80%) are a cascade of a Manual profile (often one map blocks a whole profile with dozens of maps), ~70 are maps outside a profile constructor (gnomeshade's configuration extensions, personalasst's `IMapFrom<T>`), ~60 involve type parameters, the rest the static text search could not attribute. No matching bug (namespaces, nested types) turned up: every pair checked had a profile that is Manual or no profile at all.

## Choices

- **No `IMapFrom<T>` support yet**: only personalasst (16) in this scan uses it with `CreateMap` written out; jason uses the default interface method, and the calls there do not reach this group. Rewriting a `Mapping(Profile)` method needs its own design (the extension class next to a DTO, the default `CreateMap(typeof(T), GetType())` of the interface), so it stays a queue candidate; the reason now says the map is outside a profile constructor.
- **Real blocker in the reason** instead of a fix: the biggest lever is the profile's own Manual reason (kavita: fixing one map unblocks 146 calls). The report now shows it at every call.
- **Type parameters**: a Map with a type parameter on either side is Manual with its own reason (the map depends on the type argument at run time).
- `MapBlockers` repeats the profile rewrite only on the Manual path, so Fixed runs pay nothing.

## Steps

- [x] Classify the 842 findings per repo (script over `report.json` + clones)
- [x] `MapBlockers.ReasonAsync`, used by `MapCallPattern`, `MapIntoPattern`, `ProjectToPattern`
- [x] Unit tests `MapCallPatternTests.FixAsync_MapNotRewritten_NamesTheBlocker` (Manual profile, `Mapping(Profile)` generic and `typeof` form); proof `MapCall/manual-profile` expectation updated
- [x] Build, `dotnet test` (1410 green), SampleShop (38 green)
- [x] Rescan personalasst, altinnauth, kavita with the branch tool: same counts per pattern, the reasons name the blocker (kavita 149 → `profile AutoMapperProfiles ...: CreateMap<BookmarkSeriesPair, BookmarkDto>: ...`)

## See also

- `claude/tasks/archief/TAAK-257-automapper-unsupported.md` (profile Manual reasons for options)
- `claude/tasks/CORPUSRUN-automapper.md`
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapBlockers.cs`
