# Corpus run Moq (TAAK-307 row 14, lane L6)

Tool: `main` 0842b542 (all Moq rows 0-13), published to `corpus-moq/tool-0842b542`. Harness: `claude/tools/corpus-moq-run.sh` (base, scan, after per repo,
one at a time) over `corpus-moq-am.sh` (a copy of `corpus-am.sh` with `RUNNER_FORCE=vstest` for solutions that mix VSTest projects with an MTP marker:
fluentmigrator). Repos: `claude/tools/corpus-moq-list.txt`. Tables: `python3 -I claude/tools/corpus-moq-summary.py <corpus root>`.
Results and logs on the corpus disk: `<corpus-moq root>/_results/{base,scan,after}/<repo>` (the corpus disk, never /tmp).

Status: round 2 (Windows, tool `35ebcd01` = main after L2 merged, `-maxcpucount:4`). Repos in this round: the 5 cloned under `corpus-moq/src`
(fluentmigrator, jellyfin, ocelot, orchardcore, radarr); the other 8 rows of the list are not cloned on this machine.
Harness knobs added: `TRX_FLAG` (xunit v3 without the TrxReport extension needs `--report-xunit-trx`: orchardcore, otherwise MTP exit 5 = zero tests),
`fix` step timeout 3600 s like `scan` (orchardcore fix takes ~50 min), `RUNNER_FORCE=vstest` for ocelot, `-p:TreatWarningsAsErrors=false` for jellyfin
(baseline fails on new-SDK CA1819 analyzer errors, unrelated to Moq).

## Results (round 2)

| repo | baseline tests | Fixed / Manual | after | regressions |
| --- | --- | --- | --- | --- |
| fluentmigrator | 5603 pass / 0 fail | 628 / 1311 | build ok, tests run | 1 (B1, unchanged) |
| jellyfin | 4214 pass / 0 fail (with `TreatWarningsAsErrors=false`) | 787 / 1092 | BUILD FAILS: 4x CS1061 (B4) | n/a |
| ocelot | not runnable: MTP pin + VSTest Benchmarks project; with VSTest forced the MTP MSBuild targets refuse (.NET 10 SDK) | 1058 / 567 | BUILD FAILS: 684x CS0246 (B2) | n/a |
| orchardcore | 4228 pass / 180 fail (Docker/Azurite tests) with `--report-xunit-trx` | 1195 / 952 | BUILD FAILS: 54 errors (B5, B6, B7) | n/a |
| radarr | Core 4127 pass / 3 fail, Common 5 fail, Automation 6 fail (environment) | 121 / 1774 | BUILD FAILS: 2x IDE0005 (B3) | n/a |

Only fluentmigrator reaches the test comparison; the other four stop at `build1`. Every build failure is a tool bug (B2-B7), none is environmental.

## Bugs and findings (repro, not fixed)

### B1 fluentmigrator: a test that asserts the runtime proxy type name

`MigrationRunnerTests.CanListVersions` (`test/FluentMigrator.Tests/Unit/MigrationRunnerTests.cs:642`) expects log lines such as `2011010101: IMigrationProxy`:
the name of the Castle proxy class Moq generates for `new Mock<IMigration>().Object`. After `moq.create`, `Substitute.For<IMigration>()` is a
differently named proxy, so the test fails (the only regression of the repo: 5605 pass before, 5604 pass and 1 fail after).
Not provable as equal: the type name of the mock object differs between the libraries. Candidate: `moq.create` reports Manual (or an Info line) when
`.Object` flows into code that is not visible (here `MigrationInfo(.., mockMigration.Object)` whose `ToString()`/`GetType().Name` is asserted); needs a council decision
(detecting it statically is hard: any `.Object` can reach a `GetType()`).

### H1 ocelot: restore fails on the first attempt

`restore0` NU1102 on `manual/Ocelot.ManualTest.csproj` (packages found on nuget.org but not resolvable: transient under load); retried at the end of the run.

### B2 ocelot: Moq PackageReference removed from a shared helper project while dependents still use Moq (BUILD-AFTER-FIX-FAILS, 684 CS0246)

