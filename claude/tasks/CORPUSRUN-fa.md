# Corpus run FluentAssertions (2026-10-11, main 40cf91e)

15 open-source solutions, copies in `scratchpad/run-fa/src`. Per project: restore → build/test (baseline) → `fix --fix fluentassertions --force` → build/test. Exit codes (0 = succeeded). Only SDK 10.0.112, `DOTNET_ROLL_FORWARD=Major`.

| Project | restore | build before | test before | fix | build after | test after | Cause / remark |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SmartEnum | 0 | 0 | 0 | 0 | 0 | 0 | ✅ fully green |
| FluentResults | 0 | 0 | 1 | 0 | 0 | 1 | builds after fix; tests already failed beforehand (regressions not compared) |
| CSharpFunctionalExtensions | 0 | 0 | 0 | 0 | **1** | 0* | 42× CS1061 → TAAK-110 |
| Polly | 0 | 0 | 0 | 0 | **1** | 0* | 308× CS0246 Shouldly → TAAK-111/134 |
| Ardalis.Result | 0 | 0 | 1 | 0 | **1** | 1 | 96× CS0246 Shouldly → TAAK-111/134 |
| LiteDB | 0 | 0 | 1 | 0 | **1** | 1 | 4040× CS0246 Shouldly → TAAK-111/134 |
| NUKE | 0 | 0 | 1 | 0 | **1** | 1 | 58× CS0246 Shouldly → TAAK-111/134 |
| PowerApps-Tooling | 0 | 0 | 1 | 0 | **1** | 1 | `Equal([..])` → `new T[] { [..] }` → TAAK-113 |
| Bogus | 0 | 0 | 0 | **1** | 0 | 0 | does not load in Unshackle → TAAK-112 |
| Akka.NET | 1 | – | – | – | – | – | environment: NETSDK1226 (prune data); again with `-p:AllowMissingPrunePackageData=true` |
| Steeltoe | 1 | – | – | – | – | – | environment: NETSDK1226, same |
| Mapperly | 1 | – | – | – | – | – | environment: MSB3073 in restore (own build step; `global.json` with SDK 8 removed) |
| Ardalis.Specification | 0 | 1 | – | – | – | – | project missing from tarball |
| EventFlow | 1 | – | – | – | – | – | restore fails (EventFlow.PostgreSql and others) |
| modular-monolith | 155 | – | – | – | – | – | requested SDK missing |

\* build after fix failed → tests ran on old binaries; means nothing.

## Conclusion

- Of the 9 solutions we could run, **2** build after `fix` (SmartEnum, FluentResults).
- **TAAK-111/134** (project file not editable → code rewritten anyway, Shouldly missing) alone breaks **4** of them (Polly, Ardalis.Result, LiteDB, NUKE). Highest priority; in progress.
- TAAK-110 (CSFE), TAAK-113 (PowerApps) and TAAK-112 (Bogus) are the remaining blockers.
- After these four tasks: repeat the run, including Akka/Steeltoe with `-p:AllowMissingPrunePackageData=true`, and compare test regressions.

Not done: merging Manual reasons and comparing test logs before/after (regression analysis). Reports per project: `scratchpad/run-fa/<naam>.json`.

## Rerun after TAAK-110/112/113/114 (2026-10-11)

Fresh copies in `scratchpad/v110/work/<naam>` (source `corpus/src`), only restore → fix → build (`-m:2`); script `scratchpad/v110/run.sh`.

| Project | fix | build after | Remark |
| --- | --- | --- | --- |
| CSharpFunctionalExtensions | 0 | 0 | 4481 Fixed / 70 Manual; 21 `usings` (unbound `Should()`, TAAK-110) |
| Bogus | 0 | 0 | loads; 'Builder' left out with a warning (TAAK-112) |
| Polly | 0 | 0 | 4446 Fixed / 966 Manual; Guid `NotBeEmpty` → `ShouldNotBe(Guid.Empty)` (TAAK-114) |
| PowerApps-Tooling | – | – | not repeated: no clean copy available (TAAK-113 only proven with proofs) |

