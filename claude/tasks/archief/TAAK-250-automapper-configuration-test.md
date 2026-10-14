# TAAK-250 — automapper.configuration-test

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 18 of `claude/tasks/automapper-queue.md`: `new MapperConfiguration(cfg => cfg.AddProfile<P>() / AddMaps(...))`, `config.AssertConfigurationIsValid()` and `config.CreateMapper()` used to build a test mapper go once the profiles are hand-written methods; what cannot go safely is Manual.

## Context for the next agent

- Before this task nothing claimed these calls: the package stays (no `AutoMapperFix.Package` yet), so they kept compiling and running against the emptied profiles. They are the AutoMapper code that is left in test projects after a successful run.
- New pattern class `ConfigurationTestPattern` (`ISolutionPattern`) + helper `ConfigurationSyntax`; one line in `AutoMapperFix.Patterns`, **last**: the Fixer runs patterns one after another on the same file, so by the time this pattern looks, `MapCallPattern` has already turned `_mapper.Map<B>(a)` into `a.ToB()`. `CreateMapPattern.cs` is unchanged (only its `internal static IsProfile` and `MapCallPattern.HasNoSideEffects` are reused).
- The fix is all-or-nothing (`AutoMapperFix.AllOrNothing`): every Manual below blocks the whole solution, so the rules only report Manual where leaving the code would be wrong, and report **nothing** where the code keeps working (a profile-only configuration that escapes).

## Corpus (TAAK-244 clones, `<corpus>/…`, read-only)

`new MapperConfiguration` 57 hits / 14 projects, `AssertConfigurationIsValid` 18 / 7. Forms and the outcome with this pattern:

| Form (repo) | Shape | Outcome |
| --- | --- | --- |
| jason, neozhu `MappingTests` | fields `_configuration`/`_mapper`, ctor `new MapperConfiguration(c => c.AddMaps(Assembly.GetAssembly(typeof(X))))` + `CreateMapper()`, `[Test] ShouldHaveValidConfiguration() { _configuration.AssertConfigurationIsValid(); }`, a test with `_mapper.Map(instance, source, destination)` | fields, ctor and validity test removed; but the IMapFrom convention (`profile.CreateMap(...)` outside a profile constructor) makes the check Manual, and the 3-argument `Map` is Manual (`CreateMapPattern`) — blocked either way |
| personalasst `MappingTests` + `MappingTestsFixture` (xUnit `IClassFixture`) | fixture with public `ConfigurationProvider`/`Mapper` properties; test class copies the property into a field and asserts in a `[Fact]` | `[Fact]` removed; fixture and field stay (public property: escapes) |
| personalasst `MapperMocker.GetMapper<TProfile>()`, skoruba `GetMapper()`, kavita `CreateMapper()` helper | returns `new MapperConfiguration(...).CreateMapper()` | nothing found (escapes; keeps working) |
| ombi `TvProfileMappingTests` | `private IMapper _mapper;` built in `[SetUp]`, only `_mapper.Map<B>(a)` | field and `[SetUp]` removed after the Map calls are rewritten |
| ombi `TvSearchEngineV2Tests`, kavita `AbstractDbTest`/`BackupServiceTests` | mapper passed to a service/`UnitOfWork` constructor | nothing found |
| jhipster (11 controller tests) | `_mapper = config.CreateMapper()` in a ctor with other statements, only `_mapper.Map<Dto>(entity)` | field + the two statements removed |
| nop `AutoMapperConfigurationTest` | `AutoMapperConfiguration.Init(config); AutoMapperConfiguration.MapperConfiguration.AssertConfigurationIsValid();` | the check statement removed; the configuration escapes (Init) |
| nop `NopEngine` | `foreach (var instance in instances) cfg.AddProfile(instance.GetType());`, registered | profile-only, escapes: nothing found |
| letterbook `MappingConfigProvider`/`FormsProfileProvider` + `[Fact] _mappingConfig.Posts.AssertConfigurationIsValid()` | public properties with profile-only configurations | providers: nothing; `[Fact]`s removed |
| skoruba production `*Mappers` | `static Mapper = new MapperConfiguration(cfg => cfg.AddProfile<P>()).CreateMapper()` (public static) | nothing found |
| gnomeshade `Startup` + tests | `options.AllowNullCollections = true; options.CreateMapsForV1_0();` | Manual (settings, maps outside profiles) |
| iotsharp `DevicesController` | inline `options.CreateMap<A, B>()` + `mapper.Map<A, B>(c)` | Manual |
| dasblog | `new MapperConfiguration(c => ConfigAction(sp, c))` | Manual |
| xipona converter test bases | `new MapperConfiguration(AddMapping)` (method group, inline maps in subclasses) | Manual |
| abp (out of scope) | `new MapperConfiguration(expression)`, `.Internal().AssertConfigurationIsValid(name)` | Manual |

## Choices

