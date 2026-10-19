# Corpus run — blind validation round (TAAK-268)

Tool: published from main `23c0f28` into `<corpus>/corpus-blind/_results/tool-23c0f28/`. The tool is not changed during this round.
Harness: `claude/tools/corpus-am.sh` with `REPOS=claude/tools/corpus-blind-repos.txt`, list `claude/tools/corpus-blind-list.txt` (fix per line).
Corpus root: `<corpus>/corpus-blind/`.

## Selection (step 2)

Picked from GitHub code search (`gh search code 'Include="<Library>" Version="<major>.'`, csproj) on 2026-10-15, before any run. None is on the exclusion list. Shas pinned in `corpus-blind-repos.txt`.

| Name | Repo | Fix | Size | Why |
| --- | --- | --- | --- | --- |
| skunkdeco | bfriesen/RandomSkunk.DependencyInjection.Decorator | fluentassertions | small (38 kB) | Library with one test project; FA 6; multi-target incl. net461/netstandard |
| ogooreck | oskardudycz/Ogooreck | fluentassertions | medium | API-testing library (128 stars); FA 6 across multi-targeted test projects |
| ~~omnikeeper~~ | max-bytes/omnikeeper | fluentassertions | large | Replaced: restore fails upstream (NU1201, plugins on net6 reference a newer `Omnikeeper.Base`) |
| xrepo | andyalm/xrepo | fluentassertions | medium (3.5 MB) | Replacement for omnikeeper: CLI tool with an FA 6 test project |
| realworld | minhlong149/realworld-aspnetcore | mediatr | small | RealWorld (Conduit) API with MediatR 12 handlers and a test project |
| fullcart | AmrElshaer/FullCart | mediatr | medium (35 files use MediatR) | Clean-architecture shop; MediatR 12 |
| miru | MiruFx/Miru | mediatr | large (8 test projects) | Web framework built on MediatR; net5–net9 |
| culinary | culinary-code/backend | automapper | small | API with AutoMapper 13 profiles and a test project |
| ~~ctwapi~~ | Coflnet/CtwApi | automapper | small | Replaced: build fails upstream (test project misses the `Coflnet` reference, CS0246) |
| fiapgames | thaynnaraluciano/fiap-cloud-games | automapper | small | Replacement for ctwapi: CQRS API with AutoMapper 13 and 2 test projects |
| msb | Robnoir/MSB | automapper | large (106 files) | Application using AutoMapper 13 (and MediatR); one test project |

## Results

Tool `23c0f28`, run 2026-10-15.

| Repo | Fix | Fixed | Manual | Tests before → after | Regressions | Outcome |
| --- | --- | ---: | ---: | --- | ---: | --- |
| skunkdeco | fluentassertions | 110 | 14 | 26/26 → 26/26 | 0 | migrated, green |
| ogooreck | fluentassertions | 23 | 9 | 20/20 → — | — | **build fails after fix**: 12× CS0452 (TAAK-269) |
| xrepo | fluentassertions | 122 | 9 | 71/71 → 71/71 | 0 | migrated, green |
| realworld | mediatr | 10 | 0 | 10/10 → 10/10 | 0 | fully migrated, green |
| fullcart | mediatr | 0 | 33 | 9/9 → 9/9 | 0 | blocked: 2 justified Manual block 31 findings (TAAK-270) |
| miru | mediatr | 0 | 99 | 415/474 → 414/474 ¹ | 0 | blocked: 4 Manual, could be migrated (TAAK-270) |
| culinary | automapper | 0 | 46 | 149/149 → 149/149 | 0 | blocked: cycle, null sources |
| msb | automapper | 0 | 33 | 156/156 → 156/156 | 0 | blocked: cycle, unflattening |
| fiapgames | automapper | 0 | 32 | 91/91 → 91/91 | 0 | blocked: one profile (TAAK-270) |

¹ Miru: 59 tests need a database and fail in both baseline runs; one more is flaky (compare marks it flaky, not a regression). The tool changed no file.

## Review (step 6)

