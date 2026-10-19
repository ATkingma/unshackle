# TAAK-019 — Launch FluentAssertions → Shouldly (roadmap step 4)

**Status**: 0.2.0 released 2026-10-19 (tag `v0.2.0`; blind round 6: 36/36 build, 0 regressions; decisions by the council). 0.1.0 released on nuget.org 2026-10-17 (tag `v0.1.0`, release run green, Trusted Publishing; install from nuget.org checked). Repo public, branch protection on `main` (PR + "Build and test", no force push; admins may push directly), `.vibe-wise/` removed from history. Left: step 8, the user posts the reddit draft.
**Created**: 2026-10-11
**Goal**: Unshackle 0.1 is public on GitHub and NuGet, proven on a real open-source project, with a README that shows in 30 seconds what it does.

## Context for the next agent

- Roadmap state 2026-10-11: step 1 done, step 2 ~90% (TAAK-017 project files, TAAK-018 collaboration test), steps 3/5/6 not started yet. User decision: **launch first, then MediatR** (PROJECT.md: "Speed matters").
- The tool works: `unshackle list|scan|fix` (`--dry-run`, `--force`, `--report`), `dotnet pack` + `dotnet tool install` tested (TAAK-005, `0.1.0-preview` in `src/Unshackle.Interface/Unshackle.Interface.csproj`). The README already has a Usage section (PowerShell + bash).
- Repo `ATkingma/unshackle` is **private**: branch protection and issues for outsiders only work once it is public.
- `.vibe-wise/` (personal learning notes): removed from the index and added to `.gitignore` (2026-10-15). Still in the git history; rewriting it needs a force push, only with the user's approval.

## Analysis

### Current situation

Working tool, 15 patterns, docs in `docs/projectinfo/`, CI green, issue forms (`.github/ISSUE_TEMPLATE/`, `docs/issues.md`). Not published, not tested on other people's code.

### Problem

- Nobody knows it exists; others may release separate converters in the meantime.
- Behaviour on large, real codebases (hundreds of files, multi-target, analyzers) is unproven.

### New situation

Public repo with branch protection, NuGet package `Unshackle`, README with a GIF and a real-world case, a post on r/dotnet.

### Improvement

First users and feedback via issues; proof that the tool works on real code.

## Approach

### What / Where / How

1. **Real case**: pick an open-source project with a lot of FluentAssertions 7.x (candidates: see PROJECT.md "Proof", e.g. RoseMCP; or search GitHub for `FluentAssertions` 7.x + many `.Should()`). Clone into a scratch folder, `unshackle scan` → `fix` → `dotnet build` + `dotnet test`. Record numbers of Fixed/Manual, build errors and test failures in this task; every error in a pattern = a new issue/task, not solved here.
2. **Make public**: only with the user's approval (sort out `.vibe-wise/` first). Then branch protection on `main` (PR + green check "Build and test"), see `claude/docs/agent-werkwijze.md` §6.
3. **NuGet**: version `0.1.0`, `PackageReadmeFile`, `RepositoryUrl`, `PackageLicenseExpression` (MIT, see LICENSE), `PackageTags`. Publish via a release workflow (`.github/workflows/release.yml` on tag `v*`, `NUGET_API_KEY` as a secret: the user sets the secret themselves).
4. **README**: GIF or asciinema of `scan` + `fix --dry-run` on SampleShop, the case from step 1 with numbers, list of supported patterns (link to `docs/projectinfo/modellen/patronen.md`), known limitations (Manual cases).
5. **r/dotnet post**: draft in `docs/projectinfo/lancering/reddit-post.md`; the user posts it themselves.

### Why this approach

The real case first: if it fails, you want to know before the public launch.

## Result of the real case (2026-10-11)