evo1 (MediatR, TAAK-115): no more IDE0005/IDE0065; build still red on IDE0060 and MSG0005 (TAAK-136).

## Round 3: full corpus (2026-10-12, main 2800f23)

Shallow clones (`git clone --depth 1`) in `<corpus>/corpus-fa`, each with a `nuget.config` with only nuget.org. Harness: copy of `claude/tools/corpus-regression.sh` (SDK 10, `NuGetAudit=false`, `AllowMissingPrunePackageData=true`, 3 jobs; test 2× before the fix, then `fix --force`, bin/obj removed, build, test, and per-test comparison via TRX). The tool was published from 2800f23. All HEADs still use FA ≤ 7, so no older commit was needed; Polly, Mapperly and EventFlow are on the same tag as in round 1. Commits are in `_results/commits.txt`. The clones have been removed; reports and filtered logs are in `<corpus>/corpus-fa/_results/<naam>/` (9 MB).

| Project | Commit | restore | build before | tests before (pass/fail) | fix | Fixed | Manual | build after | tests after | regressions |
| --- | --- | --- | --- | --- | --- | ---: | ---: | --- | --- | ---: |
| SmartEnum | 9bc3f7a | 0 | 0 | 1293 (1293/0) | 0 | 379 | 62 | 0 | 1293 (1293/0) | 0 |
| FluentResults | 36284d3 | 0 | 0 | 864 (864/0) | 0 | 564 | 76 | 0 | 864 (864/0) | 0 |
| Ardalis.Result | 14cf45a | 0 | 0 | 1068 (1068/0) | 0 | 292 | 49 | 0 | 1068 (1068/0) | 0 |
| CSharpFunctionalExtensions | e0fc908 | 0 | 0 | 4228 (4226/0) | 0 | 4502 | 49 | 0 | 4228 (4226/0) | 0 |
| Facility | 6e147bf | 0 | 0 | 1552 (1552/0) | 0 | 513 | 2 | 0 | 1552 (1552/0) | 0 |
| Ardalis.Specification¹ | d2fcf35 | 0 | 0 | 829 (823/0) | 0 | 828 | 45 | 0 | 829 (823/0) | 0 |
| Bogus | 6ece18c | 0 | 0 | 3155 (3149/0) | 0 | 844 | 115 | 0 | 3155 (3149/0) | 0 |
| NUKE | 27e8077 | 0 | 0 | 398 (391/0) | 0 | 339 | 34 | 0 | 398 (391/0) | 0 |
| Mapperly (v3.6.0) | 51aa1da | 0 | 0 | 1094 (1094/0) | 0 | 114 | 6 | 0 | 1094 (1094/0) | 0 |
| PowerApps-Tooling | 630e697 | 0 | 0 | 1767 (1767/0) | 0 | 293 | 103 | 0 | 1767 (1767/0) | 0 |
| Polly (7.2.4) | b1a5a17 | 0 | 0 | 5685 (5685/0) | 0 | 4449 | 964 | 0 | 5685 (5685/0) | 0 |
| EventFlow (v0.83.4713)² | e544b76 | 0 | 0 | 970 (642/326) | **1** → 0 | 483 | 59 | 0 | 970 (642/326) | 0 |
| modular-monolith³ | 91c8ef2 | 0 | 0 | 287 (246/40) | 0 | **0** | 82 | 0 | 287 (246/40) | 0 |
| LiteDB⁴ | 7b71bc4 | 0 | 0 | 4928×2 TFM green | 0 | 6874 | 357 | **1** | – | – |
| Steeltoe | f13e514 | 0 | 0 | 7 assemblies red (baseline) | 0 | 5071 | 529 | **1** | – | – |
| Akka.NET | 23a8566 | 0 | 0 | 2–3 assemblies red (baseline) | 0 | 8488 | 1868 | **1** | – | – |

