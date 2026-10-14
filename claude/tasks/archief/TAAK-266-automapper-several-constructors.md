# TAAK-266 — AutoMapper: several constructors, ConstructUsing, MaxDepth

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: the AutoMapper fix reproduces AutoMapper 14's constructor choice where it is statically decidable, uses a `ConstructUsing(s => expression)` as the creation, and drops a harmless `MaxDepth`; the rest stays Manual with a precise reason. Corpus targets: dasblog (35 calls blocked) and letterbook (~80).

## Context for the next agent

- Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (`ConstructorChoice`, `ConstructUsingProblem`, `BlockMarker` in `Extensions`, `SeveralConstructorsReason`, the depth rule in `Rewrite`), `MemberOptions.ConstructUsing`, `UnsupportedOptions.MaxDepthLimit`.
- The task brief said dasblog is blocked by "destination types with several constructors". It is not: `ProfilePost` and `ProfileDasBlogUser` are **profiles** with two constructors (one empty, one taking a service, holding all maps). Letterbook's `Mention` is a destination type with a private parameterless and a public three-parameter constructor.
- Probe: a file-based `dotnet run` program against AutoMapper 14.0.0 in a scratch folder (not in the repo); results below.
- Docs: `docs/projectinfo/patterns/automapper-constructor-{plan,modellen,verify}.md` (REQ-4, REQ-6 changed; REQ-8..11 new). The decision tree has 20 arrows (the known >12 issue, not split).

## Evidence (AutoMapper 14.0.0, probe program)

| Case                                                                                           | Result                                                                                                                                                                                                                                                                                                 |
| ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| public `()` + public `(int id)`, source has `Id`                                               | `(int id)` is taken: parameters beat a parameterless constructor (this also holds for an internal `()`)                                                                                                                                                                                                |
| `(id)` and `(id, name)`, source has no `Name`                                                  | `(id)`; `(id, other)` with `other` missing + public `()` → `()`                                                                                                                                                                                                                                        |
| `(id)` vs `(id, string x = "d")`, either order                                                 | the one with more parameters, optional ones count (resolved by the default)                                                                                                                                                                                                                            |
| `(name, id)` declared before `(id, name)`, both resolvable                                     | the first declared                                                                                                                                                                                                                                                                                     |
| internal, protected and private `(id, name)` next to public `(id)`                             | the one with more parameters, any accessibility                                                                                                                                                                                                                                                        |
| no constructor resolvable                                                                      | `Map` throws ArgumentException ("needs to have a constructor with 0 args or only optional args")                                                                                                                                                                                                       |
| `(id, innerName)` with `Inner.Name` on the source                                              | resolved by flattening, beats `(id)`                                                                                                                                                                                                                                                                   |
| `ForMember(d => d.Name, MapFrom(s => s.Title))`, `Name` also a parameter, source has no `Name` | the parameter is **not** resolved by the ForMember (throws; with `Ignore()` too); with a shorter constructor available it is taken and `Name` is then set                                                                                                                                              |
| same, source has `Name`                                                                        | the parameter takes `source.Name`, the ForMember is not applied (kept Manual)                                                                                                                                                                                                                          |
| `ConstructUsing(s => new X{Name="ctor"+s.Id})`                                                 | the destination is that object; then members are mapped: a settable member is overwritten from its source (`Id`, `Name`), a member without a source keeps the value (`Name` = "ctor7" when the source has none), `Ignore()` keeps it, `MapFrom` wins, a get-only member is not written (`Id` 70 stays) |
| `ConstructUsing` into a type with constructor parameters named like source members             | no parameter is skipped: the members are mapped on top of the object                                                                                                                                                                                                                                   |
| `ConstructUsing` returning a derived type                                                      | the derived instance is the result                                                                                                                                                                                                                                                                     |
| `ConstructUsing(s => new X { Name = s.Inner.Name })`, `Inner` null                             | a NullReferenceException is thrown, not swallowed (a MapFrom would be null-safe; a hand-written method throws as well)                                                                                                                                                                                 |
| ConstructUsing overloads in v14                                                                | `Expression<Func<TSource,TDestination>>` and `Func<TSource,ResolutionContext,TDestination>`: a block body does not compile, so only the `ResolutionContext` form is a second shape                                                                                                                     |
| `MaxDepth(1)` on a map whose member is a `List<N3>` (same with `N3` map `MaxDepth(1)`)         | the list comes out **empty**; `MaxDepth(2)`, 3 and 64 give the full list                                                                                                                                                                                                                               |
| `MaxDepth(1)` on a chain of objects `N1 → N2 → N3`, one map per pair                           | full object graph (depth counts the same pair stacked in itself)                                                                                                                                                                                                                                       |
| recursive `Node.Child`, `MaxDepth(2)`                                                          | cut after two levels (relevant only for cycles)                                                                                                                                                                                                                                                        |

## Choices