- **Fixed rewrites (FA, 265; MediatR, 10)**: covered by green test suites in skunkdeco, xrepo and realworld. One defect: `NotBeNull()` on an unconstrained type parameter (ogooreck) → TAAK-269. FA Manual reasons sampled (`Be` on `FluentAssertions.Types`, `.And`/`.Which` used, `.And` on another subject, `BeEquivalentTo` on non-plain types): all justified.
- **Manual (MediatR/AutoMapper)**: the all-or-nothing rule is right (a partial migration breaks at runtime), but on unseen code it means 5 of 6 repos get no change at all. Blockers per repo and verdict in TAAK-270; cycles, null sources and unflattening are justified.

## Conclusion

- **FluentAssertions**: ready on unseen code, apart from one build break (TAAK-269). 2 of 3 repos fully green, 0 regressions.
- **MediatR**: correct where it runs (realworld, 0 regressions), but coverage is limited: 2 of 3 repos blocked by a handful of Manual findings.
- **AutoMapper**: safe (0 regressions, nothing wrongly rewritten) but on unseen code it migrated nothing; cycles and `ReverseMap`/unflattening are common in real profiles.
- Nothing was rewritten wrongly in a way that tests passed but behaviour changed, as far as the tests and review show. Launch claim: "FA → Shouldly: production-ready; MediatR and AutoMapper: safe, report the spots to fix by hand." Round 2 after TAAK-269/270.

## Round 2 (after TAAK-269/270)

Tool `12f86c0` (fixes merged), then `d1f33bf` (two fixes after round 2). Only the three affected repos rerun.

| Repo | Tool | Fixed / Manual | Tests before → after | Regressions | Outcome |
| --- | --- | --- | --- | ---: | --- |
| ogooreck | `12f86c0` | 23 / 9 | — | — | build fails: 12× CS8600, `((object)result)` on a `T` that may be null → fixed in `d1f33bf` with `(object?)` |
| miru | `12f86c0` | 97 / 0 | — | — | build fails: MSG0007, `AddMediatR` in a generic method scans `TAssemblyOfType`; Mediator's generator needs compile-time assemblies → Manual in `d1f33bf` |
| fiapgames | `12f86c0` | 0 / 32 | 91/91 → 91/91 | 0 | still blocked: `UsuarioModel.Nome { get; private set; }` (justified Manual) |
| ogooreck | `d1f33bf` | 23 / 9 | 20/20 → 20/20 | 0 | migrated, green |
| miru | `d1f33bf` | 0 / 98 | 415/474 → 429/474 ¹ | 0 | blocked (justified): generic `AddMediatR` must move to the host by hand |

¹ Database-dependent tests; their count of failures varies between runs, none is a regression.

### Conclusion after round 2

- **FluentAssertions**: 3/3 unseen repos migrated and green, 0 regressions.
- **MediatR**: 1/3 fully migrated; the other two are blocked by Manual findings that are justified (direct `MediatR.Mediator` use, `AddMediatR` in a generic library method). Round 2 also showed that a Fixed rewrite (the expression-to-block lambda of TAAK-270) can break the generator; caught by the build, now Manual.
- **AutoMapper**: 0/3 migrated, all blocked by justified Manual findings (cycles, unflattening, private setters); 0 regressions.

## Round 3 (9 more unseen repos, all 18 rerun)

Picked 2026-10-15 before the run, none on the exclusion list (`corpus-blind-repos.txt`, "round 3"). Replaced before the tool ran on them: nsdepcop (baseline build fails, NsDepCop.Analyzer roslyn4.0 variant) → beardgame/utilities. dennisdoomen/packageguard was rejected because it now uses FA 8.

Tool `d1f33bf` for all 18; found one defect (mediatrsample), fixed in `c73b78c` and the affected repos rerun twice (runs `r4a`, `r4b`).