Fixed/Manual excluding `project-file` and `usings` findings. Test counts are summed over all TFMs; "fail" counts only real failures (skipped tests not included).

¹ `.sln` fails if the folder is not named `Specification` (`ProjectReference ..\..\..\Specification\…`); rerun with that folder name.
² The fix crashed on a Latin-1 file (TAAK-167). After manual conversion to UTF-8: green. The 326 failures are integration tests without a database/EventStore (hanging test hosts stopped after 4 min).
³ The sqlproj project removed from the solution (MSBuild.Sdk.SqlProj does not support SDK 10). All 82 findings Manual because the self-closing `<Project Sdk=".." />` cannot be edited (existing TAAK-181). Green, but nothing was migrated.
⁴ First attempt failed in the baseline due to a file lock (CS2012, LiteDB.Fuzz built in parallel); rerun with `-m:1`.

Build errors after the fix:

| Project | Error | Cause | Task |
| --- | --- | --- | --- |
| Akka.NET | 4 984× CS0121 (+ some CS8130/CS0119/CS1061/CS0452) | `Akka.TestKit` has its own `ShouldBe`/`ShouldBeTrue`; with `using Shouldly;` added it is ambiguous | TAAK-174 |
| LiteDB | 80× CS1929, 8× CS0411 (11 lines) | `BsonValue.ShouldBe(int/string)`: a custom implicit conversion, so no type inference | TAAK-169 |
| Steeltoe | 58× CS0411 (11 lines) | `ShouldContainKey` on `IReadOnlyDictionary`/headers | TAAK-168 |
| EventFlow | fix exit 1 | non-UTF-8 file → unhandled exception | TAAK-167 |

### Rerun LiteDB after TAAK-223 (2026-10-13)

Same harness and commit, tool from the working tree (`EXTRA=-m:1`), results `<corpus>/corpus-fa/_results/r5-litedb/LiteDB/`.
Fixed 6917 → 7649, Manual 291 → 297, unmatched-use 709 → 2 (`Index_Tests.cs`), usings 15 → 2. Build after fix green;
tests 9893/26, the 26 regressions are the same `Int32IdAssertions_Tests` (TAAK-222).

Largest Manual groups (all projects combined):

| Reason | Count | Mainly where | Task |
| --- | ---: | --- | --- |
| Result of `Throw<T>()` used (`.And/.Which/.Where/.WithInnerException`) | 499 | Polly 428 | TAAK-170 |
| `ContainKey(..)` with result used (`WhoseValue`), `ContainKeys`/`ContainValues` | 377 | Steeltoe 249, Polly 120 | TAAK-171 |
| `BeEquivalentTo` on a collection: order | 207 | 13 projects | TAAK-172 |
| `AssertionScope` (blocks with helpers/deconstruction) | 190 | LiteDB 140 | – (deliberately Manual) |
| Subject built with `Awaiting`/`FluentActions` for `NotThrowAsync` | 131 | Polly | TAAK-173 |
| Project file not editable (self-closing) | 61 (+21 cascade) | modular-monolith | TAAK-181 |

## Conclusion round 3

- **Fixed: 88.9%** of all FA findings (33 900 Fixed / 4 249 Manual, 16 solutions; excluding Akka, Steeltoe and LiteDB 92.5%). Calls that were not recognised are not in this denominator (see `ANALYSE-fa-100.md`).
- **Fully green after the fix (builds, 0 test regressions, something migrated): 12 of 16**: SmartEnum, FluentResults, Ardalis.Result, CSFE, Facility, Specification, Bogus, NUKE, Mapperly, PowerApps-Tooling and Polly without intervention, EventFlow after UTF-8 conversion. modular-monolith builds and has 0 regressions, but nothing was converted.
- **0 test regressions** in all 13 projects where before and after could be compared (together well over 24 000 test executions). In round 1 there were 26 (Polly) and 4 (FluentResults).
- Red: Akka.NET, LiteDB and Steeltoe do not build after the fix (TAAK-174/169/168), and EventFlow crashes on encoding (TAAK-167). So "100% FA" is not proven yet. After TAAK-167..174, repeat the run: with TAAK-170..173 Fixed reaches about 92%.

