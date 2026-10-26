# Corpus run MediatR → Mediator (2026-10-11, main 40cf91e)

Real open-source MediatR projects put through `unshackle fix --fix mediatr --force`, with baseline and
after-fix build/test. Goal: measure how many real solutions the all-or-nothing migration (`MediatRFix.AllOrNothing`) completes fully.

## Setup

- Corpus: `scratchpad/corpus-mediatr/`, copies in `scratchpad/run-mediatr/work/<name>`, reports `run-mediatr/<name>.json`,
  logs `run-mediatr/logs/<name>[-trim|-sim]/`, scripts `scratchpad/scripts/run*.sh`.
- Environment: only .NET SDK 10.0.112 (runtimes 8/9/10). Therefore per copy: `global.json` → `10.0.100/latestFeature`
  (other keys kept), and on restore/build `-p:NuGetAudit=false` (NU1902 as an error with SDK 10),
  `-p:AllowMissingPrunePackageData=true` (NETSDK1226, distro SDK), `-p:SkipNSwag=True` (jason: the NSwag post-build starts the app).
  `DOCKER_HOST` pointed at a dead port: no Docker/Testcontainers/DB.
- **-trim**: side projects that do not load on Linux removed from the copy's solution (kgrzybek `_build`, `Database.*`;
  eShop MAUI `ClientApp`, `HybridApp`, `ClientApp.UnitTests`; eShopOnWeb `docker-compose.dcproj`).
- **-sim**: project files normalised to be semantically equal (`scripts/norm.py`: `/>` → ` />`, `" >` → `">`,
  `->` → `-&gt;` in attributes, multi-line attributes on one line) to measure what sits behind blocker TAAK-134.
  This is a simulation, not the behaviour of Unshackle itself.
- nopCommerce is in the corpus but does not use MediatR: skipped. evolutionary-architecture: chapters 1 and 2 (separate solutions).

## 1. Result per project (real run)

| Project                           | Loads?                                              | All-or-nothing blocked? (by)                                                                 | Fixed | Manual | Builds after fix?                                                       | Tests baseline → after fix                                     | MediatR gone?                     |
| --------------------------------- | --------------------------------------------------- | -------------------------------------------------------------------------------------------- | ----- | ------ | ----------------------------------------------------------------------- | -------------------------------------------------------------- | --------------------------------- |
| jasontaylor CleanArchitecture 8.0 | yes                                                 | yes: project-file (`Web.csproj`, `->` in attribute)                                          | 0     | 27     | yes (unchanged)                                                         | 16/41 → 16/41 (25 red: SQL Server/Playwright)                  | no                                |
| ardalis CleanArchitecture 9.1     | yes                                                 | **no**                                                                                       | 15    | 0      | **no**: CS0311 (`DomainEventBase` from Ardalis.SharedKernel is MediatR) | 9/9 → n/a (build fails)                                        | no (transitively via SharedKernel) |
| amantinband clean-architecture    | yes                                                 | yes: project-file (`Directory.Build.props` `" >`)                                            | 0     | 26     | yes (unchanged)                                                         | 85/85 → 85/85                                                  | no                                |
| nadirbad VerticalSlice            | yes                                                 | yes: project-file (`" >`), request-processor (`return Task.Run(...)`), generic notification  | 0     | 16     | baseline already red (CA1873, SDK 10 analyzers + TWAE)                  | –                                                              | no                                |
| evolutionary ch. 1                | yes                                                 | yes: project-file (multi-line attributes)                                                    | 0     | 6      | yes (unchanged)                                                         | 45/45 → 45/45                                                  | no                                |
| evolutionary ch. 2                | yes                                                 | yes: project-file (`/>` without space)                                                       | 0     | 21     | yes (unchanged)                                                         | 39/39 → 39/39                                                  | no                                |
| kgrzybek modular-monolith         | no → yes after trim                                 | yes: project-file (`"/>`), 12× generic message (10 false, TAAK-132)                          | 0     | 90     | yes (unchanged)                                                         | ~280 unit/arch green, 40 integration red (SQL Server) → equal  | no                                |
| eShopOnWeb                        | **no** (Razor components, `.dcproj`)                | –                                                                                            | –     | –      | –                                                                       | 74/74 → 74/74 (nothing changed)                                | no                                |
| dotnet/eShop                      | **no** (MAUI; after trim: Razor `WebApp.Components`) | –                                                                                           | –     | –      | baseline red (ASPIRE009, no Aspire CLI)                                 | 64/64 (MTP)                                                    | no                                |