| Repo | Fix | Fixed / Manual | Tests before → after | Regressions | Outcome |
| --- | --- | --- | --- | ---: | --- |
| pathlib | fluentassertions | 7 / 0 | 177/184 → 177/184 | 0 | migrated, green (7 fail in baseline) |
| buildenator | fluentassertions | 66 / 318 | 127/127 → 127/127 | 0 | migrated where safe, green |
| bearded (beardgame/utilities) | fluentassertions | 375 / 80 | 1294/1294 → 1294/1294 | 0 | migrated, green (two runs) |
| mediatrsample | mediatr | 25 / 0 | 14/21 → 14/21 | 0 | `d1f33bf`: build fails (CS1061, `handler.Handle(...)` in an FA `ThrowAsync` lambda became `ValueTask`) → `c73b78c` adds `.AsTask()`: migrated, green (two runs) |
| cashflow | mediatr | 6 / 0 | 0 tests | — | migrated, builds (no tests in the repo) |
| starter | mediatr | 0 / 11 | 45/49 → 45/49 | 0 | blocked (Manual) |
| bookstore | automapper | 33 / 0 | 592/595 → 592/595 | 0 | migrated; run `r4b` showed 1 different failing test, run `r4a` none: flaky shared test database |
| restapi | automapper | 0 / 6 | 45/45 → 43–44/45 | 0 ¹ | blocked; no file changed |
| baseapi | automapper | 0 / 7 | 0/9 → 0/9 | 0 | blocked (all 9 tests fail in baseline) |
| (round 1–2 repos, 9) | | unchanged | unchanged | 0 | same results as round 2 |

¹ The tool changed no file in restapi, yet 1–2 integration tests fail differently per run (EF tracking conflict in a shared in-memory database): flaky, not a regression.

## Final conclusion (18 unseen repos)

| Fix | Migrated and green | Blocked by justified Manual | Regressions |
| --- | --- | --- | ---: |
| FluentAssertions | 6/6 (skunkdeco, ogooreck, xrepo, pathlib, buildenator, bearded) | 0 | 0 |
| MediatR | 3/6 (realworld, mediatrsample, cashflow) | 3 (fullcart, miru, starter) | 0 |
| AutoMapper | 1/6 (bookstore) | 5 (culinary, msb, fiapgames, restapi, baseapi) | 0 |

Every defect the blind rounds found (CS0452, CS8600, MSG0007, CS1061) broke the build, never a test silently: the build after `fix` is the safety net, and each defect is now fixed with a proof.

## Round 5 (TAAK-276): after the manual reduction (TAAK-272..279)

Tool: published from main `db05908` into `<corpus>/corpus-blind/_results/tool-db05908/`; not changed during the round. Harness `claude/tools/corpus-am.sh` (`after` mode: fix, rebuild, own tests, compare with the baseline). "Fixed/Manual" are findings in the report (a kept map or call counts as one Manual finding each, so Manual counts are not a measure of lines left). Definitions: **full** = no Manual left, **partial** = Fixed and Manual, the old library stays for the Manual part, **blocked** = nothing rewritten.

### Part A: the 18 blind repos again (same pins, same baselines)