- **Project**: `microsoft/PowerApps-Tooling` @ `630e6973e5c81ddc196283cde26dd696d619d6c7`, `src/PASopa.sln`, FluentAssertions 7.2.0, MSTest, ~403 `.Should()`, TFMs `net48;net8.0;net10.0`.
- Adjusted locally for the baseline: `global.json` SDK 10.0.401 → 10.0.100 (only 10.0.112 installed). net48 tests do not run on Linux (no mono), in both runs.
- **Baseline**: build green (0 warnings). Test: 1178 total, 1172 passed, 6 failed (`StressTestApps` x3 apps x2 TFMs in PAModelTests, also before the fix; environment).
- **Scan/fix**: the tool reports 778 Fixed / 162 Manual, but counts every finding per TFM (3x). Unique: **262 Fixed, 53 Manual** in 25 changed .cs files + `Directory.Packages.props` + Testing csproj. See TAAK-022.
- Biggest Manual reasons: `Contain(IEnumerable)` (6), `Be(...)` with becauseArgs (6), unknown type/null literal (5), result of `Throw<T>()` used (5), `ThrowExactly` (3), `.And/.Which` after NotBeNull/Be (3+3), BeEquivalentTo different types (3). Project file: Persistence.Tests and PAModelTests keep FA (Manual cases remain).
- **Build after fix**: 8 unique errors (x3 TFM = 21+), all in `Persistence.Tests`: CS8602 after fa.have-count (`x.Count` on nullable) and CS8604 after fa.contain on `string?`. See TAAK-021.
- **Tests after fix**: PAModelTests 530 total, 524 passed, the same 6 `StressTestApps` failed → no regression. Persistence.Tests (648 in the baseline) cannot run: does not build.
- Conclusion: not launchable yet with this case; first TAAK-021, then the case again.

### Repeat on main `b53e3cc` (2026-10-11)

- Clone reset to `630e697` (`git checkout -- . && git clean -fdx`), `global.json` back to 10.0.100. After `git clean` the solution only loads after `dotnet restore` (net48 reference assemblies come from NuGet); `fix` asks for `--force` because `global.json` changed.
- **Fix**: the tool reports **239 Fixed / 92 Manual** (331 rows, 326 unique file+line+pattern). TAAK-022 works: no more x3 per TFM. 24 files written.
    - Fewer Fixed and more Manual than the previous unique count (262/53), mainly due to the new AssertionScope rule: 14x `fa.assertion-scope` + 24x `fa.be-true-false` inside a scope (deliberately Manual).
    - Duplicate rows: the 3 csproj files appear both as Fixed (Shouldly added) and as Manual (FA stays); `PaArchiveTests.cs:400` (contain) and `MsappPackingServiceTests.cs:335` (all-satisfy) give two Manual rows on one line (two calls). Not an error.
- **Top Manual reasons**: AssertionScope (38), `Contain(IEnumerable)`/non-standard form/result used (8), `Throw` result used/ThrowExactly/Invoking (10, + 1 ThrowAsync), BeEquivalentTo unknown type/different types/becauseArgs/ImmutableDictionary (10), `Be(...)` becauseArgs or result used (9), `.And/.Which` after NotBeNull/NotBeEmpty/HaveCount/NotThrow (7), AllSatisfy non-standard form (4), project-file FA stays (3).
- **Build after fix**: green, 0 errors, 0 warnings (TAAK-021 solved).
- **Tests after fix**: 1178 total, 6 failed (the same `StressTestApps` x3 x2 TFMs as the baseline), Persistence.Tests now green → **no regression**.
- No new pattern errors found, so no new tasks. The case is launchable; these are the numbers for the README.

## Steps

- [x] 1. Wait until TAAK-017 + TAAK-018 are pushed (checks green)
- [x] 2. Real case: scan/fix/build/test on an open-source project, record the results here
- [x] 3. Record pattern errors found as issues/tasks (do not fix them in this task)
- [x] 4. NuGet metadata + release workflow (`v*` tag), check `dotnet pack` locally
- [x] 5. README: GIF, case numbers, supported patterns, limitations
- [x] 6. With the user: sort out `.vibe-wise/`, make the repo public, enable branch protection
- [x] 7. Tag release `v0.1.0` (publishing via NuGet Trusted Publishing, policy `unshackle-release` on nuget.org for `ATkingma/unshackle` `release.yml`; no API key), test installation from nuget.org
- [ ] 8. Draft r/dotnet post (draft ready, see the status line); the user posts it

## Risks & edge cases

| Risk | Mitigation |
| ---- | ---------- |
| The real case yields many Manuals or build errors | Honest in the README; errors become issues; launch only when `fix` leaves the case building |
| Personal notes made public | Step 6 with the user, before making it public |
| NuGet key in the repo | Only as a GitHub secret, never in files |
| Making public / posting / publishing is external and irreversible | Have every step explicitly confirmed by the user |

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/archief/TAAK-021-fa-nullable-subject.md` — pattern error from the case
- `claude/tasks/archief/TAAK-022-scan-telt-per-target-framework.md` — count per TFM
- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `docs/projectinfo/project.md` — roadmap, risks, proof of demand
- `claude/docs/overdracht.md` — state at an account switch