**Fully migrated: 0 of 9.** One project (ardalis) got through all-or-nothing and then does not build.

### Simulation: what sits behind the project-file blocker (-sim)

| Project  | Fixed/Manual | Builds? | What then blocks or breaks                                                          |
| -------- | ------------ | ------- | ----------------------------------------------------------------------------------- |
| evo1     | 6 / 0        | no      | marker handlers missed (TAAK-130, CS0738); MSG0005 (TAAK-136)                       |
| evo2     | 22 / 0       | no      | `using Mediator` shadowed (TAAK-131, CS0246); handlers missed (TAAK-130)            |
| amantin  | 0 / 26       | –       | another csproj with mixed CRLF/LF (TAAK-134)                                        |
| jason    | 0 / 29       | –       | `#if` template code in `Web` holds on to `global using MediatR` ("inactive #if code") |
| nadirbad | 0 / 18       | –       | request-processor `Task.Run`, generic `DomainEventNotification<T>`                  |
| kgrzybek | 0 / 96       | –       | generic messages (2 real: generic decorator, `DomainNotificationBase<T>`)           |

## 2. What blocks all-or-nothing most often (number of projects)

| #   | Cause (Manual or load error)                                                                                                                                                       | Projects                                                           | Task     |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ | -------- |
| 1   | project-file: "cannot be edited without changing unrelated text" (`/>`, `" >`, `->`, multi-line attributes, mixed line endings; often in an unchanged `Directory.Build.props`)     | 6 (jason, amantin, nadirbad, evo1, evo2, kgrzybek)                 | TAAK-134 |
| 2   | solution does not load: Razor-generated types, `.dcproj`, non-loading side project without MediatR                                                                                 | 3 (eShopOnWeb, eShop, kgrzybek)                                    | TAAK-135 |
| 3   | `mediatr.unsupported` generic message/handler                                                                                                                                      | 2 (kgrzybek, nadirbad); for kgrzybek 10/12 false (abstract base)   | TAAK-132 |
| 4   | `usings`: inactive `#if` code holds `global using MediatR`                                                                                                                         | 1 (jason, `dotnet new` template)                                   | –        |
| 5   | `mediatr.request-processor`: `Process` does not return `Task.CompletedTask` (`return Task.Run(...)`)                                                                               | 1 (nadirbad)                                                       | action 7 |
| 6   | external package builds on MediatR (Ardalis.SharedKernel) — **not detected**, should be Manual                                                                                     | 1 (ardalis)                                                        | TAAK-133 |

Other categories did not occur. Conclusion: the value of the MediatR fix is now determined by
**infrastructure** (project files, loading), not by missing MediatR patterns.

## 3. Build/runtime errors after fix

| Project    | Error                                                             | Cause                                                                                                         |
| ---------- | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| ardalis    | CS0311 `ContributorDeletedEvent` is not a `Mediator.INotification` | base class from Ardalis.SharedKernel (depends on MediatR 12.1.1); the fix does not see that → TAAK-133      |
| evo1 (sim) | CS0738 handler does not implement `Handle`                        | marker interface already swapped in an earlier processed file; handler file no longer sees MediatR → TAAK-130 |
| evo1 (sim) | MSG0005 message without handler (as error)                        | Mediator generator + TreatWarningsAsErrors → TAAK-136                                                        |
| evo2 (sim) | CS0246 `INotification`, `IPublisher`; CS1961 (follow-up error)    | `using Mediator;` inside a namespace binds to own `...Infrastructure.Mediator` → TAAK-131                    |

Runtime: no solution got far enough for runtime behaviour to be measurable. ardalis test1 ran on old binaries (build failed) and does not count.
Environment errors (not Unshackle): NU1902, NETSDK1226, NSwag, CA1873 (nadirbad), ASPIRE009 (eShop), MAUI workloads, SQL Server/Docker integration tests.