| Repo          | Fix              | Round 1-4 (Fixed / Manual) | Round 5 (Fixed / Manual) | Tests before → after  | Regressions | Outcome                                                                          |
| ------------- | ---------------- | -------------------------- | ------------------------ | --------------------- | ----------: | -------------------------------------------------------------------------------- |
| skunkdeco     | fluentassertions | 110 / 14                   | 123 / 0                  | 26/26 → 26/26         |           0 | **full**                                                                         |
| ogooreck      | fluentassertions | 23 / 9                     | 23 / 9                   | 20/20 → 20/20         |           0 | partial (9 FA left)                                                              |
| xrepo         | fluentassertions | 122 / 9                    | 122 / 9                  | 71/71 → 71/71         |           0 | partial (9 FA left)                                                              |
| pathlib       | fluentassertions | 7 / 0                      | 7 / 0                    | 177/184 → 177/184     |           0 | **full**                                                                         |
| buildenator   | fluentassertions | 66 / 318                   | 623 / 46                 | 127/127 → 127/127     |           0 | partial (46 FA left; was 318)                                                    |
| bearded       | fluentassertions | 375 / 80                   | 375 / 80                 | 1294/1294 → 1294/1294 |           0 | partial (80 FA left)                                                             |
| realworld     | mediatr          | 10 / 0                     | 10 / 0                   | 10/10 → 10/10         |           0 | **full**                                                                         |
| mediatrsample | mediatr          | 25 / 0                     | 25 / 0                   | 14/21 → 14/21         |           0 | **full**                                                                         |
| cashflow      | mediatr          | 6 / 0                      | 6 / 0                    | no tests              |           - | **full** (builds)                                                                |
| fullcart      | mediatr          | 0 / 33                     | 27 / 20                  | 9/9 → 9/9             |           0 | **partial** (TAAK-274; 15 handlers tied to an Autofac decorator stay on MediatR) |
| miru          | mediatr          | 0 / 98                     | 0 / 98                   | 400/474 → 405/474 ¹   |           0 | blocked (generic `AddMediatR`, justified)                                        |
| starter       | mediatr          | 0 / 11                     | 0 / 11                   | 45/49 → 45/49         |           0 | blocked (`AddMediatR` + `InternalsVisibleTo`)                                    |
| bookstore     | automapper       | 33 / 0                     | 33 / 0                   | 592/595 → 591/595 ²   |           0 | **full**                                                                         |
| culinary      | automapper       | 0 / 46                     | 2 / 70                   | 149/149 → 149/149     |           0 | partial (marginal: mocked `IMapper` classes keep their maps)                     |
| msb           | automapper       | 0 / 33                     | 8 / 29                   | 156/156 → 156/156     |           0 | partial                                                                          |
| fiapgames     | automapper       | 0 / 32                     | 25 / 9                   | 91/91 → 91/91         |           0 | **partial** (2 maps with private setters stay)                                   |
| baseapi       | automapper       | 0 / 7                      | 5 / 3                    | 0/9 → 0/9             |           0 | partial (all 9 tests fail in baseline)                                           |
| restapi       | automapper       | 0 / 6                      | 0 / 9                    | 45/45 → 45/45         |           0 | blocked (no file changed)                                                        |

¹ Miru: database-dependent tests, count varies per run (flaky, not a regression). ² Bookstore: shared test database, 1-2 `Update...Return_Ok` tests (`NotFound`) differ per run; a fresh untouched baseline run on 2026-10-18 failed `OrderLineUpdateAsyncTask_Return_Ok` as well, so not a regression (runs kept in `_results/after-bookstore-run1`, `_results/base-bookstore-orig`).

Per library (18 repos): **0 regressions, 0 build breaks, no wrongly rewritten test.** buildenator/fullcart/starter show renamed test display names (same pass count), not failures.

| Fix              | Full (old → new) | Partial (old → new) | Blocked (old → new) | Fixed findings (old → new) |
| ---------------- | ---------------- | ------------------- | ------------------- | -------------------------- |
| FluentAssertions | 1 → 2            | 5 → 4               | 0 → 0               | 703 → 1273                 |
| MediatR          | 3 → 3            | 0 → 1               | 3 → 2               | 41 → 68                    |
| AutoMapper       | 1 → 1            | 0 → 4               | 5 → 1               | 33 → 73                    |

(The old "FA 6/6 migrated" counted partial FA migrations as migrated; here they are shown apart.)

### Part B: 18 new, never used repos (6 per library)

Selection (2026-10-18, before any `after` run): `gh search code 'Include="<Library>" Version="<last free major>."'` on csproj files (FA 6.12/7, MediatR 12, AutoMapper 13/14), not a fork or archive, checked against every `CORPUSRUN-*.md`, `REGRESSIE-corpus*.md`, task and repo list (none used before), solution and test project present. Baseline (`base`: restore, build, test twice) had to build and run tests; rejected at the baseline (replaced by spares, listed below): supertext (build fails), octopus (tests hang over an hour), innosetup, dotnettemplate, passwordmanager (restore fails), orderservice, policeevents, expensestracker, bugtracker, cmartlineupbuilder (build fails), puppeteer (browser tests, flaky), and minifyurl, otuscleanarch, fijournal, mycrm, lulusia, techchallenge (no test discovered). Pins: `claude/tools/corpus-blind-repos.txt` ("round 5"), solutions `claude/tools/corpus-blind-list-r5-selected.txt`. Sizes are small to large (4 to 686 tests).

