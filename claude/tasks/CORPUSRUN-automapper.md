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
| neozhu | ok | 42 (13/29) | 0 | solution is `CleanArchitecture.Blazor.sln` (fixed in `claude/tools/corpus-am-list.txt`) |
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

- `claude/tasks/archief/TAAK-258-automapper-corpus-baseline.md`
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

## TAAK-266: several constructors, ConstructUsing, MaxDepth (dasblog, letterbook)

Scan with `<corpus>/_results/266/<name>-voor|na.json` (voor = main 9a919ba, na = TAAK-266). Both still 0 Fixed / 54 and 156 Manual (all-or-nothing); the reasons moved:

| Repo | Blocker before | Blocker now (real cause) | Next |
| --- | --- | --- | --- |
| dasblog | `ProfilePost`/`ProfileDasBlogUser` "have several constructors" (35) | the maps sit in the constructor that takes `IDasBlogSettings` / `ISiteSecurityManager` and read that service (a static `ToB` cannot) | `MapFrom` of `WebFinger.Aliases` null (5), `CategoryCacheEntryCollection` copy (3) |
| letterbook | `Mention` several constructors (42), `ConstructUsing`/`MaxDepth` (~39) | `Mention` is created through its private parameterless constructor (42); `ConstructUsing` reads the profile parameter `opts`/`options` (27+6) | `ConvertUsing<DateTimeOffsetMapper>` over a built-in conversion (11), `ConstructUsing` + `Condition` (6), maps outside a profile constructor (`AstMapper`, 8), `IncludeBase`/`AfterMap` |

Measured and implemented: AutoMapper takes the constructor with the most resolvable parameters (over a parameterless one, any accessibility, first declared on a tie); `ConstructUsing` makes the object and the members are then mapped on top; `MaxDepth(n>=2)` is a no-op without cycles, `MaxDepth(1)` cuts a collection member off. See `claude/tasks/archief/TAAK-266-automapper-several-constructors.md`.

## After-fix round with main 756e2ec (TAAK-258, partial)

Linux, tool `<corpus>/corpus-automapper/_results/tool-756e2ec/`, per repo `corpus-am.sh one base` then `one after` (3 repos at once;
the Windows baseline results were not on this machine, so base was rerun). Stopped early at the user's request: 6 of 14 repos done.

| Repo | base | after build | Fixed / Manual | tests before → after (pass/total) | regressions |
| --- | --- | --- | --- | --- | ---: |
| altinnauth | ok | ok | 0 / 14 | 534/534 → 534/534 | 0 |
| cats | **build fails** (RZ9985, front-end; as in the scan) | fails the same way | 0 / 125 | – | – (nothing changed) |
| dasblog | ok | ok | 0 / 54 | 51/70 → 51/70 | 0 |
| devarch | ok | ok | 0 / 4 | 37/37 → 37/37 | 0 |
| eshoponweb | ok | ok | **1 / 0** | 74/74 → 69/74 | **5** |
| jason | ok | ok | 0 / 13 | 16/41 → 16/41 | 0 |
| kavita, jhipster, letterbook, neozhu, nop, personalasst, skoruba, xipona | not run in this round | | | | stopped early (kavita base done, after interrupted) |
| bitwarden, gnomeshade, lancommander, ombi | not run in this round | | | | front-end/Rust build step; no solution filters made |

Regressions: eshoponweb, 5 PublicApi integration tests (HTTP 500 / 100 failed parallel calls). Cause: the endpoints call
`items.Select(_mapper.Map<CatalogItemDto>)` (a method group); the fix does not see it as a map call, removes all three
`CreateMap`s and AutoMapper throws "missing type map" at runtime. Build still succeeds. Follow-up **TAAK-267** (proof in
`Proofs/_Pending/AutoMapper/MapCall/method-group.*`).

Fixed/Manual: the six repos match the scan counts (1 Fixed / 210 Manual over these six; baseline scan 1 / 1373 over 18). No new
Manual reasons; top reasons are unchanged from the TAAK-262 rescan above (all-or-nothing cascade). The one Fixed finding is the regression.

Resume: `CORPUS=<corpus>/corpus-automapper TOOL=.../tool-756e2ec/unshackle.dll bash claude/tools/corpus-am.sh one base|after <name> <sln>`
for the repos marked "not run".

## After-fix round with main d496488 (TAAK-258 complete, 2026-10-15)

Windows, tool `<corpus>/corpus-automapper/_results/tool-d496488/` (TAAK-267 included), base results from the Windows
baseline (2026-10-14). All 14 repos with a usable baseline; bitwarden, gnomeshade, lancommander and ombi have no baseline
(front-end/Rust build step) and are left out. **Result: 0 regressions on all 14.**