## 4. Bug tasks

- TAAK-130 cross-file marker handler missed because of file order (silent: no Manual) — unit test, no proof possible
- TAAK-131 `using Mediator` shadowed — proof `Proofs/_Pending/MediatR/Usings/edge-namespace-shadow.*`
- TAAK-132 abstract generic base wrongly Manual — proof `Proofs/MediatR/RequestHandler/edge-abstract-generic-base.*` (done) (behaviour of Mediator 3.0.2 checked with a probe project)
- TAAK-133 external package on MediatR not detected — unit test with a second assembly
- TAAK-134 project-file round trip too strict + needless loading of `Directory.Build.props` — DataAccess tests
- TAAK-135 loader: Razor, `.dcproj`, non-loading side projects — done: eShopOnWeb builds after the fix, eShop/kgrzybek load
- TAAK-136 MSG0005 with TreatWarningsAsErrors

## 5. Top 10 actions (gain per effort)

| #   | Action                                                                             | Gain                                              | Effort       |
| --- | ---------------------------------------------------------------------------------- | ------------------------------------------------- | ------------ |
| 1   | TAAK-134 part 1: only load `Directory.Build.props`/files when they change          | unlocks evo1, evo2, nadirbad, amantin (props)     | small        |
| 2   | TAAK-134 part 2: splice writing instead of an XDocument round trip                 | removes the #1 blocker (6/9) completely           | medium       |
| 3   | TAAK-130: findings on the original semantics                                       | evo1/evo2 build; prevents silently broken builds  | medium       |
| 4   | TAAK-133: external MediatR type → Manual                                           | restores the all-or-nothing promise (ardalis)     | small        |
| 5   | TAAK-131: `global::Mediator` when shadowed                                         | evo2 builds                                       | small        |
| 6   | TAAK-132: exclude `IsAbstract`                                                     | 10 false Manuals gone (kgrzybek)                  | very small   |
| 7   | request-processor: `return <Task-expr>` → `return new ValueTask(<expr>)`           | nadirbad                                          | small        |
| 8   | TAAK-136: MSG0005 NoWarn                                                           | evo1/evo2 build with TWAE                         | small        |
| 9   | TAAK-135: loader tolerance (Razor, `.dcproj`, side projects)                       | eShopOnWeb, eShop, kgrzybek load                  | medium       |
| 10  | `#if` code: do not count inactive branches without MediatR identifiers as usage    | jason                                             | small–medium |

## Verdict

At this moment Unshackle migrates **0 of the 9** real solutions fully. Six are held back by the
project-file check and three do not load. The only project that passed all-or-nothing (ardalis) then does not
build. The MediatR patterns themselves are not the bottleneck: in the simulation without the project-file blocker
the fix rewrote evo1 and evo2 completely without Manual, and the builds broke on three concrete bugs (TAAK-130/131/136).
Expectation after actions 1–8: evo1, evo2 and presumably amantin migrate fully (3/9). ardalis then becomes an
honest Manual. nadirbad and kgrzybek rightly stay Manual (generic messages/decorators). jason, eShopOnWeb and
eShop depend on actions 9–10. A new corpus run must confirm this, with build **and** tests.

## Run 2026-10-12 (main 2800f23)

Repeated run after the MediatR queue (TAAK-130..136 done). Working folder `<corpus>/corpus-mediatr` (Windows, SDK 10.0.400),
harness `claude/tools/corpus-regression.sh` (copy with `py -3`; restore → build → test ×2 → `fix --fix mediatr --force` →
bin/obj removed → restore → build → test → per-test comparison). Every clone got a `nuget.config` with only nuget.org;
flags as before (global.json → 10.0.100/latestFeature, `NuGetAudit=false`, `AllowMissingPrunePackageData`, `SkipNSwag`).
Logs, reports and comparisons in `<corpus>/corpus-mediatr/_results/<name>/`.