| Name           | Repo @ commit                                    | Fix              | Fixed / Manual | Tests before → after | Outcome                                                                                                       |
| -------------- | ------------------------------------------------ | ---------------- | -------------- | -------------------- | ------------------------------------------------------------------------------------------------------------- |
| grpcerror      | nano-byte/grpc-rich-error @ `b83cab1`            | fluentassertions | 12 / 0         | 4/4 → 4/4            | **full**                                                                                                      |
| asuna          | luisfb/asuna-full-text-search @ `292bf1b`        | fluentassertions | 48 / 0         | 20/21 → 20/21        | **full**                                                                                                      |
| mihome         | sergey-brutsky/mi-home @ `680f841`               | fluentassertions | 319 / 15       | 448/448 → 448/448    | partial (`Invoking` on non-trivial receivers, `.And` chains)                                                  |
| catalogapi     | daohainam/catalog-api @ `2396e35`                | fluentassertions | 222 / 4        | 44/54 → 44/54        | partial (`BeEquivalentTo` on a list, `ThrowAsync` on another delegate)                                        |
| opentws        | OpenTWS/opentws-dotnet @ `a3ecb8e`               | fluentassertions | 118 / 0        | 17/56 → 17/56        | **full** (TAAK-283 fixed: unused `using FluentAssertions.Common;` removed; was a build break)                 |
| serilogmem     | serilog-contrib/SerilogSinksInMemory @ `86b7743` | fluentassertions | 17 / 399       | 570/570 → 570/570    | partial (TAAK-281 fixed; it is an FA extension library, 378 `unmatched-use`; was a build break)               |
| brewabear      | RieBi/BrewABear @ `d3199fb`                      | mediatr          | 23 / 0         | 33/33 → 33/33        | **full**                                                                                                      |
| dddbankingapi  | Ace2489/ddd-Banking-API @ `135bb36`              | mediatr          | 11 / 0         | 62/64 → 62/64        | **full**                                                                                                      |
| netcorebase    | nruizneiman/netcore-base @ `1b28112`             | mediatr          | 12 / 0         | 45/46 → 45/46        | **full**                                                                                                      |
| afripay        | Hectorberi01/AfriPay @ `10998ae`                 | mediatr          | 0 / 16         | 264/264 → 264/264    | blocked: `RequestHandlerDelegate` initialized with a real delegate in tests (not a `Substitute.For`)          |
| cleanmicro     | jorsang1/clean-microservice-template @ `eda7258` | mediatr          | 0 / 20         | 67/67 → 67/67        | blocked: `RequestHandlerDelegate<Result<T>>` used other than as a mocked field/local                          |
| projectpulse   | anubhavsharmawork/projectpulse @ `b18e2b1`       | mediatr          | 78 / 0 (TAAK-285; was 0 / 176) | 618/686 → 618/686 | **full** after TAAK-285 (the blocker was a cp1252 file, not the overload; plus Moq `Setup` fixes, see below) |
| pi22317        | Granuch/PI-223-1-7 @ `51768d9`                   | automapper       | 21 / 4         | 49/60 → 49/60        | partial (`AddAutoMapper` stays: `IMapper` still injected somewhere)                                           |
| corebanking    | tuthth/NET9_CoreBanking @ `6888d6c`              | automapper       | 1 / 1          | 10/10 → 10/10        | partial (marginal)                                                                                            |
| hospitalappt   | AtifChy/HospitalAppointmentSystem @ `e8db15a`    | automapper       | 0 / 32         | 8/8 → 8/8            | blocked: maps live in a `MapperConfig`/`new MapperConfiguration(cfg => ...)`, outside a `Profile` constructor |
| assistiva      | Dreizack97/Assistiva @ `11f679c`                 | automapper       | 55 / 1         | 14/16 → 14/16        | partial (`AddAutoMapper` stays: `IMapper` still used); was **build breaks** (`#region`, CS1028), fixed in TAAK-280, rerun 0 regressions                                           |
| encryptednotes | ExtendedGuru4883/EncryptedNotes @ `9a06174`      | automapper       | 10 / 6         | 85/85 → 85/85        | partial (mocked `IMapper` classes keep their maps); was **build breaks** (`required`, CS9035), fixed in TAAK-282, rerun 0 regressions                                              |
| estore         | ngckinhluan/eStore @ `917363b`                   | automapper       | 10 / 22        | 45/46 → 45/46        | partial (mocked `IMapper` classes keep their maps); was **build breaks** (`required`, CS9035), fixed in TAAK-282, rerun 0 regressions                                              |