`testing/Ocelot.Testing.csproj:38` has `<PackageReference Include="Moq" Version="4.21.0" />`; `unit/Ocelot.UnitTests.csproj` and
`acceptance/Ocelot.Acceptance.csproj` have no own Moq reference and get Moq transitively via `<ProjectReference Include="..	esting\Ocelot.Testing.csproj" />`
(plus `global using Moq;` in `acceptance/Usings.cs`). The report says `unit` and `acceptance` csproj: Manual "Moq is still used in this project, so its
package and global usings stay", but `testing/Ocelot.Testing.csproj`: project-file Fixed, i.e. the Moq package is removed there. Result: every remaining
`Mock<>`/`Times`/`using Moq` in unit and acceptance fails with CS0246 (e.g. `acceptance/ErrorHandling/ReturnsErrorTests.cs(82,17)`).
Expected: the project-file step keeps a Moq PackageReference in a project when any project that references it (transitively) still uses Moq;
or it adds the Moq reference to the dependents that still need it. Actual: removed per project, ignoring dependents.
Also odd: `acceptance/Ocelot.Acceptance.csproj` gets two project-file rows (one Manual, one Fixed) in the same report.

### B3 radarr: `using Moq;` kept although nothing left in the file needs the namespace (BUILD-AFTER-FIX-FAILS, IDE0005 as error)

`src/NzbDrone.Core.Test/Messaging/Events/EventAggregatorFixture.cs`: all `new Mock<IHandle<EventA>>()` fields, `.Object` and `Verify(.., Times.Once())`
are Fixed; only the two `Mocker.GetMock<IServiceFactory>().Setup(c => c.BuildAll<IHandle<EventA>>()).Returns(...)` (lines 29-34) stay Manual
(MockPassedAround). Those calls are instance members on the `Mock<T>` that Radarr's `Mocker` (AutoMoqer) returns and need no `using Moq;`.
After the fix `using Moq;` (line 3) stays, and Radarr builds with IDE0005 as error: `EventAggregatorFixture.cs(3,1): error IDE0005`.
Expected: when Manual findings remain, keep `using Moq;` only if a remaining node still binds to a symbol of the `Moq` namespace by its simple name
(`Mock<>`, `It`, `Times`, `MockBehavior`, ...); otherwise remove it. Actual: kept whenever any Manual finding remains in the file.

### B4 jellyfin: a mock that comes in through an `out var` argument is rewritten as a local substitute

`tests/Jellyfin.Api.Tests/Controllers/SubtitleControllerTests.cs`:
```csharp
private static SubtitleController CreateController(string codec, string subtitlePath, Mock<ISubtitleEncoder> subtitleEncoder, out Mock<IFileSystem> fileSystem)
{ ... fileSystem = new Mock<IFileSystem>(); ... }                // line 125: moq.create Manual (MockPassedAround)

var controller = CreateController(codec, path, encoder, out var fileSystem);   // line 40
fileSystem.Setup(f => f.FileExists(path)).Returns(true);                       // line 41: moq.setup-returns Fixed
```
Report: line 41 `moq.create` and `moq.setup-returns` Fixed. After: `fileSystem.FileExists(path).Returns(true)` on a variable that is still `Mock<IFileSystem>`:
`SubtitleControllerTests.cs(43,20): error CS1061: 'Mock<IFileSystem>' does not contain a definition for 'FileExists'` (also line 60).
Expected: a variable declared by `out var x` / `out Mock<T> x` (an out argument) is a mock created elsewhere: Manual (MockPassedAround),
like a `Mock<T>` parameter. Actual: treated as a local mock and its Setup rewritten.

### B5 orchardcore: a mock that comes in through tuple deconstruction is rewritten as a local substitute

`test/OrchardCore.Tests.Integration/MediaAzure/MediaGalleryCapabilitiesFilterTests.cs`:
```csharp
private static (Mock<INotifier> Notifier, MediaGalleryCapabilitiesFilter Filter) CreateFilter(BlobFileStore store) { var notifier = new Mock<INotifier>(); ... return (notifier, ...); }
var (notifier, filter) = CreateFilter(store);                                                     // line 94
notifier.Verify(n => n.AddAsync(NotifyType.Warning, It.IsAny<LocalizedHtmlString>()), Times.Once); // line 99
```
After: `notifier.Received(1).AddAsync(...)` on a `Mock<INotifier>`: `error CS1061: 'Mock<INotifier>' does not contain a definition for 'AddAsync'`.
Same in `OrchardCore.Tests/Modules/OrchardCore.Rules/JavascriptConditionDisplayDriverTests.cs:35` (`var (driver, notifier) = CreateDriver();`),
`OrchardCore.Admin/AdminListBuilderTests.cs:139` (`var (builder, _, columnsBuilder) = CreateBuilder(..)`, Verify inside a loop) and
`OrchardCore.Contents/AdminControllerTests.cs:34` (`.Setup(service => service.QueryAsync(..))` on the deconstructed `QueryService`; then CS1503 at line 40
because the deconstructed `Mock<IContentsAdminListQueryService>` is passed where the interface is expected).
Expected: a variable from a deconstruction (`var (a, b) = M()`) whose type is `Mock<T>` is Manual (MockPassedAround). Actual: rewritten.

