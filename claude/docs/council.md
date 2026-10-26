# Decision council

When an agent faces a choice that is not settled by the code, a task or an earlier user decision, it does not decide alone and does not
wait for the user: it puts the choice to the council. The council is three sub-agents with different personalities. They give a verdict
each; when they disagree, the main session shares the other arguments in a new round until all three accept one joint decision.
Decisions are recorded below. The user can overrule any of them.

## Members

| Member | Personality | Asks |
| --- | --- | --- |
| **The Guardian** | Cautious, correctness-first, blunt. Rather Manual than a rewrite that may break at run time. | Can this silently change behaviour in the user's code? |
| **The Pragmatist** | User advocate, results-driven, numbers-focused. A clear warning often beats refusing. | How much hand work does the developer have left? |
| **The Architect** | Principled, calm, precise; thinks in predictable rules, single responsibility, GoF/refactoring.guru. | Can a user predict this rule from the report? |

## Procedure

1. The main session writes the decision(s) with context and file pointers, and spawns the three members (read-only, e.g. `Explore`), each with its personality in the prompt.
2. Each member returns a choice per decision with a short reasoning, plus concerns.
3. Unanimous → decided. Otherwise the main session sends each dissenting member the others' arguments and asks for one rule all can accept; that proposal goes back to the others (accept / reject / amendment) until all accept.
4. The main session records the decision here (date, votes, rule) and turns follow-up work into a task.

## Decisions

### 2026-10-19 — open points after TAAK-288..291

| # | Decision | Votes (G / P / A) | Joint decision |
| --- | --- | --- | --- |
| 1 | starter (MediatR): run-time assembly scan via public `PublisherModule.AddPublisher(Assembly)` | Manual / Manual / Manual | **Principally manual.** The fix would change a public API and move registration to hosts we cannot see. |
| 2 | fullcart (MediatR): Scrutor `Decorate(typeof(INotificationHandler<>), …)` | partial / partial / partial | **Stays partial.** Under Mediator's generator the decorator becomes a plain handler: silent behaviour change. The report names the decorator. |
| 3 | unused-imapper: remove `GetService<IMapper>()` + null-guard | keep lookup / remove / remove → round 2 | **Scoped removal** (see below). |
| 4 | Empty constructors and factory classes left after unused-imapper | leave / leave + Info / leave + Info | **Leave them; report an Info line** ("`MapperHelper` is now empty"). Removing types is a separate refactoring; an opt-in rule later if users ask. |

**Decision 3, joint rule (round 2).** Remove the lookup and its null-guard, with a warning naming file:line, only when all hold:

