# Corpus regression run (2026-10-11)

Harness: `claude/tools/corpus-regression.sh` (+ `.py`). Per solution: fresh copy → restore → build → test 2× (TRX, flaky detection) → `fix --force` → remove bin/obj → build → test → comparison **per test**. Tool published from main `7edb002` (before TAAK-070/118). Output: `<corpus>/out/<naam>/`.

| Solution | status | tests before (pass/fail) | tests after (pass/fail) | regressions | failed→passed | disappeared | new | flaky | duration |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| akka | fix-faalt | | | | | | | | 906s |
| ardalis | ok | 9 (9/0) | 9 (9/0) | 0 | 0 | 0 | 0 | 0 | 61s |
| aresult | ok | 741 (741/0) | 741 (741/0) | 0 | 0 | 0 | 0 | 0 | 83s |
| bogus | ok | 1074 (1072/0) | 1074 (1072/0) | 0 | 0 | 0 | 0 | 0 | 56s |
| csfe | ok | 4228 (4226/0) | 4228 (4226/0) | 0 | 0 | 0 | 0 | 0 | 96s |
| eshoponweb | ok | 74 (74/0) | 74 (74/0) | 0 | 0 | 0 | 0 | 0 | 66s |
| evo1 | BUILD-NA-FIX-FAALT | | | | | | | | 32s |
| evo2 | BUILD-NA-FIX-FAALT | | | | | | | | 69s |
| fresults | ok | 576 (576/0) | 576 (572/4) | 4 | 0 | 0 | 0 | 0 | 67s |
| litedb | baseline-build-faalt | | | | | | | | 23s |
| nuke | ok | 398 (388/3) | 398 (388/3) | 0 | 0 | 0 | 0 | 0 | 112s |
| polly | ok | 3790 (3790/0) | 3790 (3764/26) | 26 | 0 | 0 | 0 | 0 | 423s |
| powerapps | ok | 1178 (1172/6) | 1178 (1172/6) | 0 | 0 | 0 | 0 | 0 | 530s |
| smartenum | ok | 1293 (1293/0) | 1293 (1293/0) | 0 | 0 | 0 | 0 | 0 | 104s |
| steeltoe | fix-faalt | | | | | | | | 829s |

(Harness status values: `fix-faalt` = fix fails, `BUILD-NA-FIX-FAALT` = build after fix fails, `baseline-build-faalt` = baseline build fails.)

## Conclusion

**Of the 10 solutions whose tests could be compared before and after, 8 are identical per test** (ardalis, Ardalis.Result, Bogus, CSFE, eShopOnWeb, NUKE, SmartEnum; together well over 7 800 tests). Two had regressions:

| Solution | Regressions | Cause | Status |
| --- | ---: | --- | --- |
| FluentResults | 4 | Assertion in lambda with `Throw<XunitException>` | **fixed** in TAAK-070 (merged after the run; harness on FluentResults with the new tool: 576/576, 0 regressions) |
| Polly | 26 | Own helper `Within(timeSpan, Action)` only catches `AssertionFailedException`/`XunitException` to retry. Shouldly throws `ShouldAssertException`, which is not caught → no retry → test fails | **fixed** in TAAK-140 (harness on Polly 7.2.4, unchanged source from tag `7.2.4` in `src/polly`, tool from main `d252749`: 3790/3790, 0 regressions; output `<corpus>/t140/polly/`) |

## PowerApps-Tooling via MTP (TAAK-143)

The first run gave 0 tests (the harness forced VSTest). After TAAK-143 the harness detects Microsoft.Testing.Platform and runs `dotnet test --solution … --report-trx`. Repeated on a fresh upstream tarball (output `<corpus>/t143/`): **1178 tests, identical per test, 0 regressions** (293 Fixed, 103 Manual). The 6 failing tests already fail before the fix. VSTest path check: ardalis 20 (19/1) → identical. The powerapps row above has been updated with this run.

## Not comparable (no regression verdict possible)

| Solution | Reason | Kind |
| --- | --- | --- |
| evo1, evo2 | Build after fix fails (IDE0060: unused `assembly` parameter after `AddMediator`; TAAK-117 only gave a warning) | **fixed** in TAAK-141 (`_ = assembly;`); evo2's CS0104/CS0122/CS0436/CS0535: **TAAK-144** (name conflict fixed; generator conflict → Manual, harness evo1 45/45, evo2 unchanged 39/39); TAAK-147: evo1 now Manual (internal method called via `InternalsVisibleTo`), 0 regressions |
| Akka.NET, Steeltoe | Unshackle does not load the solution: AOT/benchmark/sample projects whose types do not resolve (302 and 502 errors respectively) | product: loader (TAAK-112/135 do not cover this yet) |
| LiteDB | Baseline build already fails (`LiteDB.Fuzz`) | environment/project |

## Open follow-up (not started, at the user's request)

- Loader: load Akka/Steeltoe (skip projects with unresolved types if they do not use FA/MediatR; broaden rules from TAAK-112/135).

## See also

- `claude/tasks/CORPUSRUN-fa.md`, `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tools/corpus-regression.sh`