### B6 orchardcore: generated type names are namespace-relative and rebind inside the test's namespace

Pinned defaults / arguments emitted with a namespace-qualified name that the file's own namespace shadows:
- `OrchardCore.Tests/Data/Migration/DataMigrationManagerTests.cs` (namespace `OrchardCore.Data.Migration.Tests`): emitted `YesSql.Indexes.IIndexProvider`
  binds to `OrchardCore.Data.YesSql`: `(123,62): error CS0234: 'Indexes' does not exist in the namespace 'OrchardCore.Data.YesSql'`.
- `OrchardCore.Tests/Modules/OrchardCore.Contents/AdminControllerTests.cs` (namespace `OrchardCore.Tests.Modules.OrchardCore.Contents`) lines 123-124 and
  `OrchardCore.Title/TitlePartHandlerTests.cs:110`: emitted `OrchardCore.ContentManagement.Metadata.Models.ContentTypeDefinition` binds to
  `OrchardCore.Tests.Modules.OrchardCore`: `error CS0234: 'ContentManagement' does not exist in the namespace 'OrchardCore.Tests.Modules.OrchardCore'`.
Expected: emit `global::`-qualified names (then simplify with the Simplifier against the file's semantic model), or the minimal name valid at
that position (`ToMinimalDisplayString(model, position)`). Actual: fully qualified without `global::`.

### B7 orchardcore: a literal `null` argument becomes an ambiguous `Arg.Is<T>(null)` (CS0121)

When a call mixes matchers and a literal `null` (NSubstitute needs all arguments as specs), `null` becomes `Arg.Is<T>(null)`, which is ambiguous between
`Arg.Is<T>(T)` and `Arg.Is<T>(Expression<Predicate<T>>)`:
```csharp
// OrchardCore.Tests/Modules/OrchardCore.Contents/ContentQuickNavigationTests.cs ~249
_authorization.Verify(service => service.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), null, It.Is<IEnumerable<IAuthorizationRequirement>>(requirements => ...)), ...);
// OrchardCore.Markdown/MarkdownMigrationTests.cs ~62 (logger): It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception, string>>()
```
`error CS0121: The call is ambiguous between 'NSubstitute.Arg.Is<T>(T)' and 'NSubstitute.Arg.Is<T>(Expression<Predicate<T>>)'`; also
`FacebookPluginPartSecurityTests.cs:31`, `LiquidSecurityTests.cs:48`, `AttachedMediaFieldFileServiceTests.cs:138`.
Expected: `Arg.Is<T>((T)null)` (or `Arg.Is((T)null)`), which binds to the value overload. Actual: `Arg.Is<T>(null)`.
(Inferred from the CS0121 overload pair and the `null` arguments in the source; the after-file itself is deleted by the harness.)

### H2 orchardcore: scan/fix take ~40-50 min (2147 findings), more than the old 1800 s harness timeout for `fix`.

## Fixed / Manual counts per repo

(round 1 `report.json`; fluentmigrator: 628 Fixed, 1299 Manual; Manual reasons: ClassMockSetup 524, DefaultValueDiffers 266, MockUsedBeyondObject 233,
MockPassedAround 109, ClassMockNoCallBase 59, MatcherTypeDiffers 39, CallShape 28, MockGet 17, TooManyDefaultPins 12, StrictBehaviour 3.)

## See also

- `claude/tasks/TAAK-307-moq-to-nsubstitute.md`
- `claude/tasks/moq-queue.md`
- `docs/projectinfo/architectuur/corpus-moq.md`