Commits (MediatR ≤ 12): jason tag v8.0.0 `f2f0445` (12.1.1; HEAD has 14.2), ardalis tag v9.1.0 `ccb82ba` (12.2.0; HEAD
already uses Mediator 3), amantin `5822811` (12.2.0), nadirbad `feb3a15` (12.4.1), evolutionary `441a7cd` (12.5.0),
kgrzybek `91c8ef2` (12.2.0), eShopOnWeb `4da8212` (12.0.1), dotnet/eShop `59f1a6c` (2025-03-10, 12.4.1; HEAD has 13.1).
Environment interventions: nadirbad `.editorconfig` + `CA1873 = none` (SDK 10 analyzer, TWAE); kgrzybek `_build` + `Database/*`
removed from the solution (sqlproj on SDK 10); eShop `ClientApp`, `HybridApp`, `ClientApp.UnitTests` removed from the solution (MAUI).

| Project        | Restore | Build before | Test before        | Fix   | Fixed | Manual (root)                          | Migrated   | Build after | Test after | Regressions |
| -------------- | ------- | ------------ | ------------------ | ----- | ----: | -------------------------------------- | ---------- | ----------- | ---------- | ----------- |
| jason          | ok      | ok           | 39/41 (2 red)      | ok    |     0 | 29 (usings `#if` + project-file)       | no         | ok (unch.)  | 39/41      | 0           |
| ardalis        | ok      | ok           | 9/9                | ok    |     0 | 28 (10× SharedKernel, 1× add-mediatr)  | no (rightly) | ok (unch.) | 9/9      | 0           |
| amantin        | ok      | ok           | 85/85              | ok    |     0 | 28 (1× "still used", test project)     | no         | ok (unch.)  | 85/85      | 0           |
| nadirbad       | ok      | ok¹          | 30/30              | ok    |     0 | 18 (request-processor, generic notif.) | no         | ok (unch.)  | 30/30      | 0           |
| evo ch. 1      | ok      | ok           | 45/45              | ok    |     0 | 9 (1× add-mediatr test project)        | no         | ok (unch.)  | 45/45²     | 0           |
| evo ch. 2      | ok      | ok           | 26/52 (26 DB)      | ok    |     0 | 27 (2× add-mediatr)                    | no         | ok (unch.)  | 26/52      | 0           |
| kgrzybek       | ok³     | ok³          | 246/287 (40 DB)    | ok    |     0 | 242 (project-file + 32 generic)        | no         | ok (unch.)  | 246/287    | 0           |
| **eShopOnWeb** | ok      | ok           | 74/74              | ok    | **9** | 0                                      | **yes**    | **ok**      | **74/74**  | **0**       |
| dotnet/eShop   | ok³     | **fails** (LIB002 libman/unpkg) | – | dry-run | 0 | 34 (1× generic `IdentifiedCommand<T,R>`) | no (rightly) | – | – | – |

¹ after suppressing CA1873. ² 7 data-driven test names with random values change per run (not really gone).
³ after trim. "unch." = all-or-nothing left the solution untouched; the build/test after the fix is therefore the baseline code.
All-or-nothing held in all 8 other projects: not a single half-migrated solution, 0 regressions.

### Root Manuals grouped

| Cause                                                                          | Projects           | Verdict        | Task     |
| ------------------------------------------------------------------------------ | ------------------ | -------------- | -------- |
| test code uses `RequestHandlerDelegate` (mock), no finding, only "still used" on the csproj | amantin | gap (silent) | TAAK-180 |
| self-closing `<Project Sdk="..." />` → "cannot be edited without changing unrelated text" | kgrzybek | bug | TAAK-181 |
| inactive `#if` code (without MediatR) holds `global using MediatR`             | jason              | gap            | TAAK-182 |
| `AddMediatR` across project boundaries (internal handler, assembly param, test project) | ardalis, evo1, evo2 | investigate | TAAK-183 |
| request-processor `return Task.Run(...)`                                       | nadirbad           | gap            | TAAK-184 |
| package on MediatR (Ardalis.SharedKernel)                                      | ardalis            | rightly Manual | –        |
| generic messages/decorators (`DomainEventNotification<T>`, `*CommandHandlerDecorator<T>`, `IdentifiedCommand<T,R>`) | nadirbad, kgrzybek, eShop | rightly Manual | – |

Build errors after fix: none (eShopOnWeb, the only rewritten project, builds). TAAK-134/135 work: the previous
blockers (project-file formatting in 6 projects, loading of eShopOnWeb/eShop/kgrzybek) are gone.

