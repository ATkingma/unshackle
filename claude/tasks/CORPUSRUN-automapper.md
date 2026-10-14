# Corpus run AutoMapper (row 16 of `automapper-queue.md`)

Preparation round (TAAK-258, 2026-10-14): baseline tests on the 20-repo survey corpus
(`docs/projectinfo/architectuur/corpus-automapper.md`) and a report-only `scan` with the tool from `main` 4661eba.
No fix has been applied yet; the after-fix round is described at the bottom.

## Harness

Outside the repo, in `<corpus>/corpus-automapper/tools/` (adapted from the FA harness `<corpus>/corpus-fa/tools/corpus-regression-r4.sh`):

- `corpus-am.sh base|scan|after <name> <sln>`, `corpus-am.sh list <mode> tools/list.txt` (JOBS=2), `corpus-am.sh summary <mode>`.
- `corpus-regression.py` (copy of the FA helper): pins every `global.json` to SDK 10, picks VSTest/MTP, compares TRX per test.
- `watchdog.ps1`: kills only testhosts whose command line is under the work root or `_results`, older than 20 min. Never other dotnet processes.
- `list.txt`: 18 repos (abp and iotsharp skipped). Sources `<corpus>/corpus-automapper/src/<name>` are untrusted clones: only `dotnet restore/build/test` and the tool run on them.
- Per run: fresh copy (without `.git`, `bin`, `obj`, `node_modules`) into a short work root `<work>/<mode>-<name>` (MAX_PATH: altinnauth fails under the deeper `_results` path); a root `nuget.config` with only nuget.org when the repo has none (the machine config has a second source → NU1507 with central package management); the work copy is deleted afterwards.
- Env: `DOTNET_ROLL_FORWARD=Major`, `DOCKER_HOST=tcp://127.0.0.1:1` (container tests fail fast), TEMP/TMP under the corpus temp dir, `MSBUILDDISABLENODEREUSE=1`, msbuild `-p:NuGetAudit=false -p:AllowMissingPrunePackageData=true -p:SkipNSwag=True -p:WarningsNotAsErrors=NU1507`, step timeout 1800 s (scan 3600 s).
- Modes: `base` = restore → build → test ×2 (flaky = outcome differs between the two). `scan` = restore → build → `unshackle scan --fix automapper --report`. `after` = fresh copy → restore → `fix --fix automapper --force` → remove bin/obj → restore → build (fails: stop) → test → compare against `_results/base/<name>`.
- Results: `<corpus>/corpus-automapper/_results/{base,scan}/<name>/` (`steps.txt`, `status.txt`, `logs/`, `trx-*`, `compare.json`, `report.json`); tool in `_results/tool-4661eba/`.

## Baseline (2026-10-14)

Tests summed over all assemblies; "fail" = real failures (skipped not counted).

| Repo | build | tests (pass/fail) | flaky | Remark |
| --- | --- | --- | ---: | --- |
| altinnauth | ok | 534 (249/285) | 0 | failures: container/DB tests (Docker off) |
| cats | ok | 309 (309/0) | 0 | |
| dasblog | ok | 70 (51/19) | 0 | |
| devarch | ok | 37 (37/0) | 0 | |
| eshoponweb | ok | 74 (74/0) | 0 | |
| jason | ok | 41 (39/2) | 0 | |
| jhipster | ok | 146 (146/0) | 0 | |
| kavita | ok | 3326 (3305/0) | 28 | |
| letterbook | ok | 616 (537/72) | 130 | many flaky: excluded per test from regression counting |
| neozhu | ok | 42 (13/29) | 0 | solution is `CleanArchitecture.Blazor.sln` (corpus `repos.txt` says `CleanArchitecture.sln`) |
| nop | ok | 1060 (1051/0) | 1 | |
| personalasst | ok | 131 (131/0) | 0 | |
| skoruba | ok | 272 (272/0) | 0 | |
| xipona | ok | 1193 (969/81) | 32 | |
| bitwarden | **fails** | – | – | MSB3073 `cargo build` (run with `-p:TreatWarningsAsErrors=false`; without it also CA1305/CA1304/BWA0001 under SDK 10.0.400) |
| gnomeshade | **fails** | – | – | MSB3030: npm `node_modules` missing in the web project |
| lancommander | **fails** | – | – | MSB3073 `npm run package` |
| ombi | **fails** | – | – | MSB3073 `yarn install` |

14 usable baselines, 7851 tests, 191 flaky (mostly letterbook). The 4 failing builds are front-end/Rust build steps,
not AutoMapper; a solution filter without that project would make them usable (not done).

