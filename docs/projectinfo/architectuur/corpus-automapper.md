# Corpus: AutoMapper ≤14 API usage in 20 open-source projects

Measured on 2026-10-14 for TAAK-244 (row 7 of `claude/tasks/automapper-queue.md`). Replaces the AutoMapper half of
`docs/projectinfo/architectuur/corpus-mediatr-automapper.md`, which was mainly nopCommerce.

## Method

- 20 repos on AutoMapper 12.0–14.0.0 (14.0.0 is the last free version), cloned shallowly at a pinned commit into
  `D:\playground\corpus-automapper\src\<name>\` (outside the repo, read only). List: `D:\playground\corpus-automapper\repos.txt`
  (`<name> <url> <ref> <solution path>`). Repos whose default branch is on AutoMapper 15/16 are pinned at the last commit
  before 2025-07-01 (before AutoMapper 15 went commercial); `jason` is pinned at tag `v8.0.0`, `nop` at `release-4.90.8`.
- Measurement: a regex script over `*.cs` (comments stripped; `bin`, `obj`, `templates`, `wwwroot`, `*.g.cs`,
  `*.Designer.cs` skipped). No Roslyn: numbers are indicative. Script and raw output live outside the repo:
  `python -I D:\playground\corpus-automapper\tools\measure.py <src dir> <out.json>`, then `agg.py <out.json>` for the table
  (`tools\am.json` is this run).
- Structural estimates (nested, flattening, conversions, constructors) use a crude type index: every
  `class`/`record`/`struct` by **simple name** with its public properties (plus base classes and positional record
  parameters). For every `CreateMap<A,B>` (and the reverse direction when the chain has `.ReverseMap()`), each
  destination member that is not configured by `ForMember`/`ForPath` is classified:
  - **nested**: same-named source member whose (element) type differs and the destination (element) type is itself a
    mapped destination; split into single and collection members.
  - **flattening**: no same-named source member, but `GetX()` on the source, or `A` + `B` where `A` is a source member
    and `B` a member of `A`'s type (two levels deep).
  - **conversion**: same-named, non-collection members with different types (`T?` → `T`, to `string`, enum ↔ other, rest).
  - **constructor**: destination type has a positional record/primary constructor, or only constructors with parameters.
  Pairs whose destination type is not in the repo (NuGet types, e.g. Duende entities in `skoruba`) are skipped (108 pairs).
- Builds: `dotnet build <solution>` with SDK 10.0.400, run from outside the repo (so the repo's `global.json` pin is
  not applied), `TEMP`/`TMP` = `<temp-dir>`, 10 minute limit. `abp` was not built (2.2 GB, 150+ test projects).

## Repos

"Tests": test framework, number of test projects; "containers" = some test projects need Testcontainers/Docker.
"partial" = every error is in one non-AutoMapper project (front-end npm/yarn step, Aspire AppHost, Rust tool);
the rest of the solution compiles. Summary: 12 build cleanly, 6 partially, 1 does not, 1 not tried. Logs:
`D:\playground\corpus-automapper\logs\<name>.log`.

| Name | Repo (ref) | AutoMapper | TFM | Tests | Builds | `CreateMap` |
| ---- | ---------- | ---------- | --- | ----- | ------ | ----------: |
| abp | `abpframework/abp` (`d3c97e340785`) | 14.0.0 | net10.0 | xUnit, 151 | not tried (2.2 GB) | 34 |
| altinnauth | `Altinn/altinn-auth` (`2355aed31457`) | 14.0.0 | net10.0 | xUnit (v2/v3), 17; containers | yes | 22 |
| bitwarden | `bitwarden/server` (`a0058fb5a988`) | 14.0.0 | net10.0 | xUnit (v2/v3), 39; containers | partial: one project runs `cargo build` | 70 |
| cats | `ministryofjustice/CFO-CaseAssessmentTrackingSystem` (`8e93a2a650e1`) | 14.0.0 | net10.0 | NUnit, 2 | partial: Aspire AppHost needs the Aspire CLI | 83 |
| dasblog | `poppastring/dasblog-core` (`3d4a3b61f2d1`) | 13.0.1 | net8.0 | xUnit, 2 | yes | 28 |
| devarch | `DevArchitecture/DevArchitecture` (`b59fe4adbc68`) | 13.0.1 | net9.0 | NUnit, 1 | yes | 1 |
| eshoponweb | `dotnet-architecture/eShopOnWeb` (`4da8212117e8`) | 12.0.1 | net8.0 | xUnit + MSTest, 4 | yes | 3 |
| gnomeshade | `VMelnalksnis/Gnomeshade` (`0ae798923f67`) | 14.0.0 | net10.0 | NUnit, 8; containers | partial: web project needs `npm install` | 44 |
| iotsharp | `IoTSharp/IoTSharp` (`3e26d65e0cc5`) | 14.0.0 | net9.0 | MSTest, 2; containers | no (CS0103 in the main project) | 1 |
| jason | `jasontaylordev/CleanArchitecture` (`v8.0.0 (f2f0445721f5)`) | 12.0.1 | net8.0 | NUnit, 5; containers | yes, with `-p:SkipNSwag=True -p:NuGetAudit=false` and NU1507 not as error | 5 |
| jhipster | `jhipster/jhipster-sample-app-dotnetcore` (`7a799c7babb4`) | 13.0.1 | net9.0 | xUnit, 1 | partial: `npm install` step timed out | 11 |
| kavita | `Kareadita/Kavita` (`3233c26dd5b6`) | 12.0.1 | net10.0 | xUnit, 7 | yes | 76 |
| lancommander | `LANCommander/LANCommander` (`2e6fdd19253f`) | 13.0.1 | net9.0 | xUnit, 2 | partial: `npm run package` step | 56 |
| letterbook | `Letterbook/Letterbook` (`dace8fbfb156`) | 14.0.0 | net9.0 | xUnit, 11 | yes | 88 |
| neozhu | `neozhu/CleanArchitectureWithBlazorServer` (`c89f0f9c28d6`) | 13.0.1 | net8.0 | NUnit, 3 | yes | 23 |
| nop | `nopSolutions/nopCommerce` (`release-4.90.8 (918444773367)`) | 14.0.0 | net9.0 | NUnit, 1 | yes | 245 |
| ombi | `Ombi-app/Ombi` (`697d35647bf8`) | 12.0.0 | net8.0 | NUnit, 7 | partial: `yarn install` step | 62 |
| personalasst | `davidtimovski/personal-assistant` (`ac7e1085c3db`) | 14.0.0 | net10.0 | xUnit, 2 | yes | 55 |
| skoruba | `skoruba/Duende.IdentityServer.Admin` (`3fee47255860`) | 14.0.0 | net9.0 | xUnit, 5 | yes | 144 |
| xipona | `Velociraptor45/Xipona` (`12677f35f17a`) | 13.0.1/14.0.0 | net9.0 | xUnit v3, 13 | yes | 33 |

## Frequencies

Hits = regex matches over all repos; "Projects" = repos with at least one hit; "In tests" = hits in paths containing `test`.

| API / shape                                                                 |  Hits | Projects | In tests | Queue row |
| --------------------------------------------------------------------------- | ----: | -------: | -------: | --------- |
| `CreateMap<A,B>()`                                                          |  1084 |       20 |       69 | 1 (done) |
| … of which without any option (or only `.ReverseMap()`)                     |   372 |       18 |        — | 1 (done) |
| `class X : Profile`                                                         |   211 |       17 |       15 | 1 (done) |
| … of which nested in the DTO (`class Mapping : Profile`, jason style)       |    73 |        6 |        — | 21 (Manual today) |
| `.ForMember(...)` (all)                                                     |  2555 |       17 |      187 | 2/3 |
| `opt.Ignore()`                                                              |  1378 |       11 |       44 | 2 (done) |
| `opt.MapFrom(s => ...)` (lambda)                                            |  1339 |       17 |      340 | 3 (done) |
| `opt.MapFrom("Name")` / `MapFrom(nameof(..))`                               |     0 |        0 |        0 | 13 |
| `IMapper` (type use: fields, ctor parameters)                               |   546 |       19 |       70 | 4 (done) |
| `mapper.Map<T>(x)`                                                          |   904 |       16 |      157 | 4 (done) |
| `mapper.Map<T>(x, opts => ...)`                                             |     8 |        1 |        0 | 15 |
| `mapper.Map(src, dest)`                                                     |    67 |        7 |       43 | 5 (done) |
| `ObjectMapper.Map<TSrc,TDest>(x)` (ABP's `IObjectMapper`, not AutoMapper)   |   273 |        2 |       73 | out of scope |
| `AddAutoMapper(...)`                                                        |    22 |       14 |        4 | 6 (done) |
| `cfg.AddProfile<T>()` / `AddMaps(...)`                                      |    75 |       12 |       37 | 6 / 18 |
| nested members (pairs / single members / collection members)                | 131 / 97 / 115 | 12 | — | 8 |
| flattening (pairs / members)                                                |  10 / 13 |   3 |        — | 9 |
| `.ReverseMap()`                                                             |   186 |       10 |        7 | 10 |
| type conversions (pairs with ≥1)                                            |   207 |       15 |        — | 11 (done) |
| … `T?` ↔ `T` / to `string` / enum ↔ other / other (members)                 | 191 / 71 / 29 / 113 | 12 / 11 / 3 / 11 | — | 11 |
| destination without parameterless constructor (pairs)                       |    63 |        8 |        — | 12 |
| `.ConstructUsing(...)`                                                      |    56 |        5 |        4 | 12 |
| `.ForCtorParam(...)`                                                        |   211 |        3 |      197 | 12 |
| `opt.Condition(...)`                                                        |    26 |        4 |        0 | 13 |
| `opt.PreCondition(...)` / `opt.NullSubstitute(...)`                         |     0 |        0 |        0 | 13 |
| `.ForPath(...)`                                                             |    22 |        3 |        0 | 13 |
| `ProjectTo<T>(...)`                                                         |   189 |        7 |        1 | 14 |
| `CreateMap<A,B>(MemberList.X)`                                              |   267 |        6 |       10 | 17 (new) |
| `new MapperConfiguration(...)`                                              |    57 |       14 |       30 | 18 (new) |
| `AssertConfigurationIsValid()`                                              |    18 |        7 |       16 | 18 (new) |
| `.ConvertUsing(...)` / `ITypeConverter<,>`                                  | 40 / 24 | 5 / 6 |   7 / 1 | 19 (new) |
| `IValueConverter<,>`                                                        |    11 |        3 |        0 | 19 (new) |
| `MapFrom<TResolver>` / `IValueResolver` / `IMemberValueResolver`            | 34 / 16 / 23 | 2 / 3 / 3 | 0 | 20 (new) |
| `.MaxDepth(n)` / `.PreserveReferences()`                                    |    97 |        4 |        2 | 15 |
| `.AfterMap(...)` / `.BeforeMap(...)` / `IMappingAction`                     | 18 / 2 / 7 | 4 / 1 / 2 | 4 / 0 / 4 | 15 |
| `.IncludeBase<,>()` / `.Include<,>()` / `IncludeAllDerived()`               | 15 / 0 / 2 | 2 / 0 / 1 | 0 | 15 |
| `.ForSourceMember(...)`                                                     |    23 |        2 |        0 | 15 |
| `IMapFrom<T>` + `void Mapping(Profile)` convention                          | 45 / 42 |      1 |        — | 15 |
| global config (naming conventions, `ShouldMap*`, `AllowNull*`, …)           |     5 |        2 |        2 | 15 |
| `[AutoMap(typeof(..))]` attribute, `ForAllMembers`, `ForAllOtherMembers`, `IncludeMembers`, `UseDestinationValue` | 0 | 0 | 0 | 15 |

Concentration (hits that come mostly from one repo): `Ignore()` (nop 1083 of 1378), `ProjectTo` (kavita 105 of 189),
`ForCtorParam` (xipona 197 of 211, all in test code), `MaxDepth` (letterbook 83 of 97), `ConstructUsing` (letterbook 40 of 56),
`ForPath` (cats 20 of 22), `MemberList` (skoruba 138 of 267). Count projects first, hits second.

## Observations

- AutoMapper in **test code** is common: `MapFrom` lambdas (340), `Map<T>` (157), `ForCtorParam` (197, xipona builds expected
  objects with AutoMapper in tests), and `new MapperConfiguration` / `AssertConfigurationIsValid` are mostly in tests.
  Removing the package means test projects need the same treatment as production code.
- `MemberList.X` is only an argument for configuration validation (`AssertConfigurationIsValid`); a hand-written mapping
  has no use for it, so it can be dropped. Today it would push 267 `CreateMap` calls (skoruba: almost all of them) to Manual.
- The `T?` → `T` and to-`string` conversions are implicit in AutoMapper but a compile error in hand-written code, so they
  block plain `CreateMap` rewrites in 12 of 20 projects. This is the biggest gap after nested members.
- `ProjectTo` is used by few projects, but heavily (kavita, cats, neozhu): all jason-style Clean Architecture.
- Flattening is rarer than expected: 13 members in 3 projects (projects write it out instead: 179 `MapFrom(s => s.A.B)` lambdas).
- `PreCondition`, `NullSubstitute`, `MapFrom("Name")`, `[AutoMap]`, `ForAllMembers`, `IncludeMembers`: 0 hits.
- ABP (`abp`) wraps AutoMapper behind its own `IObjectMapper`, `IMapFrom<T>` and `Ignore*Properties()` helpers;
  ABP also ships a Mapperly integration. Treat ABP as out of scope (counted, but not used for the ordering).

## Recommended order (queue rows 9–15 and new rows)

Row 8 (`automapper.nested-maps`, in progress) is confirmed by the data: 131 pairs in 12 projects.

| Order | Row | Pattern                          | Why                                                                                  |
| ----: | --: | -------------------------------- | ------------------------------------------------------------------------------------ |
|     1 |  17 | `automapper.member-list` (new)   | 267 hits / 6 projects, trivial (drop the argument); unblocks most of skoruba          |
|     2 |  11 | `automapper.conversions`         | 207 pairs / 15 projects; blocks plain `CreateMap` rewrites                            |
|     3 |  10 | `automapper.reverse-map`         | 186 / 10                                                                              |
|     4 |  12 | `automapper.constructor`         | 63 pairs / 8 + `ConstructUsing` 56 / 5 + `ForCtorParam` 211 / 3                       |
|     5 |  14 | `automapper.project-to`          | 189 / 7, heavy per project; needs an EF-translatable expression                       |
|     6 |  18 | `automapper.configuration-test` (new) | `new MapperConfiguration` 57 / 14, `AssertConfigurationIsValid` 18 / 7; mostly tests; needed before row 16 compiles |
|     7 |  19 | `automapper.convert-using` (new) | `ConvertUsing` 40 / 5, `ITypeConverter` 24 / 6, `IValueConverter` 11 / 3             |
|     8 |  20 | `automapper.value-resolver` (new) | 73 hits / 4 projects (`MapFrom<TResolver>`, `IValueResolver`, `IMemberValueResolver`) |
|     9 |  13 | `automapper.member-options`      | `Condition` 26 / 4, `ForPath` 22 / 3 (move `ForPath` here); the rest of the row has 0 hits |
|    10 |   9 | `automapper.flattening`          | 13 members / 3 projects                                                               |
|    11 |  15 | `automapper.unsupported`         | rest: `MaxDepth`/`PreserveReferences` (4), `AfterMap` (4), `IncludeBase` (2), `ForSourceMember` (2), `IMapFrom` convention (1) |

## See also

- `claude/tasks/automapper-queue.md` — the queue this survey orders
- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` — the earlier, smaller corpus (MediatR + AutoMapper)
- `claude/tasks/archief/TAAK-244-automapper-corpus-survey.md` — the task
- `src/Unshackle.Core/Fixes/AutoMapper/` — the AutoMapper patterns