### Conclusion

**1 of 9 fully migrated and green** (eShopOnWeb: 9 Fixed, 0 Manual, MediatR gone, 74/74). Previous run: 0 of 9.
Four projects (also) keep rightful Manuals (ardalis, nadirbad, kgrzybek, eShop). After TAAK-180 and TAAK-182
amantin and jason are expected to be complete; after TAAK-183 possibly evo1/evo2. "100% MediatR" is thereby not yet
proven: at most 5 of 9 is achievable, the rest use constructs Mediator does not support.

## Repeat after TAAK-180..184 (2026-10-12, 5f9b366)

Same commits, flags, trims (nadirbad `CA1873 = none`; kgrzybek `_build` + `Database/*` removed from the solution) and harness
(`py -3`, nuget.config only nuget.org) as the run above; dotnet/eShop not repeated (baseline fails on libman).
Results in `<corpus>/corpus-mediatr/_results/r2/<name>/` (clones removed).

| Project        | Test before      | Fixed | Manual | Root Manual after these tasks                                 | Migrated   | Build after | Test after | Regressions |
| -------------- | ---------------- | ----: | -----: | ------------------------------------------------------------- | ---------- | ----------- | ---------- | ----------- |
| jason          | 39/41 (2 red)    | **27** | 0     | none                                                          | yes, but   | **fails** (4× CS0305) | – | n/a |
| ardalis        | 9/9              | 0     | 28     | 11× Ardalis.SharedKernel, 1× add-mediatr (internal handler)   | no (rightly) | ok (unch.) | 9/9 | 0 |
| amantin        | 85/85            | 0     | 30     | 1× request-handler-delegate (shared mock field)               | no         | ok (unch.) | 85/85 | 0 |
| nadirbad       | 30/30            | 0     | 17     | 1× generic notification                                       | no (rightly) | ok (unch.) | 30/30 | 0 |
| evo ch. 1      | 45/45            | 0     | 9      | 1× add-mediatr (internal method, test project calls it)       | no (rightly) | ok (unch.) | 45/45¹ | 0 |
| evo ch. 2      | 26/52 (26 DB)    | 0     | 27     | 2× add-mediatr (assembly param, internal handler)             | no (rightly) | ok (unch.) | 26/52¹ | 0 |
| kgrzybek       | 246/287 (40 DB)  | 0     | 278    | 32× generic decorator/notification, 30× unmatched-use (Autofac `MediatorModule`) | no (rightly) | ok (unch.) | 246/287 | 0 |
| **eShopOnWeb** | 74/74            | **9** | 0      | none                                                          | **yes**    | **ok**   | **74/74** | **0**     |

¹ 7 data-driven test names with random values change per run (missing/new 7), not really gone.

Per task:

- TAAK-180 (amantin): `ValidationBehaviorTests` covered; the safety net now gives a Manual with a location. Remaining gap: one
  mock field for three request types in `AuthorizationBehaviorTests` → **TAAK-191**.
- TAAK-181 (kgrzybek): no "cannot be edited" Manual any more. The safety net from TAAK-180 now shows 30 `unmatched-use` in the five
  Autofac `MediatorModule`s (`MediatR.Pipeline` types, `RequestPre/PostProcessorBehavior`).
- TAAK-182 (jason): the `#if` blocker is gone, everything Fixed and MediatR gone, but the build breaks: `RequestLoggerTests` constructs
  `LoggingBehaviour<CreateTodoItemCommand>` and calls `Process`, while the processor is now `<TRequest, TResponse>` → **TAAK-190**.
  This is a silent build break despite 0 Manual.
- TAAK-183 (ardalis, evo1, evo2): exactly the deliberately kept add-mediatr Manuals; no change, as expected.
- TAAK-184 (nadirbad): request-processor Manual gone; the only root is the generic `DomainEventNotification<T>`.

### Rightful Manuals (Mediator does not support it)

