# TAAK-268 — Blind validation round: 3 unseen projects per fix

**Status**: done — 3 rounds, 18 unseen repos, 0 regressions; every defect found is fixed (see `CORPUSRUN-blind.md`)
**Created**: 2026-10-15
**Goal**: every fix (fluentassertions, mediatr, automapper) has run on 3 open-source projects that played no part in developing it: 0 regressions, and every Fixed and Manual finding has been checked by hand.

## Context for the next agent

- The corpora so far (FA round 5, MediatR round 6, AutoMapper TAAK-258) were also used to *develop* the patterns, so they overstate quality. This round is a blind test: pick the projects first, change nothing in the tool during the run.
- Exclusion list (never pick these): every repo in `claude/tools/corpus-am-repos.txt`, the FA corpus (akka, aresult, bogus, csfe, eventflow, facility, fresults, litedb, mapperly, modmono, nuke, polly, powerapps, smartenum, spec, steeltoe), the MediatR corpus (jason, ardalis, amantin, nadirbad, eShopOnWeb, dotnet/eShop, kgrzybek), the survey lists in `docs/projectinfo/architectuur/corpus-automapper.md` and `corpus-mediatr-automapper.md`, and `samples/`.
- Harness: `claude/tools/corpus-am.sh` (modes `base|scan|after`, `compare` via `corpus-regression.py`). Since TAAK-258 `after` builds before the fix and keeps `node_modules/**/bin`. It is AutoMapper-specific only in `--fix automapper`.
- Windows: `TEMP`/`TMP` on a `D:` folder (exe in `%TEMP%` is blocked); the summary of `corpus-regression.py` needs `PYTHONIOENCODING=utf-8`.

## Analysis

### Current situation

`corpus-am.sh` hard-codes `--fix automapper` (`claude/tools/corpus-am.sh`, the `scan` and `after` steps). There is a separate harness for FA/MediatR (`corpus-regression.sh`). The results of the earlier rounds sit in `claude/tasks/CORPUSRUN-*.md`.

### Problem

- The 0-regression numbers come from projects that guided the implementation (overfitting risk).
- Corpus runs only count regressions; nobody checks whether a Fixed rewrite is semantically right in code the tests do not cover, or whether a Manual reason is justified.

### New situation

One `CORPUSRUN-blind.md` with 9 projects (3 per fix), each with base/after results, 0 regressions, and a per-finding review (sample when > 30 findings). Every defect found becomes its own TAAK.

### Improvement

An honest launch number (TAAK-019): quality measured on unseen code, not on the training set.

## Approach

### What

A `FIX` env var in `corpus-am.sh` (default `automapper`), a repo list per fix, a run per fix, and a manual review.

### Where

- `claude/tools/corpus-am.sh` — `--fix "$FIX"` instead of `--fix automapper`
- `claude/tools/corpus-blind-repos.txt` (name, repo, pinned sha), `claude/tools/corpus-blind-list.txt` (name, sln, fix)
- `claude/tasks/CORPUSRUN-blind.md` — results and review

### How

1. Selection (GitHub search via `gh`): per fix, 3 repos that use the library (FA ≤ 7.x, MediatR ≤ 12.x, AutoMapper ≤ 14.x), build on .NET 8+ without npm/cargo/docker, have tests that exercise the library code (for FA: the test suite itself), and vary in size (one small, one medium, one large). Pin the sha.
2. `clone` → `base` (build + tests x2) → `scan` → `after` with the tool from a fixed main sha.
3. Review: Fixed rewrites against the semantics of the old library; Manual reasons against the code (justified / could be migrated / wrong reason).

### Why this approach

Reusing the existing harness keeps the numbers comparable with earlier rounds. Picking all repos *before* the run, and not changing the tool during it, keeps the test blind. Fixes go into follow-up tasks and a second round, not into this one.

## Steps

- [x] 1. `corpus-am.sh`: `FIX` env var + `fix` column in the list; check with one AutoMapper repo that the outcome is unchanged
- [x] 2. Select 3 repos per fix (criteria above, exclusion list), record why each was picked; the user may veto
- [x] 3. Publish the tool from the current main sha into `<corpus>/corpus-blind/_results/tool-<sha>/`
- [x] 4. `clone` + `base` for all 9; replace a repo whose baseline does not build (record why)
- [x] 5. `scan` + `after` for all 9; summary table in `CORPUSRUN-blind.md`
- [x] 6. Manual review of Fixed and Manual per repo; one TAAK per defect (regression, wrong rewrite, unjustified Manual)
- [x] 7. Report to the user: per fix Fixed/Manual/regressions + review verdict; update `overdracht.md` and `INDEX.md`

## Risks & edge cases

| Risk | Mitigation |
| --- | --- |
| Few unseen repos still use FA ≤ 7 / MediatR ≤ 12 / AutoMapper ≤ 14 | Search on the pinned package version in `*.csproj`/`Directory.Packages.props`; an older commit is allowed (pin it) |
| Baseline does not build (front-end, docker, secrets) | Replace it in step 4, before the tool runs (keeps the test blind) |
| Flaky tests look like regressions | Two baseline runs; `compare` marks flaky; check regressions by hand |
| Temptation to fix the tool during the run | Forbidden in this task: defects go to follow-up tasks and a round 2 |
| Untrusted clones | Only restore/build/test/tool; never run their scripts (as `corpus-am.sh` already does) |

## Changed files

- `claude/tools/corpus-am.sh` — `FIX`/`REPOS` env vars and an optional fix column (step 1; checked: scan of `jason` with the old and new script and the same tool gives a byte-identical report)
- `claude/tools/corpus-blind-repos.txt`, `claude/tools/corpus-blind-list.txt` (step 2)
- `claude/tasks/CORPUSRUN-blind.md`

## See also

- `claude/tasks/CORPUSRUN-automapper.md`
- `claude/tasks/CORPUSRUN-fa.md`
- `claude/tasks/TAAK-019-lancering.md`
- `claude/docs/INDEX.md`