**TAAK-286 (2026-10-18):** hospitalappt with a `MapperConfiguration` field initializer treated as a map class: 0 / 32 → 6 / 35 Fixed/Manual, tests 8/8 → 8/8, 0 regressions; the rest is `ReverseMap` with unflattening or an unmapped source member (follow-up in TAAK-286).

Spares that were cloned and baselined but not used (pinned in `corpus-blind-repos.txt`): valid8r, notepadapi, rideshare, dpollvsa, lynx, blogapi, ecommercecloth, flowerinventor, alphabet.

| Fix              | Full | Partial | Blocked (justified Manual) | Build breaks after fix                |  Test regressions |
| ---------------- | ---- | ------- | -------------------------- | ------------------------------------- | ----------------: |
| FluentAssertions | 3    | 3       | 0                          | 0 (opentws, serilogmem fixed: TAAK-281/283) | 0 |
| MediatR          | 3    | 0       | 3                          | 0                                     |                 0 |
| AutoMapper       | 0    | 5       | 1                          | 0 (3 fixed by TAAK-280/282, see below) | 0                 |

### Regressions and tasks

All five new failures are build breaks after `fix` (the build is the safety net: no test changed silently), from four defects. Each has a task with a failing proof parked in `_Pending` (runners skip it; verified failing by copying it out). Not fixed in TAAK-276, by agreement.

| Task     | Defect                                                                                | Repos                  | Proof                                                             |
| -------- | ------------------------------------------------------------------------------------- | ---------------------- | ----------------------------------------------------------------- |
| TAAK-280 | `#region` around removed `CreateMap` statements leaves unbalanced directives (CS1028) | assistiva              | `Proofs/AutoMapper/_Combined/region-directives` (fixed)           |
| TAAK-281 | FA using inside `#if`/`#else` removed with its directives, stray `#endif` (CS1028). **Fixed**; also a `#if` inside a rewritten chain and a file linked into a project without Shouldly | serilogmem | `Proofs/FluentAssertions/_Combined/using-in-conditional`, `directives-in-chain` |
| TAAK-282 | Generated `new() { ... }` omits `required` destination members (CS9035)               | encryptednotes, estore | `Proofs/AutoMapper/_Combined/required-members` (fixed)            |
| TAAK-283 | `using FluentAssertions.Common;` stays after the package is removed (CS0246). **Fixed**  | opentws                | `Proofs/FluentAssertions/_Combined/using-sub-namespace`           |

**Rerun for TAAK-280 and TAAK-282** (tool from branch `worktree-agent-a7fb36f3b60c1eb0d`, published to `_results/tool-a7fb36f/`, harness `after` mode, results `_results/after/{assistiva,encryptednotes,estore}`): all three build after `fix` and have **0 regressions** (assistiva 14/16 → 14/16, encryptednotes 85/85 → 85/85, estore 45/46 → 45/46; same Fixed/Manual as before, the Manual findings are planned `Stays on AutoMapper` keeps). Fix TAAK-282 measured first against AutoMapper 14.0.0: an unmapped `required` member stays at `default` without an error, so the generated initializer names it as `= default`; a `required` member with an initializer or set by a constructor keeps that value in AutoMapper, so that profile is Manual.

**Recheck with the combined fixes (main `58ded1e`, 2026-10-18):** the five repos that broke in round 5, `after` mode with one tool (`_results/tool-58ded1e`): assistiva 14/16 → 14/16, encryptednotes 85/85 → 85/85, estore 45/46 → 45/46, opentws 17/56 → 17/56 (baseline already fails 36 here), serilogmem 570/570 → 570/570. All build, **0 regressions**. Merging TAAK-280/282 with TAAK-281/283 first broke `_Combined/region-directives` (the TAAK-281 directive guard blocked the TAAK-280 region repair); fixed in `Fixer.Replace` (a whole `#region` pair may go, `#if`/`#else` never), with a unit test.