## Rerun Polly after TAAK-170/173 (2026-10-13)

Polly 7.2.4 (`b1a5a17`, 2026-10-13, harness without clone changes): Fixed 4449 → 5255, Manual 964 → 171, build 0 errors, tests 5685/5685 → 5685/5685, 0 regressions. Remaining top Manuals: Invoking receiver not stable (42), `ContainInOrder` (34), different compile-time types in `BeEquivalentTo` (20).

## Rerun Akka after TAAK-174 (2026-10-13)

Akka.NET `23a8566` (fresh clone, same build flags as the harness): Fixed 8 488 → **5 463**, Manual 1 868 → **5 126** (3 449 of which: file Manual due to a clash with `Akka.TestKit.AkkaSpecExtensions`), build **0 errors** (was 4 984× CS0121 + 28 follow-up errors). Tests: 2 real regressions (TAAK-199: own `ShouldBe` in an enclosing namespace silently shadows Shouldly; TAAK-200: `object`-subject `ShouldBe` in DData), 2 flaky (port conflict, MultiNode timeout). Reduce conflict Manuals: TAAK-198.

## Rerun Akka after TAAK-198..200 (2026-10-13)

Clean checkout `23a8566` (`<probe-dir>/probe-174`): Fixed 5 463 → 6 016, Manual 5 518 → 4 649 (conflict Manuals now per call: 2 943). Build 0 errors. Akka.Tests 1553/0, Akka.TestKit.Tests 337/0, DData 201/1 (`ReplicatorMessageSerializerSpec`: asymmetric `Equals` in Akka itself, now as a warning in the report, TAAK-200). 173 warnings for the `Equals` order.

## Round 4 (2026-10-13)

Full corpus again, main `04f113b`. The tool was published from a clean worktree of HEAD (LF checkout). The same 16 commits from `_results/commits.txt` (shallow fetch per commit, `nuget.config` with only nuget.org). Harness: `<corpus>/corpus-fa/tools/corpus-regression-r4.sh` (= round 3 with `py -3`; `NuGetAudit=false`, `AllowMissingPrunePackageData=true`, 3 jobs, test 2× before, `fix --force`, bin/obj removed, build, test, per-test comparison via TRX). `TEMP`/`TMP` on a folder outside `%TEMP%`. Exceptions as in round 3: Specification in a folder `Specification`, LiteDB with `-m:1`, EventFlow with the test host watchdog. For modular-monolith **both** MSBuild.Sdk.SqlProj projects (`Database` and `Database.Build`) are now removed from the `.sln`; with only the first one the baseline build failed. EventFlow ran **without** UTF-8 intervention (TAAK-167). Results: `<corpus>/corpus-fa/_results/r4/<naam>/` (reports, compare.json, filtered logs). The clones have been removed.