1. an `AddAutoMapper` call is in the lookup's own project or in a project that transitively depends on it (`GetProjectsThatTransitivelyDependOnThisProject`);
2. that call is unconditional (not inside `if`, `?:`, `switch`, `#if` or a loop);
3. no project in that set has a `new ServiceCollection()`, `Host.CreateApplicationBuilder` or `WebApplication.CreateBuilder` whose own method body lacks `AddAutoMapper` (purely syntactic, Architect's amendment).

Otherwise keep the lookup; the report says which condition failed, with file:line (Pragmatist's amendment). This replaces the solution-wide `bool registered` in `MapperCleanup`.

**Concerns raised for 0.2.0** (follow-up in TAAK-292):

- Rerun the blind corpus on current main (the table predates the redone TAAK-288..290); any "build fails after fix" blocks the release.
- `MapperCleanup` plan cache keyed by documentation comment id: test overloads/partial types across projects; check no stale plan after earlier fixes in the same run.
- Removing call-site arguments of a public constructor is an API change for external consumers: consider limiting to non-public types or non-packable projects.
- Enforce the order call patterns → unused-imapper → AddAutoMapper explicitly; one end-to-end test where a single remaining `IMapper` use keeps the registration.
- Release notes list Fixed/Manual per library and every Manual/partial case.

### 2026-10-19 — implementer questions from TAAK-292 (round 3, unanimous after amendments)

| # | Question | Joint decision |
| --- | --- | --- |
| Q1 | Info line in the model | **Separate optional `FixedResult.Info`**, not counted as a warning; also in the JSON/markdown output. Putting it in `Warning` would make warning counts untrustworthy. |
| Q2 | How a kept lookup is reported | **Manual finding on the lookup call, not undoing the rest of the fix, with a typed reason code** (Architect) instead of a string prefix; the renderer prints `Stays on AutoMapper:`, and a test pins that rendered text (Guardian). The text names the failed rule with file:line. |
| Q3 | Literal reading of the rules | Accepted, with two changes. (a) A target-typed `new()` counts as a container through its converted type (`IServiceCollection`/`ServiceCollection`): not recognising it errs the unsafe way. (b) An `AddAutoMapper` inside a lambda counts as unconditional only when that lambda is passed directly to an unconditional registration or builder call; otherwise the lookup is kept, with reason code `ConditionalOrDeferredRegistration` and the file:line of the `AddAutoMapper` call/lambda. Inactive `#if` code fails rule 1, and a container filled in another method keeps the lookup. |
| Q4 | Which removals warn | **Every removed lookup warns**, `GetRequiredService` included: removing it changes how a mis-wired host fails. |
| — | fullcart reason text says "Autofac's RegisterGenericDecorator" | **Fix before 0.2.0**: name Scrutor's `Decorate(typeof(INotificationHandler<>), …)`. |

Condition (Pragmatist): rerun the corpus `after` on the six AutoMapper repos, 0 regressions. Implementation: TAAK-293.

### 2026-10-19 — implementer questions from TAAK-293 (round 4, unanimous)

| # | Question | Joint decision |
| --- | --- | --- |
| Q1 | `AddAutoMapper` inside a local function | **Deferred: keep the lookup.** A local function may never run, and it is not a lambda passed directly to a registration call. |
| Q2 | Other kept findings still use a string prefix | **Move to reason codes after 0.2.0** (TAAK-295). Users see no change; the tests pin the rendered text. |
| Q3 | Recognising registration/builder types | **Match on the fully qualified metadata name, namespace included** (`Microsoft.Extensions.*`, `Microsoft.AspNetCore.*`, `Microsoft.Azure.Functions.*`), not on a bare name and not on the assembly. A lookalike name would wrongly remove a lookup. Add `HostApplicationBuilder` and the isolated Functions builders (`FunctionsApplicationBuilder`, `IFunctionsWorkerApplicationBuilder`). Before 0.2.0. |
| Q4 | Scrutor recognised by syntax only | **Require that `Decorate` resolves to a method in the `Scrutor` namespace**; use the syntax match only when the symbol does not resolve. A user's own `Decorate` would otherwise cause a false partial. Before 0.2.0. |

Nothing here blocks 0.2.0 outright; Q3 and Q4 are small and land before it (TAAK-294), with the corpus `after` at 0 regressions.

### 2026-10-19 — implementer questions from TAAK-294 (round 5, unanimous)

| # | Question | Joint decision |
| --- | --- | --- |
| Q1 | Scrutor 4.2.2 declares `Decorate` in `Microsoft.Extensions.DependencyInjection` (assembly `Scrutor`) | **Namespace `Scrutor` OR assembly `Scrutor`**, syntax match only when the symbol is unresolved. Corrects the round-4 wording. |
| Q2 | `IsContainer` matches `ServiceCollection`/`IServiceCollection` by bare name | **Leave for 0.2.0** (a lookalike only keeps more lookups); qualify for consistency after 0.2.0 (added to TAAK-295). |

### 2026-10-21 — open questions from TAAK-295 and TAAK-297 (round 6, unanimous after round 2)

| # | Question | Votes round 1 (G / P / A) | Joint decision |
| --- | --- | --- | --- |
| Q1 | `Stays on AutoMapper: ` lives twice (`ReportRow.KeptPrefix` in DataAccess, `MapOwners.KeptPrefix` in Core) | one in Core / one in Core / one in Core | **One constant and one rendering in Core.** `ReportRow` calls `MapOwners.Rendered` (or a shared Core helper) and its own `KeptPrefix` is deleted. Output unchanged: corpus diff must be zero. |
| Q2 | MediatR "Stays on MediatR next to Mediator:" is a plain string | leave / code / code → round 2 | **Give it a `KeptReason` code, rendered through a per-fix prefix**, with byte-identical rendered text pinned by a test. Amendment (Guardian, Architect): before merging, list every branch on `Kept != None` (report grouping, counts, filters, summaries, `AllOrNothing`, partial plan) and prove with a test per branch that it behaves exactly as it did when that row had `Kept == None` (tightening, Architect; accepted by all). The zero-diff gate covers every report output, not only the reason text. `AllOrNothing` and the MediatR partial plan stay as they are. |
| Q3 | `MapBlockers` quotes a kept reason with prefix inside another finding's reason | keep / drop / keep → round 2 | **Keep the quote with its prefix**: one fact, one wording. Add a test that the outer reason contains the prefix exactly once (the outer row is not kept). The Pragmatist withdrew "drop" after seeing the prefix is not doubled. |
| Q4 | Example IMapper user in the kept `add-automapper` row differs between runs | stable / stable / stable | **Pick the first user by file path (ordinal, independent of the path separator), then line**, in both `Rewrite` and `RewriteAsync`. This changes rendered text: a one-time, reviewed corpus baseline refresh, recorded in the commit. |
| Q5 | `ThrowIfNull` in the public members of fix classes (`FluentAssertionsFix` constructor, `MediatRFix.PlanPartialAsync`) | keep / keep / keep | **Keep them: they are entry points** (built at the composition root; `PlanPartialAsync` is `IFix` surface the Fixer calls). Add "fix constructors and public `IFix` members" to the entry-point list in `code-standaarden.md` §7 (Architect). |

Follow-up: TAAK-300.

### 2026-10-21 — parallel file processing and the pre-filter (TAAK-298, round 7, unanimous after round 2)

| # | Question | Votes round 1 (G / P / A) | Joint decision |
| --- | --- | --- | --- |
| D1 | How many files at once | fixed 4 / ProcessorCount / min(ProcessorCount, 8) → round 2 | **min(ProcessorCount, 8)**, one named default (`Fixer.DefaultMaxDegree`), overridable with `FixRequest.MaxDegree` / `--max-degree N` (Guardian's amendment: a user out of memory steps down without going fully sequential). Results per file in an array slot by file index, merged in file order. akka is measured at degree 1, 4 and 8 (wall time and peak working set, Pragmatist's amendment); if peak at 8 is more than 2× sequential, the default becomes 4. |
| D2 | Fixes whose caches are not thread-safe (AutoMapper: one `MapOwners`/`MapperCleanup` entry per SolutionId, mutable `MapOwners.Rewrites`) | opt-in / opt-in / opt-in | **Opt-in `IFix.FixesFilesInParallel`, default false.** FluentAssertions and MediatR opt in; AutoMapper stays sequential until a follow-up makes its caches thread-safe (its files belong to TAAK-300 now). The flag promises that every static cache and shared mutable field is thread-safe and answers the same whichever file asks first. |
| D3 | Default and control | parallel only once proven / parallel + `--sequential` / parallel + `--sequential` | **Parallel by default once the identity proof below is green; `--sequential` stays permanently.** One rule in one place (Architect): parallel only when `!request.Sequential && fix.FixesFilesInParallel`. |
| D4 | Proof of identical output | unit + corpus + 5× varied / unit + corpus (+ files) / unit + SampleShop + corpus | **All of it:** unit tests on multi-file solutions of all three fixes (pre-filter on/off, sequential, parallel at degrees 2, 3, 4, 8 and default), the inactive-branch cleanup over two target frameworks, a shuffled-completion test repeated 5× at varied degrees; a SampleShop integration test (scan sequential vs parallel, all three fixes); the corpus compares report.json and the rewritten files byte for byte between sequential and parallel. |
| D5 | Pre-filter contract `IFix.FindsOnlyOldLibraryCode` | true, reluctantly / true / true | **Default true**, the contract in the XML doc, with a guard: the identity tests and the corpus compare `CheckEveryFile` on and off for every fix, so a fix that breaks the contract fails loudly. |

### 2026-10-22 — round 8: Moq, Polly, EPPlus, MassTransit, manual reduction, new libraries

Questions: `claude/tasks/COUNCIL-round-8.md` (sources linked there; Polly P1–P7 in `corpus-polly.md`). Two rounds; all joint.

**General rule (all fixes).** Fixed means the behaviour is proven the same. A shape is Fixed only when it is literal and visible in
one place, and a proof pins the behaviour that could change (a proof that passes either way proves nothing). Everything else is
`Manual` with a typed reason code and file:line. The old package is removed per project only when that project has 0 Manual rows.

| # | Decision | Joint rule |
| --- | --- | --- |
| M1 | Moq `MockBehavior.Strict` | Manual (`StrictBehaviour`); explicit `Loose`/`Default` is dropped. |
| M2 | `SetupSequence` | Fixed to `Returns(a, b, c)` only when every step is `Returns`/`ReturnsAsync`, with a warning that NSubstitute repeats the last value after the sequence; any other step makes it Manual. |
| M3 | `Protected()` | Manual; the reason names the `HttpMessageHandler.SendAsync` fake-handler recipe. |
| M4 | Verify family | `Verifiable()`+`Verify()` and `VerifyAll` expand to one `Received()` per covered setup (VerifyAll: all setups) only when every setup on that mock is in the same test method, the mock is not set up elsewhere (constructor, `[SetUp]`, helper, base class, field) and no setup is conditional or in a loop; each expansion warns with file:line. Otherwise Manual (`VerifyScopeNotLocal`). `VerifyNoOtherCalls` is Manual. |
| M5 | Target / AutoMocker | NSubstitute only. AutoMocker/AutoMoq Manual; its own design later. |
| P1 | Polly target | `Microsoft.Extensions.(Http.)Resilience` (Polly.Core via Microsoft), not a fork. |
| P2 | Delays | The v7 delay lambda is carried over as `DelayGenerator`; a backoff enum only for a constant delay. |
| P3 | `onRetry` | Fixed only with a 1:1 parameter mapping, emitting `args.AttemptNumber + 1`; a body using `context` is Manual. |
| P4 | `Wrap` order | Fixed only when every operand is a policy built inline or in a once-assigned local from a literal chain; the builder adds them outermost first; a proof per arity (2, 3) fails if the order is reversed. Otherwise Manual (`WrapOrderNotProvable`). |
| P5 | Polly 8 still on the v7 API | In scope: fixes apply to the API shape, not the package version; the report states the version found. |
| P6 | Transitive or CPM-only reference | In scope for the code rewrite; only declared references are edited (CPM through the project-file step, S3); an Info line says the OSMF status of transitive use is unconfirmed (#3183). |
| P7 | README wording | "Future majors plus v7 API debt"; no "fee from 16 Nov" claim; note the conflicting licence statements. |
| E1 | Cell-compare harness | Mandatory proof part for every pattern touching values or styles, in Unshackle.Tests (never Core): before on EPPlus 4.5.3.3, after on ClosedXML, compare per cell. |
| E2 | Test types | EPPlus 4.5.3.3 only; 5+ members matched by syntax when the symbol does not resolve; unresolved 5+ members Manual. |
| E3 | Integer worksheet index | Rewritten by the project's declared EPPlus version (4.x 1-based, 5+ 0-based); unknown or range version → Manual (`UnknownEPPlusVersion`). |
| E4 | `object` values | Fixed only when the static type, or a provably closed set of run-time types, is covered by harness proofs (string, double, int, bool, DateTime, decimal, enum, null); otherwise Manual (`UncoveredRuntimeType`). |
| E5 | Target | ClosedXML 0.105.x (MIT; confirm no licence check from a primary source before the skeleton ships). |
| E6 | Wrapper libraries | Manual by design when a public/protected signature in a packable project exposes EPPlus (`PublicApiExposesOldLibrary`). |
| T1–T2 | MassTransit target and timing | A report-only detector now; a package-ID swap to OpenTransit only once it ships on NuGet; re-check in Q1 2027. |
| T3–T4 | Consumer rewrites | No rewrite without working registration, no TODO skeletons; a routing change is never Fixed. |
| T5 | Test types | MassTransit 8.5.x (Apache-2.0) only; no Wolverine/Rebus until a target is chosen. |
| A1 | TAAK-311 mocked IMapper | Rewrite the `Setup(m => m.Map<X>(..))` only when the mock serves just maps that are all rewritten and the mocked return provably equals the generated mapping's result (or the test only asserts pass-through); each rewritten test warns. Otherwise keep the mock, Manual row naming the test. |
| A2 | TAAK-312 assembly scan | Fixed when every caller in the solution passes the same literal `typeof(X).Assembly` and the method is non-public or in a non-packable project; callers in inactive `#if` or unknown lambdas count as unknown. Otherwise Manual (`AssemblyNotStaticallyKnown`). |
| A3 | TAAK-313 nullable source | `src is null ? null! : src.ToX()` only where AutoMapper provably returns null; config like `AllowNullDestinationValues` → Manual. Re-measure the corpus first. |
| A4 | TAAK-314 config-lambda maps | Fixed: a lambda passed directly to `new MapperConfiguration` is treated as a profile body (round 3 Q3(b) rule); stored or conditional lambdas Manual. Re-measure first. |
| A5 | TAAK-315 generic messages | Stays justified Manual until Mediator's generator supports generics. |
| S1 | Scope | In: licence, sponsor and EOL/abandoned ("a third party forces you off this library"). Out: forced upgrades without that trigger (xUnit v3, NUnit 4) and preference migrations (Swashbuckle, Newtonsoft). |
| S2 | Report-only fixes | Allowed as their own kind: the fix declares itself report-only, its patterns return only Manual/Info, the Fixer touches no packages, the report header says "inventory only"; not counted as Fixed. |
| S3 | Non-C# edits | An `IProjectFileMigration` Strategy per fix, a closed set of exact typed edits (package rename/remove/add, one MSBuild property, known config file rename/transform), applied by the Fixer after the document pass and only when the project's C# part has 0 blocking Manual rows; each edit reported with file:line; `OutputType`/`IsTestProject` always warn; a before/after snapshot proof per edit kind. Patterns never write files. |
| S4 | Order after step 10 | 11 SpecFlow → Reqnroll, 12 IdentityServer4 detector. iTextSharp 5 slice next only after a scout proves the read/extract/merge slice provable; EF bulk parked (Pragmatist accepted parking). No new library before Moq, Polly and EPPlus are at 100% and the MassTransit detector is done. |

### 2026-10-22 — round 9: Moq defaults, proofs, target version (moq.create)

Questions from the TAAK-307 skeleton. Two rounds; all joint (amendments of all three members adopted).

| # | Decision | Joint rule |
| --- | --- | --- |
| Q1 | Default values that differ (Moq null vs NSubstitute "" / auto-substitute) | Pin Moq's default for **every** member of the mocked type whose default differs (set from the type, deterministic), only for known defaults: string → null, interface/delegate → null. Members the test sets up explicitly get no pin and do not count. Any differing member outside the known list, or a settable property, makes the mock Manual (`DefaultValueDiffers`); never silently unpinned. More than `MockDefaults.MaxPins` (= 10) pins → Manual (`TooManyDefaultPins`), counted separately in corpus runs to revisit the threshold with numbers. Each pin is an Info line. Unresolvable type → Manual. |
| Q2 | Run-time proof | Required for every Moq pattern whose Fixed status depends on defaults/pins, argument matching, call counting, returns or throws: `before` runs on Moq and `after` on NSubstitute against the same consumer; compare return values, exceptions and Verify/Received pass/fail; include a case that must fail under both. Compile-only only for a pure syntax rename, stated explicitly in that pattern's plan. The harness is built first (next TAAK-307 sub-task); Q1 pins ship only with it. |
| Q3 | NSubstitute version | Always 6.2.0 (netstandard2.0 + net8.0 covers what 5.3.0 covers); a TFM that is not netstandard2.0-compatible → Manual (`TargetFrameworkUnsupported`). |
| Q4 | Unused mock | Fixed (`Substitute.For<T>()`), never removed; Info line "mock never used". |

### 2026-10-22 — round 10: reason codes, engine, Polly, MediatR, EPPlus

Questions: `claude/tasks/COUNCIL-round-10.md`. Two rounds; all joint (amendments adopted). Engine follow-up: TAAK-318.

| # | Decision | Joint rule |
| --- | --- | --- |
| C1 | Typed reason codes | `ManualResult` gets a reason-code field separate from `Kept` (blocker ≠ kept). Rendered text stays byte-identical, pinned by tests; zero-diff corpus gate. Polly/EPPlus/MediatR move their text prefixes onto it. |
| C2 | Fix-level info | An optional fix-level hook returns Info rows per project (e.g. the Polly version found); rendered like `PatternResult.Note`, never counted as a warning. |
| C3 | Blank first line | The engine removes only the blank line left by a removed first-line using; other leading trivia stays; idempotent; corpus diff only that change. |
| G1 | Report-only header | The header row stays for now with a distinct kind every count excludes, pinned by a test. A `FixReport` metadata field (rendered by every writer) is due before the first report-only fix ships in a release. |
| G2 | CPM old `PackageVersion` | Removed only when no project under that `Directory.Packages.props` uses it, it is not a `GlobalPackageReference`, and every project under it is loaded; otherwise kept with an Info line. |
| G3 | Shared `Directory.Build.props` declaration | Edited only when every project importing it is loaded and has 0 blocking rows; otherwise Manual `SharedDeclarationBlocked`, naming the blocking project. |
| G4 | What blocks file edits | Any blocking document row, and any project-level row with its own blocking reason (`TargetFrameworkUnsupported`, `SharedDeclarationBlocked`, `LicensedDependency`). A "package kept" row that is only a consequence of a block does not count on its own. |
| G5 | Config transforms | Literal replacements, each exactly once; the result must parse, and a proof shows the parsed JSON tree equals the original apart from the listed keys; otherwise Manual. A typed JSON-key rename edit is added when a corpus case needs it. |
| P8 | Direct `Polly.Core` | Removed when M.E.Resilience is added, the project has 0 blocking rows, and the direct version is ≤ the Polly.Core version M.E.Resilience brings; otherwise kept with an Info line. |
| P9 | Old TFMs | Manual per project (`TargetFrameworkUnsupported`); no older M.E.Resilience unless every proof runs against it. |
| P10 | Introduced names | Fresh names (`args1`, …) only after a Roslyn lookup at that position, and in nested lambdas/local functions of the rewritten span, finds no symbol of that name (locals, parameters, members, base members, statics, using aliases); otherwise `NameClash` Manual. |
| R1 | Packability | `IsPackable` read from MSBuild via DataAccess; unset → SDK default (libraries packable → Manual side). |
| R2 | Internal method without friend caller | Stays Fixed when there is no `InternalsVisibleTo` to an unloaded project (clarifies A2: its own project is statically known). |
| R3 | "Same literal" | Compared per resolved assembly symbol, never by type name; unresolvable → Manual. Proof with two types from one assembly. |
| E7 | SixLabors.Fonts 2.x licence | Always a Warning naming the licence and how to pin < 2.0. Pin through the project-file step only when the assets file is present and current and shows no requirement ≥ 2.0; missing/stale assets → Warning only; project already resolving 2.x → Manual `LicensedDependency`. |
| E8 | Column width | Manual `ColumnWidthDiffers` unless the E1 harness proves the read-back equal within a documented tolerance; no padding arithmetic. |
| E9 | Plain local without `using` | Accepted for `epplus.package`, with a proof for that shape. |

### 2026-10-22 — round 11: EPPlus index base, TAAK-311 follow-ups, MassTransit detector

Questions: `claude/tasks/COUNCIL-round-11.md`. Two rounds; all joint (amendments adopted). E10/E11 supersede the 4.x part of E3.

| # | Decision | Joint rule |
| --- | --- | --- |
| E10 | 4.x worksheet index by TFM | Fixed only when the project is not packable (R1), every TFM is one family (all net4x → 1-based `Worksheet(i)`; all netcoreapp/net5+ → 0-based `Worksheet(i + 1)`), every project that transitively depends on it is in the same family, and E11 is clean. netstandard or mixed → Manual `WorksheetIndexBaseNotProvable`. The report names the TFM; a test pins the netstandard exclusion. |
| E11 | `IsWorksheets1Based` override | The key in any config file of the solution, or in code (string literal or assignment), makes every integer index in projects that can see it Manual `WorksheetIndexConfigured`, also on 5+, without reading the value. The row names the key's file:line and says removing it makes the index fixable on the next run; an Info line says only config files and code are checked. |
| A6 | `Verify` on the mocked mapper | Manual `VerifyOnMapper` (no `Received()` equivalent remains); the reason suggests "assert the mapped result instead". `Times.Never`/`Exactly(n)` Manual too. |
| A7 | Stub with argument matcher | Fixed only when the source is built in the test, every asserted member was set in the canned stub from the same source member the generated map copies, and the stub's return is never compared to a hand-built object; otherwise Manual `MockedReturnNotProvable`. |
| A8 | Mock without stubs | Allowed only when the test asserts nothing on the mapped value, the method calling `Map` has no null check/null-conditional on it, and the value does not leave that method (not returned, passed or stored); Info "mock had no stubs; Map now returns a real object". Otherwise Manual. |
| T6 | `Publish`/`Send` | Info rows, only when the symbol resolves to a MassTransit type (`IPublishEndpoint`, `ISendEndpoint`, `ISendEndpointProvider`, `ConsumeContext`, `IBus`); name-only matches never reported. |
| T7 | Easy vs hard | Info only for a consumer using nothing but `ctx.Message` and cancellation; headers, Respond, Redeliver, Publish, a definition or any filter → Manual with a per-feature code. Classification table in the fix docs. Never Fixed. |

### 2026-10-22 — round 12: EPPlus reads and proofs, engine G6–G9, Moq, TAAK-311/310

Questions: `claude/tasks/COUNCIL-round-12.md`. Two rounds; all joint. Engine follow-up: TAAK-322.

| # | Decision | Joint rule |
| --- | --- | --- |
| E12 | Reading cells | `GetValue<T>()` with T ∈ {int, long, double, decimal, DateTime, bool}, read into a typed target (no `object`, no `ToString()`), is Fixed only with the full harness matrix (empty, null, bool, number, date, string cells: same result or same exception). `.Text`, `.Value` and `GetValue<string>()` stay Manual `CellReadDiffers`. Opening an existing workbook is Fixed only when every read in that method is Fixed. Allowed shapes listed in the pattern docs. |
| E13 | AutoFit | Manual for good (`AutoFitDiffers`); the reason names the font dependency. |
| E14 | Scaffold proofs | Full `.after.cs` proofs (the Fixer's real output) per Fixed shape; existing scaffold proofs are replaced before their patterns ship. |
| E15 | LoadFrom DateTime/enum | Harness cases per property type; Fixed only where value and number format compare equal; otherwise Manual `LoadFromTypeDiffers`. |
| E16 | Dimension with styled empty cells | Harness case with `XLCellsUsedOptions.All`; if equal to EPPlus, always rewrite with `All`; otherwise Manual when the file sets any style (`DimensionWithStyles`). No silent "values only". |
| G6 | Shared-declaration block | A blocked project gets no package migration at all (with TAAK-321). |
| G7 | CPM old version | A solution-level pass: plan all projects, decide once per `Directory.Packages.props`, then write; every row states the final outcome. |
| G8 | Comment before a removed first using | Kept: the leading trivia moves to the next node; idempotent, tested together with C3. |
| G9 | Pre-filter | Each fix can declare its old-library assemblies (or package ids); the pre-filter matches on referenced assembly, namespace as fallback. `FindsOnlyOldLibraryCode => false` stays an escape hatch. A contract test runs every fix with the pre-filter on and off (round 7 D5). |
| M6 | `SetupsOverlap` | Stays Manual. |
| M7 | Properties id | Row 5 gets its own `moq.properties` id (rename only). |
| A9 | Member read on the mapped value | Stays Manual (A8 strict). |
| A10 | `A` created in place | Counts as built in the test only when it is the only `A` in the method, created with literal or no arguments, and every asserted member still meets A7 (default-constructed members read by the test → Manual). |
| T8 | Filters outside the solution | Accepted; an Info line says filters registered outside the solution are not seen. |

## Round 13 (2026-10-26)

Full text: `claude/tasks/COUNCIL-round-13.md`.

| Id | Question | Decision |
| --- | --- | --- |
| A11 | Generic `Map<T>` | Close per type argument collected from callers (TAAK-193 style, depth 3); constraints only filter; else Manual `GenericMapOpenSet` (TAAK-324). |
| A12 | Type-parameter destination | Same set; `ProjectTo<T>` Fixed only when every member is Fixed and selected by `typeof(T)` before the query (TAAK-325). |
| E17 | EPPlus all-or-nothing | Partial migration per project, no new flag; `EpplusTypeCrossesProject` blocks (TAAK-326). |
| E18 | EPPlus next rows | Flow types (fields/parameters/returns) → address Rows/Columns → tables → drawings (TAAK-327). |
| M8 | Moq per-result ids | Yes, rename only (TAAK-328). |
| M9 | `Configure()` in output | Never; M6 stands. |
| E19 | EPPlus read mapping | E12 stands, no shim. |
| R4 | ReadyToRun | Not built; TAAK-305 deferred. |

## See also

- `claude/docs/agent-werkwijze.md` — task management and rules of conduct
- `claude/docs/overdracht.md` — handover
- `claude/tasks/archief/TAAK-292-council-follow-up.md` — implementation of these decisions
- `claude/tasks/archief/TAAK-293-council-round-3.md` — implementation of the round-3 decisions
- `claude/tasks/archief/TAAK-294-council-round-4.md` — round-4 changes before 0.2.0
- `claude/tasks/archief/TAAK-295-kept-reason-codes.md` — reason codes for all kept findings (after 0.2.0)
- `claude/tasks/archief/TAAK-300-council-round-6-followups.md` — follow-up of the round-6 decisions