Re-measured after the TAAK-281/283 fixes (tool = branch `worktree-agent-a703fd17d0ed2c1c1`): serilogmem and opentws build and have 0 test regressions ([verify](../../docs/projectinfo/features/using-removal-edge-cases-verify.md)). Because the break hides later errors, the repos are not yet known to be green after these fixes; a rerun after TAAK-280..283 is needed before claiming them. Also noted: serilogmem is itself a FluentAssertions extension library (third-party `Should()` extensions), where a Shouldly migration is not meaningful.

**Recheck for TAAK-287 (branch `worktree-agent-a6a00e1539f53e94e`, 2026-10-18):** `Func<V>` subjects of Throw/ThrowAsync and `var act = x.Invoking(...)` locals are migrated now. Fixed / Manual: mihome 319/15 → 328/6, catalogapi 222/4 → 223/2, bearded 375/80 → 383/72; ogooreck, xrepo, buildenator unchanged. All six build, tests unchanged (448/448, 44/54, 20/20, 71/71, 127/127, 1294/1294), **0 regressions**. Details: `claude/tasks/archief/TAAK-287-fa-residual-round-2.md`.

### Conclusion of round 5

- **Manual work went down where the tasks aimed**: FA Manual 430 → 144 on the six old repos, MediatR fullcart blocked → partial, AutoMapper 5 blocked repos → 1 blocked and 4 partial, 0 test regressions on all 18 old repos.
- **On unseen code the full-migration rate is still modest**: FA 2/6 full (+2 partial), MediatR 3/6 full, AutoMapper 0/6 full. AutoMapper stays the weakest: injected `IMapper` and mocked `IMapper` in tests keep `AddAutoMapper` and some maps, and maps outside a `Profile` (hospitalappt) are not touched.
- **New repos found 4 build-breaking defects** in the rewriting of trivia and generated initializers; blind testing keeps paying off. None changed a test result silently. Before a 0.2.0 claim of "no regressions on unseen code", fix TAAK-280..283 and rerun those five repos.
**Recheck for TAAK-285 (branch `worktree-agent-ade469fda0960cfbe`, tool `_results/t285e`):** projectpulse had never been measured past the first file: its `Program.cs` is cp1252 and was skipped (TAAK-167), so the report said `unmatched-use`. With Latin-1 files migrated byte-exact: 78 Fixed / 0 Manual, builds, 618/686 → 618/686, 0 regressions. The first `after` run (175 Fixed) exposed two defects of `mediatr.send-as-task` that blind data had never shown, both fixed with proofs: `.AsTask()` added inside a Moq `Setup(m => m.Send(..))` expression tree (101 tests `NotSupportedException`; now left untouched), and `Setup(...).Returns(Task.FromResult(x))` / `Task.FromResult(Unit.Value)` left as Task (8 CS1503; now `ValueTask.FromResult`). The other 11 MediatR corpus repos (realworld, fullcart, miru, mediatrsample, cashflow, starter, netcorebase, cleanmicro, brewabear, afripay, dddbankingapi; tool `t285e`, results `_results/t285f`): all build, same pass counts before and after, **0 regressions**, same Fixed/Manual as before.

**Recheck for TAAK-290 (starter):** still 0 Fixed / 11 Manual, 45/49 → 45/49 (`fix --force` writes no file). Judged principally manual: the shared library's internal `AddMediatR` scans an assembly that the public `PublisherModule.AddPublisher` receives from the host at run time; Mediator's generator cannot see it. The report now names that blocker instead of the `InternalsVisibleTo` host clash.

## See also

- `claude/tasks/archief/TAAK-268-blind-validation-round.md`
- `claude/tasks/TAAK-276-blind-round-after-manual-reduction.md`, `claude/tasks/archief/TAAK-280-region-directives.md`, `TAAK-281-using-in-conditional.md`, `TAAK-282-required-members.md`, `TAAK-283-using-sub-namespace.md`
- `claude/tasks/CORPUSRUN-automapper.md`