## Scan with main 4661eba (report only)

| Repo | Fixed | Manual | Top Manual patterns |
| --- | ---: | ---: | --- |
| altinnauth | 0 | 14 | map-call 12 |
| bitwarden | 0 | 271 | map-call 202, create-map 60 |
| cats | 0 | 137 | create-map 63, for-member-map-from 32, map-call 32 |
| dasblog | 0 | 54 | map-call 48 |
| devarch | 0 | 4 | map-call 2 |
| eshoponweb | 1 | 0 | – |
| gnomeshade | 0 | 56 | map-call 53 |
| jason | 0 | 13 | create-map 7, configuration-test 4 |
| jhipster | 0 | 126 | map-call 91, configuration-test 33 |
| kavita | 0 | 164 | create-map 105, map-call 52 |
| lancommander | 0 | 49 | map-call 39 |
| letterbook | 0 | 156 | map-call 135 |
| neozhu | 0 | 58 | create-map 36, map-call 8 |
| nop | 0 | 5 | 1 each |
| ombi | 0 | 78 | map-call 71 |
| personalasst | 0 | 52 | map-call 35, create-map 11 |
| skoruba | 0 | 120 | map-call 100, for-member-map-from 14 |
| xipona | 0 | 16 | map-call 12, configuration-test 4 |
| **Total** | **1** | **1373** | |

Only eshoponweb would be migrated. Everywhere else the all-or-nothing rule holds everything back: one Manual finding
blocks every rewrite in the solution, and the blocked map calls then report "no CreateMap in a profile this fix rewrites".

### Top 10 Manual reasons (normalised)

| # | Count | Pattern | Reason |
| --- | ---: | --- | --- |
| 1 | 685 | map-call | No `CreateMap<A,B>` in a profile this fix rewrites, and no `A.ToB()` exists (mostly a cascade of the reasons below) |
| 2 | 192 | create-map | `ProjectTo` / `Map` with options not migrated yet (`automapper.unsupported`); while they exist, CreateMap rewrites stay |
| 3 | 107 | map-call | `Map<List<T>>` maps a collection; write it by hand with `Select` |
| 4 | 54 | map-call | Source of `Map` may be null (AutoMapper returns null) |
| 5 | 46 | configuration-test | Not rewritten while other findings are Manual |
| 6 | 28 | create-map | Not rewritten while other findings are Manual |
| 7 | 15 | map-call | Not rewritten while other findings are Manual |
| 8 | 15 | for-member-map-from | Not rewritten while other findings are Manual |
| 9 | 14 | map-call | `Map<IEnumerable<T>>` maps a collection |
| 10 | 9 | configuration-test | `new MapperConfiguration` that is not `AddProfile`/`AddMaps` |

Suggested order before the after-fix round: `ProjectTo`/options (reason 2), collection maps (3, 9), nullable sources (4);
these unblock the cascade in reasons 1 and 5–8.

## After-fix round (how to run)

1. Publish the tool from a clean worktree of `main`: `dotnet publish src/Unshackle.Interface -c Release -o <corpus>/corpus-automapper/_results/tool-<hash>`.
2. Start `tools/watchdog.ps1`.
3. `TOOL=<corpus>/corpus-automapper/_results/tool-<hash>/unshackle.dll bash tools/corpus-am.sh list after tools/list.txt` (reuses `_results/base`; bitwarden needs `EXTRA=-p:TreatWarningsAsErrors=false` as in its baseline).
4. `bash tools/corpus-am.sh summary after`: regressions per test, flaky tests from the two base runs excluded. Build errors in `_results/after/<name>/build1-errors.txt`.

## See also

- `claude/tasks/TAAK-258-automapper-corpus-baseline.md`
- `claude/tasks/automapper-queue.md` (row 16)
- `docs/projectinfo/architectuur/corpus-automapper.md`
- `claude/tasks/CORPUSRUN-fa.md`

## Rescan on Linux with main 6d7e3c9 (TAAK-262, report only)