| Project   | Manual                                                        | Reason                                                                                 |
| --------- | ------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| ardalis   | Ardalis.SharedKernel (11×)                                    | external package builds on MediatR types; Unshackle cannot rewrite the package          |
| ardalis   | add-mediatr, `ContributorDeletedHandler` internal             | the Mediator generator in Web cannot call an internal handler without InternalsVisibleTo (CS0122, TAAK-183) |
| nadirbad  | generic `DomainEventNotification<T>`                          | **solved by TAAK-192** (`mediatr.generic-notification`): nadirbad 18 Fixed, 0 Manual, build ok, 30/30 |
| evo1      | add-mediatr in an internal method the test project calls      | handlers in two assemblies; one `AddMediator` per host (TAAK-148)                      |
| evo2      | add-mediatr with an `Assembly` parameter; internal handlers in Contracts | handlers at the caller / internal without friend (TAAK-144, TAAK-183)       |
| kgrzybek  | 32 generic decorators and `DomainNotificationBase<T>`         | generic handlers/decorators not supported by the generator                              |
| kgrzybek  | 30 unmatched-use in Autofac `MediatorModule`                  | manual Autofac registration of MediatR-internal types; Mediator registers via its own DI, needs a redesign |

### Conclusion

**1 of 9 fully migrated and green** (eShopOnWeb, unchanged). jason is fully rewritten but does not build (TAAK-190);
amantin hangs on one shared mock field (TAAK-191). Expected after those two tasks: 3 of 9. The other six are rightly Manual
(dotnet/eShop too, plus the libman baseline). All-or-nothing held: 0 regressions in the 7 projects that ran.

## Repeat round 3 (2026-10-13)

main `9189df4` (after TAAK-190..197). Same commits, flags, trims and harness as above; results in
`<corpus>/corpus-mediatr/_results/r3/<name>/` (clones removed). dotnet/eShop now did run: libman bypassed with
`LibraryRestore=false` (environment variable → MSBuild property), MAUI trim as before, `TEST_TIMEOUT=600` (functional tests
wait for Docker/SQL and otherwise run for an hour).

| Project        | Test before      | Fixed | Manual | Root Manual                                                     | Migrated   | Build after | Test after | Regressions |
| -------------- | ---------------- | ----: | -----: | --------------------------------------------------------------- | ---------- | ----------- | ---------- | ----------- |
| **jason**      | 39/41 (2 red)    | **33** | 0     | none                                                            | **yes**    | **ok**   | **39/41** | **0**     |
| **ardalis**    | 9/9              | **32** | 0     | none                                                            | **yes**    | **ok**   | **9/9**  | **0**      |
| **amantin**    | 85/85            | **29** | 0     | none                                                            | **yes**    | **ok**   | **85/85** | **0**     |
| **nadirbad**   | 30/30            | **18** | 0     | none                                                            | **yes**    | **ok**   | **30/30** | **0**     |
| evo ch. 1      | 45/45            | 0     | 9      | 1× add-mediatr (internal method, test project calls it)         | no (rightly) | ok (unch.) | 45/45¹ | 0 |
| evo ch. 2      | 26/52 (26 DB)    | 0     | 27     | 1× add-mediatr (`Assembly` parameter, public method)            | no (rightly) | ok (unch.) | 26/52¹ | 0 |
| kgrzybek       | 246/287 (40 DB)  | 0     | 270    | 15× `mediatr.unsupported` on module decorators without `using MediatR` | no (**bug**) | ok (unch.) | 246/287 | 0 |
| **eShopOnWeb** | 74/74            | **9** | 0      | none                                                            | **yes**    | **ok**   | **74/74** | **0**     |
| dotnet/eShop   | 32 unit green²   | 52    | 0      | none                                                            | yes, but   | **fails** (12× NS1000) | – | n/a |

¹ 7 data-driven test names with random values change per run. ² Basket 3/3, Ordering.UnitTests 29/29; functional tests
red (no Docker), the test run hit the time-out.

- jason (TAAK-190), amantin (TAAK-191), nadirbad (TAAK-192), ardalis (TAAK-194/195/197): now fully migrated and green;
  MediatR only still occurs in the solutions in names/comments of the vendored SharedKernel (ardalis; `sample/` is a separate solution).
- kgrzybek: TAAK-196 (Autofac) and TAAK-193 work for the BuildingBlocks decorator, but the 15 module decorators
  (`ICommandHandler<T>` marker, file without `using MediatR`) still fall into `mediatr.unsupported` → **TAAK-210** (reproduced in a
  two-project repro: with `using MediatR` everything Fixed, without it Manual).