| Repo | Fixed / Manual | tests before → after (pass/total) | regressions | flaky / missing (as in the baseline runs) |
| --- | --- | --- | ---: | --- |
| altinnauth | 0 / 14 | 249/534 → 249/534 | 0 | – |
| cats | 0 / 137 | 309/309 → 309/309 | 0 | – |
| dasblog | 0 / 54 | 51/70 → 51/70 | 0 | – |
| devarch | 0 / 4 | 37/37 → 37/37 | 0 | – |
| eshoponweb | **4 / 0** | 74/74 → 74/74 | 0 | – (the TAAK-267 regression is gone) |
| jason | 0 / 13 | 39/41 → 39/41 | 0 | – |
| jhipster | 0 / 126 | 146/146 → 146/146 | 0 | – |
| kavita | 0 / 169 | 3305/3326 → 3305/3326 | 0 | 28 / 14 |
| letterbook | 0 / 156 | 537/616 → 537/616 | 0 | 130 / 64 |
| neozhu | 0 / 58 | 13/42 → 13/42 | 0 | – |
| nop | 0 / 5 | 1051/1060 → 1051/1060 | 0 | 1 / 0 |
| personalasst | 0 / 52 | 131/131 → 131/131 | 0 | – |
| skoruba | 0 / 120 | 272/272 → 272/272 | 0 | – |
| xipona | 0 / 47 | 969/1193 → 969/1193 | 0 | 32 / 16 |

Flaky/missing only occur in repos where the fix changed nothing (0 Fixed): tests that differ between the two baseline runs
or carry generated names. The low Fixed count is the all-or-nothing rule per solution; the remaining root blockers are
user-code shapes a mapping method cannot reproduce safely, e.g. kavita (two profiles map `Series` → `SeriesDto`, the order
decides), letterbook (`Mention` only has a private parameterless constructor), lancommander (`Server` ↔ `Game` cycle).

Harness fixes in this round (`claude/tools/corpus-am.sh`): `after` builds before the fix (letterbook needs its source
generator output to resolve types) and keeps `node_modules/**/bin` when cleaning `bin`/`obj` (jhipster `ng build` broke).

## After-fix round with main 7c0db29 (TAAK-332, 2026-10-27)

Windows, `IProjectProperties` wired into `AutoMapperFix` (TAAK-324 follow-up). Harness copy with its own result dir
(`<work>/cam332/res/after`) and work root, so `_results/after` from TAAK-258 is kept; base from `_results/base`.
`NUGET_PACKAGES`/`NUGET_HTTP_CACHE_PATH` under `<corpus>/corpus-automapper/`. JOBS=2 ran the data disk full (restore and
pdb writes failed: "not enough space on the disk"), so the failed repos were rerun with JOBS=1; cats and kavita still
could not restore for lack of disk and are not measured.

| Repo | Fixed / Manual | tests before → after (pass/total) | regressions | flaky / missing |
| --- | --- | --- | ---: | --- |
| altinnauth | 0 / 14 | 249/534 → 249/534 | 0 | – |
| devarch | 0 / 4 | 37/37 → 37/37 | 0 | – |
| eshoponweb | 7 / 0 | 74/74 → 74/74 | 0 | – |
| jason | 0 / 13 | 39/41 → 39/41 | 0 | – |
| jhipster | 0 / 126 | 146/146 → 146/146 | 0 | – |
| letterbook | 0 / 165 | 537/616 → 537/616 | 0 | 130 / 64 |
| neozhu | 0 / 64 | 13/42 → 13/42 | 0 | – |
| nop | 0 / 5 | 1051/1060 → 1051/1060 | 0 | 1 / 0 |
| personalasst | 0 / 52 | 131/131 → 131/131 | 0 | – |
| skoruba | 0 / 120 | 272/272 → 272/272 | 0 | – |
| xipona | 0 / 47 | 969/1193 → 969/1193 | 0 | 32 / 16 |
| cats, kavita | not measured (disk full) | | | |

**0 regressions on 12 repos; Manual→Fixed 0.** The generic-map rows now name the real blocker: letterbook and personalasst
have the generic member in a class library without `IsPackable`, which the SDK treats as packable (R1). skoruba is blocked
by a generic profile (`CreateMap<TRole, TRole>`), xipona by a closed type not visible from the test project, jhipster and
nop by `Map<T>(object)`. Details in `docs/projectinfo/patterns/automapper-generic-map-verify.md` and
`claude/tasks/TAAK-332-automapper-remeasure.md`.
