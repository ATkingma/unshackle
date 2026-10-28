# Corpus run EF bulk (TAAK-342 step 9)

## Round 1 (2026-10-28, Linux)

Tool published from branch `efbulk-corpus` (origin/main merged, `6fe378a2`) to `corpus-efbulk/tool`. Clones, results and logs on the
corpus disk (`<corpus>/corpus-efbulk/{src,_results/{base,scan,after}/<name>}`), own NuGet cache under
`corpus-efbulk/nuget`, TMPDIR on the corpus disk. Driver `claude/tools/corpus-efbulk-run.sh` -> `corpus-efbulk-am.sh` (`--max-degree 4`);
the last two repos ran with `EXTRA=-m:2`, two at a time. `after` = build, `fix --fix efbulk --force`, clean, restore, build, test, compare
per test with base (`corpus-regression.py`). Repos and solutions: `claude/tools/corpus-efbulk-{repos,list}.txt`.

| Repo | Baseline | After | Fixed | Manual (calls) | Tests base -> after |
| ---- | -------- | ----- | ----- | -------------- | ------------------- |
| protagonist | ok | ok | 3 (1 delete, 2 update) | 4 update | 4039/4043 -> 4039/4043, 0 regressions |
| plutus | ok | ok | 10 (1 delete, 9 update), package removed | 0 | 2/2 -> 2/2, 0 regressions |
| morenote | ok (Logic project, no tests) | ok | 26 (11 delete, 15 update) | 2 update, 24 unsupported | no tests |
| nexusstack | ok (no tests found) | ok | 3 delete | 3 update | no tests |
| lavcode | ok (no tests) | ok | 0 | 9 delete, 4 update (EF Core 6) | no tests |
| openttddiscord | ok | ok | 7 delete, package removed (2 projects) | 0 | 166/197 -> 166/197, 0 regressions (1 flaky Akka test) |
| damselfly | ok | ok | 0 | 0 | no tests; uses EFCore.BulkExtensions `BatchDelete`/`BatchUpdate` only (out of scope, B2) |
| yofi | baseline-build-fails (MSB3073: `pwsh` missing) | BUILD-AFTER-FIX-FAILS, same 2 MSB3073 as baseline | 0 | 0 | excluded; no Z usage at this commit |
| sharding-core | ok (tests fail in base) | rerun alone: ok, `fix` 56 s (round 1 hung 1800 s next to yofi) | 0 | 2 update (EF Core < 7), 3 unsupported, 2 project-file, 1 unmatched-use | 0 files written; tests fail in base |

Totals over 7 completed repos: 49 Fixed calls, 0 build breaks caused by the fix, 0 test regressions.

## Manual reasons

| Reason | Count | Where | Assessment |
| ------ | ----- | ----- | ---------- |
| `NoNativeExecute` + project row `EfCoreBelow7` | 13 (+2 in the sharding-core scan) | lavcode (EF Core 6.0.8), sharding-core | correct: no `ExecuteDelete`/`ExecuteUpdate` |
| `UnsupportedPattern` (`Unsupported`) | 24 (+3 scan) | morenote, sharding-core | correct: Future/FromCache/Bulk* etc. stay |
| `UpdateValueNotProvable` | 6 | protagonist: `Math.Max`, 3 conditionals; morenote: 2 `DateTime.Now` | correct per B4 (method calls/conditionals/`DateTime.Now` stay Manual); input for a future allow-list widening |
| `UpdateFactoryShape` | 3 | nexusstack `ServiceBase`: factory passed in as a parameter | correct: not an inline initializer |
| kept package / unmatched-use rows | per project | protagonist, morenote, nexusstack, lavcode | correct, except the rows below |

## Bugs

- **TAAK-356**: morenote `MoreNote.Config`, `MoreNote.CryptographyProvider`, `MoreNote.SignatureService` reference no Z package but get the
  kept-for-dependent row with the B13 fallback name "Plus or Extensions". Fixed: they get Z only through `MoreNote.Models`;
  `ProjectPackages.KeepForDependents` no longer keeps a project that gets the package from a project it references (all fixes).

## Not a confirmed bug

- sharding-core `fix` hang: not reproduced. Rerun alone (2026-10-28, same tool, `corpus-efbulk-run.sh after sharding-core`): status ok,
  `fix` loaded and finished in 56 s (8 Manual, 0 files written), whole run 1004 s. Round 1's hang came from running next to the yofi build
  on a loaded machine; no task opened.

## See also

- `claude/tasks/TAAK-342-ef-bulk.md` — EF bulk fix
- `claude/tasks/archief/TAAK-356-efbulk-kept-row-without-z.md` — kept row on a project without Z
- `docs/projectinfo/architectuur/candidate-efbulk.md` — scout report with the repo selection