- dotnet/eShop: `IdentifiedCommand<T,R>` migrates (TAAK-193), but `mediatr.send-as-task` turns NSubstitute setups into
  `mock.Send(...).AsTask().Returns(...)` → NS1000, a silent build break → **TAAK-211**.
- evo1/evo2: rightly (TAAK-195 measured: no exact translation).

### Conclusion

**5 of 9 fully migrated and green** (jason, ardalis, amantin, nadirbad, eShopOnWeb; previous round 1). Two rightly Manual
(evo1, evo2). Two bugs: kgrzybek (TAAK-210) and dotnet/eShop (TAAK-211); expected after those tasks: 7 of 9. 0 regressions.

## Repeat round 4 (2026-10-13, only kgrzybek and dotnet/eShop)

Working tree on `9189df4` + TAAK-210/211 (not committed; also contains ongoing FA work of another agent in `Fixer`/`UsingDirectives`).
Same commits, trims and harness (`_results/r4/harness.sh`, copy of r2 with path to `r3/corpus-regression.py`); eShop with
`LibraryRestore=false`, `TEST_TIMEOUT=600`. Results in `<corpus>/corpus-mediatr/_results/r4/<name>/` (clones in `corpus-mediatr/src/`).

| Project      | Test before     | Fixed | Manual | Root Manual | Migrated   | Build after | Test after | Regressions |
| ------------ | --------------- | ----: | -----: | ----------- | ---------- | ----------- | ---------- | ----------- |
| kgrzybek     | (DB tests red)  | 217   | **0**  | none        | yes, but   | **fails** (88× MSG0001, 26× SA1210, 6× CS1503, 4× CS0122) | – | n/a |
| dotnet/eShop | unit 32/32¹     | 52    | 0      | none        | **yes**    | **ok**      | unit 32/32¹ | **0** |

¹ Basket + Ordering.UnitTests; functional tests (Docker) hit the time-out; after the fix 6 extra functional tests red that before the fix did not
run within the time-out (harness: "flaky/new", not a regression).

- TAAK-211: the 6 NSubstitute setups in `Ordering.UnitTests` become `.Returns(ValueTask.FromResult(true))`; no more NS1000.
- TAAK-210: 15 module decorators now `mediatr.generic-decorator` Fixed. Three extra causes found and solved on the way
  (intermediate runs `kgr-try1..3`): non-CPM `Directory.Packages.props` → NU1015; `Unit` without `using Mediator` in void handlers → CS0246/CS0738;
  SA1513 after `return Unit.Value;`. What remains → **TAAK-212**.

### Conclusion

dotnet/eShop now fully migrated and green: **6 of 9** (round 3: 5). kgrzybek: 0 Manual, build still red (TAAK-212).

## Repeat round 5 (2026-10-13, only kgrzybek, TAAK-212)

Working tree after TAAK-212 (not committed). Harness `_results/r5/harness.sh` (copy of r4); results in `<corpus>/corpus-mediatr/_results/r5/kgr/`.

| Project  | Fix | Build after | Errors |
| -------- | --- | ----------- | ------ |
| kgrzybek | 218 Fixed, 0 Manual | **fails** | 0× MSG0001, 0× SA1210, 0× CS1503, 0× CS0122 (r4: 88/26/6/4); now visible: 410× CS0436 + 2× CS0121, only in `Registrations.Infrastructure` |

- Those new errors were hidden in r4 behind the MSG0001 break of `UserAccess.Infrastructure` (Registrations was not compiled then).
  Cause: a generator host refers to another host (generated Mediator types twice) → **TAAK-213**.

## Repeat round 6 (2026-10-13, only kgrzybek, TAAK-213)

Working tree after TAAK-213 (not committed; also contains ongoing FA work in `Fixer.cs`/`UsingDirectives.cs`). Harness `_results/r6/harness.sh` (copy of r5); results in `<corpus>/corpus-mediatr/_results/r6/kgr/`.