The corpus now lives on Linux (24 cores, 31 GB) under `<corpus>/corpus-automapper/` (on this Linux machine on the large data disk) (`src/<name>` = untrusted
shallow clones at the pinned commits, `_results/scan/<name>/`, tool in `_results/tool-6d7e3c9/`). Harness in the repo:
`claude/tools/corpus-am.sh` (+ `corpus-am-repos.txt`, `corpus-am-list.txt`, `corpus-regression.py`); see its header for the modes.
Run: `TOOL=<corpus>/corpus-automapper/_results/tool-<sha>/unshackle.dll bash claude/tools/corpus-am.sh list scan claude/tools/corpus-am-list.txt`.
Build step: all repos but cats, gnomeshade, lancommander and bitwarden build; those four fail on the front-end/Aspire/cargo step as in the
baseline, and the scan still runs on the rest of the solution. bitwarden only restores with `NOLOCK=1` (its `packages.lock.json` files
break a nuget.org-only restore with NU1101) and its scan takes about 50 minutes (the others 10-160 s).

| Repo | Fixed | Manual | Top Manual patterns | Was (4661eba) |
| --- | ---: | ---: | --- | ---: |
| altinnauth | 0 | 14 | map-call 12 | 14 |
| bitwarden | 0 | 271 | map-call 202, create-map 57 | 271 |
| cats | 0 | 125 | project-to 37, for-member-map-from 32, create-map 26 | 137 |
| dasblog | 0 | 54 | map-call 48 | 54 |
| devarch | 0 | 4 | map-call 2 | 4 |
| eshoponweb | 1 | 0 | – | 0 (1 Fixed) |
| gnomeshade | 0 | 56 | map-call 53 | 56 |
| jason | 0 | 13 | create-map 4, configuration-test 4, project-to 3 | 13 |
| jhipster | 0 | 126 | map-call 91, configuration-test 33 | 126 |
| kavita | 0 | 164 | project-to 105, map-call 52 | 164 |
| lancommander | 0 | 49 | map-call 39, project-to 7 | 49 |
| letterbook | 0 | 156 | map-call 135, configuration-test 10 | 156 |
| neozhu | 0 | 58 | project-to 23, create-map 13, map-call 8 | 58 |
| nop | 0 | 5 | 1 each | 5 |
| ombi | 0 | 78 | map-call 71 | 78 |
| personalasst | 0 | 52 | map-call 35, create-map 11 | 52 |
| skoruba | 0 | 120 | map-call 100, for-member-map-from 14 | 120 |
| xipona | 0 | 16 | map-call 12, configuration-test 4 | 16 |
| **Total** | **1** | **1361** | | **1 / 1373** |

Still 1 Fixed (eshoponweb): the all-or-nothing rule per solution holds everything back; only cats changed (-12 Manual). The counts did not move
because TAAK-259..261 changed the reasons, not what is blocked. About 300 of the 1361 are cascade ("Not rewritten while other findings are
Manual": bitwarden 150, jhipster 33, ombi 27, skoruba 27, cats 33); the rest name the real blocker, grouped here (Manual findings):

| # | Blocker (root cause) | Repo | Blocked |
| ---: | --- | --- | ---: |
| 1 | `CreateMap<BookmarkSeriesPair, BookmarkDto>`: `MapFrom` for `Id` dereferences `src.Bookmark`, which may be null | kavita | 145 |
| 2 | Condition or `ForPath` on a reverse map in `AutoMapperProfile` | jhipster | 89 |
| 3 | `Mention` has several constructors (`CreateMap<MentionDto, Mention>`), plus `ConstructUsing`/`MaxDepth` maps (`ModerationReport`, `Post`, `Uuid7`) | letterbook | 42 + 39 |
| 4 | `MapFrom` dereferences a possibly-null source member (`UserProjectAccessPolicy` reverse map; others: ombi `TvMazeSearch` 16, skoruba `UserClaims` 21, personalasst, dasblog, cats) | bitwarden 32, ombi, skoruba | ~80 |
| 5 | Profiles with several constructors (`ProfilePost`, `ProfileDasBlogUser`) | dasblog | 35 |
| 6 | Reference cycles (`Server`/`Game`; `Secret`/`Project`) | lancommander 24, bitwarden 14 | 38 |
| 7 | `Condition` in `CreateMap<Client, ClientDto>` and `Map` of `object`/`dynamic` | skoruba | 20 |
| 8 | `AllowNullCollections = true` in a profile | altinnauth | 7 |
| 9 | Members without a source (`TrackingState`, `Password`) in `ProjectTo` maps | neozhu | 10 |
| 10 | Maps in an extension method outside a Profile constructor (`CreateMapsForV1_0`) | gnomeshade | 9 |

Suggested order: kavita nullable `MapFrom` (1, 4: one rule serves five repos), jhipster reverse-map `Condition`/`ForPath` (2),
constructor choice (3, 5), cycles (6).