- **A standalone `x.AssertConfigurationIsValid();` → statement removed** (receiver and arguments without side effects: names, `this.x`, member chains, constants, `typeof`). After a successful (all-Fixed) run every `CreateMap` in a profile constructor is a hand-written method and the profile is empty, so the check would pass on nothing; a hand-written mapping is checked by the compiler, and a destination member that is not assigned is visible in the method.
- **…unless a map stays AutoMapper**: a `CreateMap`/`CreateProjection` outside a profile constructor (extension methods on `IProfileExpression`, `IMapFrom.Mapping`, a `MapperConfiguration` lambda) or an `[AutoMap]` attribute in the project or a project it depends on → Manual ("still has maps to check"). Profile constructors are claimed by the profile patterns, so "Fixed" there means rewritten. Only removals that contain a check do this scan.
- **A test method left with nothing** (every statement removable, or an expression body that is the check) → method removed. Test = an attribute of xUnit/NUnit/MSTest (`Fact`, `Theory`, `Test`, `TestCase`, `TestCaseSource`, `TestMethod`, `DataTestMethod`, also derived ones), and setups `SetUp`, `OneTimeSetUp`, `TestInitialize`. Not for `override`/`partial` methods. An empty test method proves nothing; nobody calls a test method.
- **The class's only constructor** (public, parameterless, no initializer, not static) left with nothing → removed (the implicit one is the same). Any other constructor keeps its remaining statements.
- **Test mapper / configuration variable** (local, or private field of a non-partial type, typed as an AutoMapper type) → declaration, assignments and field removed when: every value it gets is `new MapperConfiguration(<profile-only lambda>)`, `….CreateMapper()` of such a value, or another such variable; at least one value is built here; and every read is the check, `CreateMapper()` into another such variable, or the receiver of an AutoMapper `Map`/`ProjectTo` call (those calls belong to the Map patterns: if one stays Manual, all-or-nothing keeps the whole solution as it was). Variables are linked both ways (value source and target), so a mapper from an injected or non-profile configuration stays.
- **Profile-only** = a lambda whose body is only `AddProfile`/`AddProfiles`/`AddMaps` calls on its parameter (also inside a block or `foreach`). A profile type named by a type argument, `typeof(...)` or `new P()` must be source code of the solution: a package's profile keeps its maps → Manual.
- **A configuration that is not profile-only → Manual**, wherever it is (also DI registrations): its settings or own maps stay AutoMapper, and `MapCallPattern` would rewrite Map calls on its mapper to the profile's method, which ignores those settings (silent behaviour change).
- **A profile-only configuration that escapes** (passed to a constructor/method, returned, stored in a public property or static) → **not reported**. It keeps working with the emptied profiles; reporting it would block migrations that work today. It goes with the IMapper clean-up (see `AddAutoMapperPattern`'s "IMapper is still used").
- **A check with side effects in receiver/arguments, or not a statement of its own** (inside a lambda such as `Action act = () => mapper.ConfigurationProvider.AssertConfigurationIsValid();`, SampleShop, or an expression body of a non-test method) → Manual. Rare in the corpus (abp, SampleShop).
- **No `_ = p;` for a constructor parameter that only fed a removed field** (personalasst keeps `fixture` used; jhipster's ctors have no such parameter). `CS0414`/`CS0169` do not fire for a private field that is assigned but never read (checked with Roslyn on the proof outputs).
- Formatting: removing the first member(s) of a class leaves the blank line in front of the next member (the Fixer replaces nodes, not their neighbours' trivia). Usings that become unused (`System.Reflection` for `Assembly.GetAssembly`) stay.

## Steps

- [x] 1. Research: corpus forms, current behaviour (no finding), Fixer order and all-or-nothing.
- [x] 2. Implementation: `ConfigurationTestPattern.cs`, `ConfigurationSyntax.cs`, one line in `AutoMapperFix.cs`.
- [x] 3. Proofs + unit tests.
- [x] 4. Plan, models, verify; INDEX/README/queue rows handed to the main session.
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.

## Result

- Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ConfigurationTestPattern.cs`, `ConfigurationSyntax.cs`, `src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs` (pattern added last).
- Proofs (Fixed): `ConfigurationTest/validity-test` (jason), `class-fixture` (personalasst), `local-configuration` (local config, `CreateMapper()` + `ConfigurationProvider`, expression-bodied `[Fact]`, static field), `setup-mapper` (ombi `[SetUp]`), `assert-statement` (nop), `_Combined/configuration-test-fixture` (fixture builds a mapper, maps with it, asserts validity → profile methods, `order.ToOrderDto()`, fixture gone). Proofs (Manual): `inline-create-map` (iotsharp), `settings` (gnomeshade), `method-group` (xipona), `unmigrated-map`, `side-effect-receiver`.
- Unit tests `src/Unshackle.Tests/Core/ConfigurationTestPatternTests.cs`: escaping mappers and an injected IMapper are not found; a package profile is Manual; a map in a referenced project blocks the check (solution scan); a check as a non-test expression body is Manual.
- Tests: `dotnet build` 0 warnings; `dotnet test` 1198/1198; SampleShop 38/38.
- Open: `AutoMapperFix.Package` (the package goes when no AutoMapper code is left; escaping configurations and injected `IMapper`s still need it); a check wrapped in `Should.NotThrow`/`Action` stays Manual.

## See also

- `claude/tasks/automapper-queue.md` — row 18
- `docs/projectinfo/patterns/automapper-configuration-test-plan.md`
- `docs/projectinfo/patterns/automapper-configuration-test-verify.md`
- `docs/projectinfo/architectuur/corpus-automapper.md` — the survey
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ConfigurationTestPattern.cs`