| Project  | Fix | Build after | Tests before → after | Regressions |
| -------- | --- | ----------- | -------------------- | ----------- |
| kgrzybek | 0 Manual | **ok** (r5: 410× CS0436 + 2× CS0121) | 246/287 → 242/287 | 4, all `ArchTests.ModuleTests.*Module_DoesNotHave_Dependency_On_Other_Modules` |

- Registrations.Infrastructure got `Assemblies = [...]`, UserAccess.Infrastructure `GenerateTypesAsInternal = true`; no more `InternalsVisibleTo` between the hosts.
- Regressions: generated `Mediator.Mediator`/`Mediator.Internals.ContainerMetadata` depend on other modules → **TAAK-214**.
- TAAK-214 (probe on the r6 working tree): the dispatcher names the IntegrationEvents of Meetings/Registrations because Administration has handlers for them.
  Restricting `Assemblies` makes those events silently drop (rejected). **Legitimate difference**: with `DoNotResideInNamespace("Mediator")` in the 4 archtests
  they pass (ArchTests 8/8) → effectively 246/287 = baseline. No code change, so no r7.
- TAAK-240: the fix now warns in the report (4× on the MediatorModules) that archtests must exclude the namespace `Mediator`.

## Repeat round 7 (2026-10-26, ardalis, amantin, nadirbad, evo1, evo2; main 75e869b)

The clones were gone from `<corpus>/corpus-mediatr/src`; re-fetched as GitHub source archives (no `.git`, no scripts run) at the
same commits (ardalis `ccb82ba`, amantin `5822811`, nadirbad `feb3a15`, evolutionary `441a7cd` chapters 1 and 2). Tool published
from main 75e869b. Harness as round 6 plus a `scan` step; both `scan` and `fix` with `--max-degree 4 --force`. Same flags, the
nuget.org-only `nuget.config`, nadirbad `CA1873 = none`. Environment intervention: the archive has LF line endings, so evo1
failed its **baseline** build with 30× IDE0055 (TWAE); its `.cs` files were converted to CRLF (what a Windows checkout gives).

| Project   | Test before   | Scan F/M | Fix F/M | Root Manual | Migrated | Build after | Test after | Regressions |
| --------- | ------------- | -------- | ------- | ----------- | -------- | ----------- | ---------- | ----------- |
| ardalis   | 9/9           | 32/0     | 32/0    | none        | **yes**  | ok          | 9/9        | 0 |
| amantin   | 85/85         | 29/0     | 29/0    | none        | **yes**  | ok          | 85/85      | 0 |
| nadirbad  | 30/30         | 18/0     | 18/0    | none        | **yes**  | ok          | 30/30      | 0 |
| evo ch. 1 | 45/45         | 0/9      | 0/9     | 1× add-mediatr (internal method, test project calls it) | no (rightly) | ok (unch.) | 45/45 | 0 |
| evo ch. 2 | 26/52 (26 DB) | 0/27     | 0/27    | 1× add-mediatr (`Assembly` parameter, public method, packable project) | no (rightly) | ok (unch.) | 26/52 | 0 |

Identical to round 3. Scan and fix reports agree. In ardalis MediatR only remains in `sample/` (separate solution).
With rounds 4–6 and the re-measure in `ANALYSE-manual-round-2.md`: **7 of 9 fully migrated and green**, evo1/evo2 rightly Manual. No new bugs.

## See also

- `claude/tasks/archief/TAAK-210-mediatr-generieke-decorator-zonder-using.md`, `claude/tasks/archief/TAAK-211-mediatr-send-as-task-nsubstitute-setup.md`, `claude/tasks/archief/TAAK-212-mediatr-kgrzybek-build-na-fix.md`, `claude/tasks/archief/TAAK-213-mediatr-host-verwijst-naar-host.md`, `claude/tasks/archief/TAAK-214-mediatr-archtests-gegenereerde-mediator.md`
- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` — frequencies from this corpus
- `claude/tasks/mediatr-queue.md` — pattern queue
- `docs/projectinfo/architectuur/core.md` — all-or-nothing
- TAAK-130 through TAAK-136 in `claude/tasks/`
- TAAK-180 through TAAK-184 (archive), TAAK-190, TAAK-191
- TAAK-210 (generic decorator without `using MediatR`), TAAK-211 (send-as-task in NSubstitute setup)