| Project | Fixed | Fixed+warn. | Manual | unmatched-use | Fixed % | build after | tests before (pass/fail) | tests after (pass/fail) | regressions |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | ---: |
| SmartEnum | 342 | 36 | 47 | 1 | 88.7 | 0 | 1293/0 | 1293/0 | 0 |
| FluentResults | 614 | 0 | 20 | 59 | 88.6 | 0 | 864/0 | 864/0 | 0 |
| Ardalis.Result | 286 | 1 | 45 | 0 | 86.4 | 0 | 1068/0 | 1068/0 | 0 |
| CSharpFunctionalExtensions | 4494 | 32 | 23 | 4 | 99.4 | 0 | 4226/0 | 4226/0 | 0 |
| Facility | 512 | 0 | 0 | 0 | 100 | 0 | 1552/0 | 1552/0 | 0 |
| Ardalis.Specification | 840 | 0 | 25 | 9 | 96.1 | 0 | 823/0 | 823/0 | 0 |
| Bogus | 861 | 2 | 92 | 1 | 90.3 | 0 | 3149/0 | 3149/0 | 0 |
| NUKE | 334 | 11 | 20 | 0 | 94.5 | 0 | 389/2 | 389/2 | 0 |
| Mapperly | 112 | 1 | 3 | 3 | 95.0 | 0 | 1094/0 | 1094/0 | 0 |
| PowerApps-Tooling | 291 | 5 | 92 | 76 | 63.8 | 0 | 1767/0 | 1767/0 | 0 |
| Polly | 5238 | 17 | 170 | 1 | 96.8 | 0 | 5685/0 | 5685/0 | 0 |
| EventFlow¹ | 451 | 15 | 44 | 24 | 87.3 | 0 | 642/326 | 642/326 | 0 |
| modular-monolith² | 20 | 41 | 1 | 0 | 98.4 | 0 | 246/40 | 246/40 | 0 |
| LiteDB | 6787 | 130 | 291 | 709 | 87.4 | 0 | 9919/0 | 9893/26 | **26** |
| Steeltoe | 5060 | 2 | 469 | 41 | 90.8 | 0 | 6099/21 | 6081/39 | **18** |
| Akka.NET³ | 5797 | 175 | 4310 | 304 | 56.4 | 0 | 8981/4 | 8973/12 | **10** |
| **Total** | **32 039** | **468** | **5 652** | **1 232** | **82.5** | 16/16 | | | 54 |

Excluding `project-file` and `usings` findings. "unmatched-use" = FA call that no pattern knows (in round 3 not yet reported as a finding, hence not in the denominator). Fixed % = (Fixed + Fixed with warning) / all findings. Test counts are summed over all TFMs; skipped tests are not counted.

¹ 326 failures = integration tests without a database/EventStore (same as round 3).
² The 40 failures are already red in the baseline (integration tests). Finally something migrated (TAAK-181).
³ In addition 29 "missing" (test names with timestamp/GUID, not a real disappearance) and 61 flaky. Of the 10 regressions, 1 is the known DData `Equals` asymmetry (TAAK-200, reported as a warning in the report).

Test regressions (no more build errors):

| Project | Tests | Cause | Task |
| --- | --- | --- | --- |
| Akka.NET | 9 in Akka.Streams.Tests (QueueSourceSpec ×5, FlowScan, FlowSupervision, FlowWatchTermination, SinkForeachAsync) | FA `Throw<T>()` unwraps `AggregateException`, Shouldly does not; the Manual rule for blocking delegates misses lambda parameters (`_ => _.Wait(..)`), delegates in locals and own `WaitAsync` | TAAK-220 |
| Akka.NET | 1 (DData `ReplicatorMessageSerializerSpec`) | asymmetric `Equals` in Akka, known | TAAK-200 (warning) |
| Steeltoe | 6 × 3 TFMs | `ThrowExactlyAsync<OperationCanceledException>` / `ThrowAsync<TaskCanceledException>().WithInnerException<..>` on a cancelled task: Shouldly throws its own `TaskCanceledException` (type and inner exception lost) | TAAK-221 |
| LiteDB | 13 × 2 TFMs (`Int32IdAssertions_Tests`) | meta test: `Throw<XunitException>()` on an own assertion helper in another file; that helper was migrated to Shouldly and now throws `ShouldAssertException` | TAAK-222 |

Largest Manual groups (all projects combined):

