# Corpus: Moq 4.x API usage in 13 open-source projects

Measured on 2026-10-22 for TAAK-307 step 1 (survey). Model: `corpus-automapper.md`.

## Method

- 13 repos on Moq 4.7.99–4.21.0. Five new shallow clones at a pinned commit in `<corpus>\corpus-moq\src\<name>\`;
  eight reuse the AutoMapper/FA corpus clones at the same sha (`<corpus>\corpus-automapper\src\`,
  `<corpus>\corpus-fa\src\eventflow`). List: `claude/tools/corpus-moq-repos.txt` (`<name> <repo> <sha> <solution>`).
- Measurement: a regex script over `*.cs` files that mention `Moq`, `Mock<` or `Mock.Of` (comments stripped; `bin`, `obj`,
  `wwwroot`, `*.g.cs`, `*.Designer.cs` skipped). Counting `Mock<` too catches projects with a global `using Moq;`
  (Ocelot, OrchardCore). No Roslyn: numbers are indicative. Script and raw output live outside the repo:
  `python -I <corpus>\corpus-moq\tools\measure.py <out.json> <src dir>...` (`tools\moq.json`, `tools\table.txt`).
- Builds: `dotnet build <solution> -m:2` with SDK 10.0.400 from inside the clone, `TEMP`/`TMP` on `D:`, 15 minute limit,
  one repo at a time (`tools\build.sh`, logs in `<corpus>\corpus-moq\logs\`). The reused repos keep the status from
  their earlier survey (same sha).

## Repos

| Name | Repo (sha) | Moq | TFM | Tests | Builds | Files | `Setup` | `Verify` |
| ---- | ---------- | --- | --- | ----- | ------ | ----: | ------: | -------: |
| fluentmigrator | `fluentmigrator/fluentmigrator` (`c7a11e9ec0ae`) | 4.20.72 | net48/netstandard2.0/net8+ | NUnit | yes | 66 | 224 | 141 |
| jellyfin | `jellyfin/jellyfin` (`c7ce6a1b56ee`) | 4.18.4 | net10.0 | xUnit v3, 17 | yes | 132 | 543 | 43 |
| ocelot | `ThreeMammals/Ocelot` (`7adbbcabeeff`) | 4.21.0 | net8.0/9.0/10.0 | xUnit v3 | yes | 93 | 334 | 206 |
| orchardcore | `OrchardCMS/OrchardCore` (`0f7975917a53`) | 4.20.72 | net10.0 | xUnit v3 | yes (from outside the clone: `global.json` pins SDK 10.0.402) | 135 | 469 | 114 |
| radarr | `Radarr/Radarr` (`56f6168bb6f2`) | 4.18.4 | net10.0 | NUnit, 10 | not yet: restore ran out of disk space on `C:` (NuGet cache); retry needed | 222 | 1041 | 545 |
| altinnauth | `Altinn/altinn-auth` (`2355aed31457`) | 4.21.0 | net10.0 | xUnit, 17; containers | yes (AutoMapper survey) | 52 | 416 | 69 |
| ombi | `Ombi-app/Ombi` (`697d35647bf8`) | 4.18.2/4.20.70 | net8.0 | NUnit, 7 | partial: `yarn install` step | 62 | 476 | 90 |
| personalasst | `davidtimovski/personal-assistant` (`ac7e1085c3db`) | 4.20.72 | net10.0 | xUnit, 2 | yes | 27 | 130 | 25 |
| letterbook | `Letterbook/Letterbook` (`dace8fbfb156`) | [4.18.4] | net9.0 | xUnit, 11 | yes | 38 | 281 | 114 |
| xipona | `Velociraptor45/Xipona` (`12677f35f17a`) | 4.18.4 | net9.0 | xUnit v3, 13 | yes | 134 | 8 | 4 |
| cats | `ministryofjustice/CFO-CaseAssessmentTrackingSystem` (`8e93a2a650e1`) | 4.21.0 | net10.0 | NUnit, 2 | partial: Aspire AppHost | 10 | 55 | 10 |
| skoruba | `skoruba/Duende.IdentityServer.Admin` (`3fee47255860`) | 4.20.72 | net9.0 | xUnit, 5 | yes | 10 | 4 | 0 |
| eventflow | `eventflow/EventFlow` (`e544b7629124`) | 4.7.99 | netstandard/net8 | NUnit | not tried: restore fails on NU1902/NU1903 (see `core.md`) | 35 | 74 | 45 |

Summary: 9 build (4 new + 5 reused), 2 partially, Radarr blocked by disk space, EventFlow not tried.

## Frequencies

Hits = regex matches across all 13 repos; "Projects" = repos with at least one hit. Practically all hits are in test paths.

| API / shape | Hits | Projects | Queue (draft) |
| ----------- | ---: | -------: | ------------- |
| `It.IsAny<T>()` | 5396 | 13 | 2 arg matchers |
| `.Setup(...)` | 4055 | 13 | 3 setup-returns |
| `.Object` | 3981 | 13 | 1 create |
| `.Returns(...)` | 3326 | 13 | 3 |
| `new Mock<T>()` | 2389 | 12 | 1 |
| `AutoMocker` / `Mocker.GetMock` (Radarr's own AutoMoq, Moq.AutoMock) | 1745 | 5 | 12 automock |
| `.Verify(x => ..., Times)` | 1406 | 12 | 4 verify |
| `.ReturnsAsync(...)` | 1139 | 10 | 3 |
| `Times.Once` | 819 | 11 | 4 |
| `Mock.Of<T>()` | 664 | 9 | 1 |
| `Mock<T>` as field/parameter type | 657 | 12 | 1 |
| `Times.Never` | 496 | 12 | 4 (`DidNotReceive`) |
| `It.Is<T>(pred)` | 485 | 10 | 2 |
| `.SetupGet(...)` | 443 | 8 | 5 properties |
| `.Returns(lambda)` (argument-dependent) | 302 | 10 | 7 returns-lambda |
| `.Throws(...)` | 257 | 8 | 6 throws |
| `new Mock<T>(MockBehavior.Strict)` | 232 | 8 | council |
| `.VerifyGet` / `.VerifySet` | 202 | 3 | 5 |
| `.Callback(...)` | 193 | 11 | 8 callback |
| `Times.Exactly(n)` | 114 | 9 | 4 |
| `Mock<ILogger<T>>` | 109 | 9 | 13 logger verify (`Verify(l => l.Log(...It.IsAnyType...))`) |
| `.VerifyNoOtherCalls()` | 101 | 5 | council |
| `.ThrowsAsync(...)` | 89 | 9 | 6 |
| `.Verifiable()` + `.Verify()` (no args) | 85 + 37 | 6 / 4 | council |
| `MockBehavior.Loose/Default` explicit | 76 | 5 | 1 (drop) |
| `It.IsAnyType` | 72 | 4 | 13 |
| `.SetupProperty` / `SetupAllProperties` | 70 | 4 | 5 |
| `.Protected()` | 53 | 5 | council |
| `It.IsIn/IsInRange/IsRegex/IsNotNull` | 32 | 2 | 2 |
| `.VerifyAll()` | 28 | 4 | council |
| `Mock.Get(obj)` | 23 | 3 | 1 |
| `new Mock<T>(ctor args)` | 23 | 6 | 9 partial/class mocks |
| `.As<T>()` | 21 | 3 | council |
| `Mock<HttpMessageHandler>` | 15 | 4 | council (with `Protected`) |
| `Times.AtLeastOnce` / `AtLeast` / `AtMost` / `Between` | 14 + 2 | 7 / 1 | 4 (`Received()`; ranges Manual) |
| `.SetupSequence(...)` | 10 | 3 | council |
| `CallBase = true` | 10 | 4 | 9 |
| `.Invocations` | 10 | 3 | 11 unsupported |
| `.Reset()` | 7 | 3 | 11 (`ClearReceivedCalls`) |
| `Mock<DbSet<T>>` / `.As<IQueryable<T>>` | 6 | 1 | 11 |
| `.Raise(...)` | 4 | 1 | 11 (`Raise.EventWith`) |
| `.SetupSet(...)` | 3 | 2 | 5 |
| `It.Ref<T>.IsAny` | 3 | 3 | 11 |
| `MockRepository`, `MockSequence`/`InSequence`, `Capture.In`, `DefaultValue.Mock`, `.Raises` | 0 | 0 | — |

Concentration: Radarr has 1041 `Setup` / 545 `Verify` and nearly all `Mocker.GetMock` (its own `TestBase.Mocker`,
an AutoMoq container); Ocelot, OrchardCore, Jellyfin, Ombi and Altinn follow. Xipona and Skoruba barely use Moq
directly.

## Draft queue (frequency-ranked)

| Row | Pattern | Covers |
| --: | ------- | ------ |
| 1 | `moq.create` | `new Mock<T>()` (+ explicit Loose), `Mock<T>` fields/params, `.Object`, `Mock.Of<T>()`, `Mock.Get` |
| 2 | `moq.arg-matchers` | `It.IsAny` → `Arg.Any`, `It.Is` → `Arg.Is`, `It.IsNotNull`/`IsIn`/`IsRegex` |
| 3 | `moq.setup-returns` | `Setup(...).Returns/ReturnsAsync(value)` → `sub.M(...).Returns(value)` |
| 4 | `moq.verify` | `Verify(..., Times.Once/Never/Exactly/AtLeastOnce)` → `Received(n)`/`DidNotReceive`/`Received()` |
| 5 | `moq.properties` | `SetupGet`, `SetupSet`, `SetupProperty`, `VerifyGet`/`VerifySet` |
| 6 | `moq.throws` | `Throws`/`ThrowsAsync` → `Throws`/`ThrowsAsync` (NSubstitute.ExceptionExtensions) |
| 7 | `moq.returns-lambda` | `Returns((a, b) => ...)` → `Returns(ci => ...)` with `ci.ArgAt<T>(i)` |
| 8 | `moq.callback` | `Callback(...)` → `AndDoes` / `When(...).Do(...)` |
| 9 | `moq.class-mocks` | `new Mock<T>(ctor args)`, `CallBase` → `Substitute.ForPartsOf<T>(args)` |
| 10 | strict/sequence/protected/verify-all | Manual or Fixed per council answer |
| 11 | `moq.unsupported` | `Invocations`, `Reset`, `Raise`, `It.Ref`, `DbSet` mocks: Manual (or `ClearReceivedCalls`, `Raise.EventWith`) |
| 12 | `moq.automock` | `AutoMocker` / AutoMoq customizations (`Mocker.GetMock<T>()`): needs its own design |
| 13 | `moq.logger-verify` | `Mock<ILogger<T>>` `Verify(l => l.Log(..., It.IsAnyType ...))`: very common idiom, hard shape |

## Open questions for the council

1. `MockBehavior.Strict` (232 / 8): Manual, or Fixed by dropping strictness (tests then pass where they failed before)?
2. `SetupSequence` (10 / 3): Fixed to `Returns(a, b, c)` when only `Returns` steps, Manual when it mixes `Throws`?
3. `Protected()` (53 / 5, mostly `HttpMessageHandler.SendAsync`): Manual, or a rewrite to a hand-written fake handler?
4. `VerifyAll` / `Verifiable()` + `Verify()` / `VerifyNoOtherCalls` (28 + 122 + 101): expand each verifiable setup into a
   `Received()` check, or Manual?
5. Target: NSubstitute only, or also offer FakeItEasy? And `AutoMocker` (1745 / 5): rewrite to
   `AutoFixture.AutoNSubstitute`/own helper, or Manual?

## See also

- `claude/tasks/TAAK-307-moq-to-nsubstitute.md` — the task
- `claude/tools/corpus-moq-repos.txt` — repos and pinned shas
- `docs/projectinfo/architectuur/corpus-automapper.md` — the model for this survey