- **Constructor choice**: all non-copy constructors, most parameters first (stable order = declaration order), the first whose every parameter is resolvable (exactly one readable source member of that name in any case, or an optional parameter) wins. Manual when: a parameter has no match but `MayFlatten` (flattening or a Get method might resolve it), it matches several members, `ref/out/params`; no constructor resolves (AutoMapper throws); tied winners in different files; the winner is private/protected (an extension method cannot call it) or a non-public parameterless one. A single public constructor keeps the TAAK-251 path and messages. A `ForMember` on a parameter's member does not influence the choice (measured).
- **Parameterless wins only as fallback**: before this task a public parameterless constructor always meant `new()`; AutoMapper would have used a resolvable longer constructor instead. Fixed together with the choice.
- **`ConstructUsing`**: a one-parameter lambda with an expression body (the parameter becomes `source`) is the creation; nothing of the profile may be used (no constructor parameter such as `opts.Value`, field, private or non-static member; `this`; a private static helper) and no `source`/`destination` name clash; the members are then mapped as usual (`constructor` stays null: no parameter is skipped). A plain `new T(args)` without initializer (or no member left) → `=> new T(...) { members }`; anything else with members left → block body `T destination = expression; destination.X = ...; return destination;`.
- **`ConstructUsing` Manual**: `ResolutionContext` variant, block body, method group or `ConstructUsing<T>()`, abstract or non-class/struct destination, after `ReverseMap`, with a Condition or ForPath, with `MapInto` or `ProjectTo` on the map.
- **`MaxDepth(n)`**: dropped on a nested copy only for a constant n of 2 or more, without `ReverseMap` and `ProjectTo`; cycles are already Manual in `MemberValues` ("can lead back"), which makes a stacked pair impossible. `MaxDepth(1)`, a non-constant n and `PreserveReferences` (shared references change identity even without a cycle) stay Manual.
- **Profile with several constructors**: stays Manual, but the reason now says which constructor holds the maps and which parameters or fields the lambdas read. A static `ToB` cannot reach a service the application hands in, and the application decides which constructor runs. Not fixable without changing the call shape (pass the service as an argument).

## Corpus (scan with `automapper` fix, `_results/266/<name>-voor|na.json`, tool = main 9a919ba / this branch)

| Repo       | Fixed voor | Manual voor | Fixed na | Manual na |
| ---------- | ---------: | ----------: | -------: | --------: |
| dasblog    |          0 |          54 |        0 |        54 |
| letterbook |          0 |         156 |        0 |       156 |

Counts did not move (all-or-nothing per solution); the reasons did:

- dasblog: the 35 "several constructors" are now "the maps are in the constructor (IDasBlogSettings dasBlogSettings) and read `_dasBlogSettings`, `dasBlogSettings`, `ConvertCategory`" (27) and "(ISiteSecurityManager siteSecurityManager) ... `siteSecurityManager`" (8): real blocker, services injected through the profile constructor. Next blockers behind it: `MapFrom` of `WebFinger.Aliases` dereferences a possibly-null member (5), collection of a kind this fix does not copy (`CategoryCacheEntryCollection`, 3), `ConfigAction(sp, c)` configuration (1).
- letterbook: `Mention` (42) is created by AutoMapper through its **private** parameterless constructor (the public three-parameter constructor has `source`/`subject` parameters without a source member): cannot be written outside the type. `ModerationReport` ConstructUsing uses the profile constructor parameter `opts` (27, also `Post` via `options.Value` and the private static `NewPost`). Behind them: `ConvertUsing<DateTimeOffsetMapper>` replacing the built-in conversion (11), `ConvertUsing` with `MaxDepth(64)` on a reverse/base map (6), `ConstructUsing` with `Condition` (6), maps outside a profile constructor (`AstMapper`, 8), `IncludeBase`/`AfterMap` (unsupported), the ResolutionContext `ConstructUsing` on `ContentDto → Content`.

## Steps

- [x] 1. Probe against AutoMapper 14.0.0 (choice, ConstructUsing, MaxDepth).
- [x] 2. `ConstructorChoice`, `ConstructUsingProblem`, block body, `MaxDepth` rule, profile-constructor reason.
- [x] 3. Proofs (`CreateMap/constructor-several*`, `construct-using*`, `max-depth-*`, `ForMemberIgnore/construct-using-keeps`) and unit tests (`CreateMapPatternTests`).
- [x] 4. Plan, models, verify of `automapper-constructor`; `kingmadoc render`; proofs doc.
- [x] 5. Corpus scan dasblog and letterbook before and after.
- [x] 6. `dotnet build`, `dotnet test` (1480), SampleShop tests (38).

## Result

- Changed: `CreateMapPattern.cs`, `MemberOptions.cs`, `UnsupportedOptions.cs`, `CreateMapPatternTests.cs`, proofs, the three `automapper-constructor-*` docs, `proofs.md`, `CORPUSRUN-automapper.md`, `automapper-queue.md`, `INDEX.md`.
- Open: the profile-with-service shape (dasblog) and `IOptions` captured by `ConstructUsing` (letterbook) need a design decision (extension methods that take the service as a parameter, or leave to the user); `ForCtorParam`; `ConstructUsing` after `ReverseMap`; `ForMember` on a parameter-filled member whose source member exists (measured: the ForMember is ignored; still Manual).

## See also

- `claude/tasks/archief/TAAK-251-automapper-constructor.md`
- `claude/tasks/CORPUSRUN-automapper.md` (section TAAK-266)
- `docs/projectinfo/patterns/automapper-constructor-plan.md`