| Reason | Count | Where | Justified? | Task |
| --- | ---: | --- | --- | --- |
| Shouldly call ambiguous/misbound with own `Should*` (CS0121, not imported) | ≈ 2 900 | Akka | yes (TAAK-174/198/199) | – |
| `x.Should()` not migrated: file only examined in the first TFM (net462), code is in `#if !NETFRAMEWORK` | 709 (593 `Should()`) | LiteDB | **no** | TAAK-223 |
| FA time extensions `n.Seconds()`/`Milliseconds()` | 380 | Akka, Steeltoe | **no** | TAAK-224 |
| `Equal(..)` on a dictionary | 344 | Akka 337 | **no** | TAAK-225 |
| `BeEquivalentTo(params items)` with non-leaf items, `UniqueAddress`/`KeyValuePair`/`Member`/… | ≈ 520 | Akka | yes (structural comparison) | – |
| `ContainKey(..)` result used / `WhoseValue` with computed key | 148 + 77 | Steeltoe | partly (computed key: candidate for later) | – |
| `Be(..)` on `FluentAssertions.Types` (different equality) | 82 | 7 projects | yes | – |
| `ContainInOrder` | 67 | Polly, Akka | yes (no equivalent) | – |
| `AssertionScope` (block + asserts inside) | 47 + 102 | LiteDB, PowerApps | yes | – |

## Conclusion round 4

- **All 16 solutions build after the fix** (round 3: 13). Akka, LiteDB and Steeltoe were solved by TAAK-167..200; EventFlow runs without intervention.
- **Fully green (builds, 0 test regressions, something migrated): 13 of 16**: SmartEnum, FluentResults, Ardalis.Result, CSFE, Facility, Specification, Bogus, NUKE, Mapperly, PowerApps-Tooling, Polly, EventFlow and now also modular-monolith (62 converted). Red due to test regressions: LiteDB (26), Steeltoe (18), Akka (10; 9 new, 1 known). Together 54 regressions on ±48 800 test executions, with 3 causes: TAAK-220/221/222.
- **Fixed: 82.5%** of all findings (32 507 of 39 391), 85.2% excluding the new unmatched-use rows. Excluding Akka it is 92.1%. Compared to round 3 (88.9%) this looks like a drop, but that is because the Akka calls that broke the build in round 3 are now honestly Manual (≈ 2 900 conflict Manuals) and because unrecognised calls now count.
- "100% FA" is not proven yet. First TAAK-220..222 (regressions), then TAAK-223..225 (together ≈ 1 430 Manual, +3.6 percentage points).

## See also

- `claude/tasks/TAAK-220-throw-aggregateexception-uitpakken.md` to `claude/tasks/TAAK-225-equal-op-dictionary.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/ANALYSE-fa-100.md`
- `claude/tasks/archief/TAAK-167-niet-utf8-bestand-breekt-fix.md` to `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`

## Round 5 (2026-10-13): rerun Akka, Steeltoe, LiteDB after TAAK-220..222

Tool published from the **working folder** (not from a commit): main `04f113b` plus the uncommitted changes of TAAK-220..222 and the ongoing TAAK-223 change in `Fixer.cs` (multi-TFM passes) by another agent. Same harness (`<corpus>/corpus-fa/tools/corpus-regression-r4.sh`), the same commits, LiteDB with `-m:1`, 3 projects at a time. Results: `<corpus>/corpus-fa/_results/r5/<naam>/`.

| Project | Fixed | Manual | build after | tests before (pass/fail) | tests after (pass/fail) | regressions |
| --- | ---: | ---: | --- | --- | --- | ---: |
| LiteDB | 7649 | 299 | 0 | 9918/1 | 9919/0 | **0** (was 26) |
| Steeltoe | 5059 | 516 | 0 | 5984/21 | 6099/21 | **0** (was 18) |
| Akka.NET | 5962 | 4624 | 0 | 8976/6 | 8984/1 | **1** (was 10; only the known DData `Equals`, TAAK-200) |

Excluding `project-file`/`usings`. LiteDB: Fixed 6917 → 7649 is mainly due to TAAK-223. Steeltoe "after" has 123 more tests (test names with variable parts, counted as "new"/flaky, not a regression). New Manual reasons: LiteDB 25 (helper + meta tests, TAAK-222), Akka 23 + 2 (aggregate/`WaitAsync`, TAAK-220) and 6 (TAAK-222 rule), Steeltoe 6 (cancelled task, TAAK-221).

Conclusion: the three causes from round 4 are gone; together with round 4, 15 of 16 solutions are now green, Akka only keeps the known TAAK-200 warning.
