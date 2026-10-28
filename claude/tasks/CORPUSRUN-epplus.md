# Corpus run EPPlus (row 12 of `epplus-queue.md`)

Scan round 1 (L4, 2026-10-26): `unshackle scan --fix epplus --max-degree 4 --report` with the tool from `pool/l4-epplus-corpus-wip` 7a27c9e
(`main` c544d1d merged in) on the 38 repos of `claude/tools/corpus-epplus-list.txt`.

## Harness (Windows)

- `claude/tools/corpus-epplus-scan.py <corpus root> <tool dir> <list> <fix>`: per repo `dotnet build` (restores), then `scan`; logs and
  `report.json` in `<corpus root>/scan/<name>/`, one line per repo in `scan/_results.txt`. NUGET_PACKAGES, NUGET_HTTP_CACHE_PATH, TEMP/TMP and
  DOTNET_CLI_HOME under the corpus root (exe from the user temp folder is blocked). Sources are untrusted clones: only build and the tool run on them.
- `claude/tools/corpus-epplus-summary.py <corpus root>`: the tables below.

## Result

Zero `Fixed`: every repo has at least one Manual finding, and `epplus` is AllOrNothing (E8), so the shapes the patterns do rewrite are reported as
"Not rewritten" (blocked, not wrong). No pattern crashed and no finding was a false Fixed, so there is no after-fix round yet: `fix --force`
would leave every solution unchanged.

| Repo | Build / scan | Fixed | Manual | Crash in log |
| --- | --- | ---: | ---: | --- |
| bookfx | build=0 4s scan=0 25s | 0 | 168 |  |
| excelchop | build=0 4s scan=0 11s | 0 | 16 |  |
| xlobjmapper | build=0 5s scan=0 16s | 0 | 84 |  |
| zserver | build=0 10s scan=0 24s | 0 | 3 |  |
| dexiom | build=1 3s scan=1 12s | 0 | 0 |  |
| dataextractor | build=0 11s scan=0 10s | 0 | 38 |  |
| pivotdata | build=0 4s scan=0 5s | 0 | 0 |  |
| excelcake | build=0 15s scan=0 9s | 0 | 115 |  |
| griddly | build=1 29s scan=0 21s | 0 | 15 |  |
| haohao | build=0 19s scan=0 23s | 0 | 18 |  |
| alizhou | build=0 12s scan=0 6s | 0 | 7 |  |
| dotnettools | build=0 17s scan=0 17s | 0 | 7 |  |
| endurance | build=1 15s scan=0 27s | 0 | 8 |  |
| filecontextcore | build=0 8s scan=0 13s | 0 | 24 |  |
| nopframework | build=0 35s scan=0 31s | 0 | 17 |  |
| qpas | build=1 14s scan=0 29s | 0 | 21 |  |
| closetcode | build=1 0s scan=1 33s | 0 | 0 |  |
| dorisoy | build=1 0s scan=1 5s | 0 | 0 |  |
| geekconfig | build=0 3s scan=0 4s | 0 | 30 |  |
| manga | build=0 13s scan=0 9s | 0 | 9 |  |
| aistory | build=1 0s scan=1 6s | 0 | 0 |  |
| lazyfurs | build=0 8s scan=0 7s | 0 | 30 |  |
| lesi | build=2147516571 0s scan=1 0s | 0 | 0 | yes |
| openmirroring | build=0 8s scan=0 6s | 0 | 7 |  |
| salesapi | build=0 5s scan=0 15s | 0 | 10 |  |
| coreframework | build=0 27s scan=0 27s | 0 | 19 |  |
| tunnetcom | build=0 45s scan=0 184s | 0 | 131 |  |
| zamin | build=1 5s scan=0 14s | 0 | 0 |  |
| newdcms | build=1 20s scan=0 109s | 0 | 62 |  |
| etlast | build=0 24s scan=0 37s | 0 | 55 |  |
| etlnet | build=0 23s scan=0 41s | 0 | 36 |  |
| montr | build=0 29s scan=0 47s | 0 | 45 |  |
| packt8 | build=1 341s scan=0 93s | 0 | 62 |  |
| panda | build=0 29s scan=0 38s | 0 | 17 |  |
| yourbrand | build=0 155s scan=0 142s | 0 | 9 |  |
| simoona | build=0 45s scan=0 82s | 0 | 41 |  |
| officeimo | build=0 733s scan=1 132s | 0 | 0 |  |
| eppsamples | build=0 4s scan=0 11s | 0 | 473 |  |

| Pattern | Status | Count |
| --- | --- | ---: |
| epplus.unsupported | Manual | 492 |
| epplus.styles | Manual | 412 |
| project-file | Manual | 170 |
| epplus.dimension | Manual | 167 |
| epplus.package | Manual | 163 |
| epplus.layout | Manual | 124 |
| epplus.load-from | Manual | 29 |
| epplus.license-context | Manual | 19 |
| usings | Manual | 1 |


### Manual codes that block most (top)

| Pattern | Code | Count | Next step |
| --- | --- | ---: | --- |
| epplus.unsupported | UnmigratedEPPlusApi | 454 | top members: `ExcelWorksheet` as a type (222), `ExcelWorksheet.Cells` (182), `ExcelPackage.Workbook` (145), `Worksheets` (138): worksheets and packages that are not a single local (fields, parameters, helpers); candidate new rows. Drawings (`SetPosition` 30) and `ExcelTable` (21) have no row |
| epplus.package | CellReadDiffers | 115 | opened workbooks (E12): reads stay Manual by design |
| epplus.styles | UnmappedEPPlusStyle | 77 | look at the most frequent members |
| epplus.dimension | UnmappedDimension | 69 | `Dimension` used other than `.End/.Start.Row/Column` |
| epplus.layout | AutoFitDiffers / ColumnWidthDiffers | 66 / 24 | by design (E13, harness finding a) |
| epplus.unsupported | PublicApiExposesOldLibrary | 38 | by design (E6) |
| epplus.package | uncoded messages (several locals, not a local, `p.Workbook` use) | ~30 | give these an `EPPlusReason` code |

### Environment failures (not tool bugs, except the first)

- lesi: global.json asks SDK 8.0.0, not installed. The tool prints an **unhandled** `RemoteInvocationException` (hostfxr_resolve_sdk2) instead
  of a clean "solution could not be loaded" message: a DataAccess loader bug, outside lane L4 (also seen in 4 MassTransit repos).
- dexiom (net40 reference assemblies), closetcode (missing projects, TFM mismatch), dorisoy, aistory (restore errors), officeimo (duplicate
  project paths): the solution does not load; the tool reports that cleanly with exit code 1.

## See also

- `claude/tasks/epplus-queue.md` — row 12
- `claude/tasks/TAAK-309-epplus-to-closedxml.md` — the task
- `docs/projectinfo/architectuur/corpus-epplus.md` — survey

## TAAK-326 re-measure: per-project migration (council round 13, E17), 2026-10-26

Tool `bf48ddc` (`tool-326c`), same 38 repos, `scan` → `<corpus>/scan-326c/`. **17 of 30 loadable repos now migrate at least one project** (round 1: 0).
"Fixed" counts rows; most are project-file swaps in projects without blocking rows. Projects kept = `epplus.kept` rows.

After-fix round (`fix --force` + `dotnet build`, sources reset after): montr (23 files), manga (3), simoona (12) build green.
salesapi broke with the first tool (`tool-326b`): Domain declared EPPlus without EPPlus code, and the kept Application project got EPPlus only through it,
while Infrastructure used Microsoft.Extensions.Configuration that EPPlus 7 brought in transitively (16 errors). Fixed in `bf48ddc`: a project a kept project
references keeps EPPlus too, so salesapi now stays whole. The transitive-dependency loss itself is general: TAAK-329.

| Repo | Build / scan | Fixed | Manual | Fixed % | Projects kept (TAAK-326) | Crash in log |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| bookfx | build=0 3s scan=0 22s | 2 | 160 | 1% | 2 |  |
| excelchop | build=0 2s scan=0 6s | 0 | 17 | 0% | 0 |  |
| xlobjmapper | build=0 2s scan=0 9s | 0 | 85 | 0% | 0 |  |
| zserver | build=0 7s scan=0 21s | 0 | 4 | 0% | 0 |  |
| dexiom | build=1 1s scan=1 9s | 0 | 0 | - | 0 |  |
| dataextractor | build=0 3s scan=0 7s | 0 | 39 | 0% | 0 |  |
| pivotdata | build=0 1s scan=0 4s | 0 | 0 | - | 0 |  |
| excelcake | build=0 1s scan=0 7s | 1 | 57 | 2% | 2 |  |
| griddly | build=1 3s scan=0 20s | 0 | 5 | 0% | 1 |  |
| haohao | build=0 3s scan=0 17s | 2 | 5 | 29% | 2 |  |
| alizhou | build=0 1s scan=0 3s | 1 | 5 | 17% | 1 |  |
| dotnettools | build=0 3s scan=0 12s | 0 | 8 | 0% | 0 |  |
| endurance | build=1 2s scan=0 18s | 1 | 4 | 20% | 1 |  |
| filecontextcore | build=0 1s scan=0 12s | 3 | 20 | 13% | 1 |  |
| nopframework | build=0 2s scan=0 26s | 0 | 18 | 0% | 0 |  |
| qpas | build=1 2s scan=0 26s | 1 | 15 | 6% | 1 |  |
| closetcode | build=1 0s scan=1 32s | 0 | 0 | - | 0 |  |
| dorisoy | build=1 0s scan=1 4s | 0 | 0 | - | 0 |  |
| geekconfig | build=0 1s scan=0 4s | 0 | 31 | 0% | 0 |  |
| manga | build=0 4s scan=0 12s | 3 | 4 | 43% | 1 |  |
| aistory | build=1 0s scan=1 5s | 0 | 0 | - | 0 |  |
| lazyfurs | build=0 1s scan=0 6s | 0 | 31 | 0% | 0 |  |
| lesi | build=2147516571 0s scan=1 0s | 0 | 0 | - | 0 |  |
| openmirroring | build=0 1s scan=0 4s | 0 | 8 | 0% | 0 |  |
| salesapi | build=0 2s scan=0 15s | 0 | 11 | 0% | 0 |  |
| coreframework | build=0 6s scan=0 25s | 0 | 20 | 0% | 0 |  |
| tunnetcom | build=0 5s scan=0 135s | 3 | 26 | 10% | 1 |  |
| zamin | build=1 1s scan=0 8s | 0 | 0 | - | 0 |  |
| newdcms | build=1 1s scan=0 108s | 4 | 55 | 7% | 1 |  |
| etlast | build=0 3s scan=0 26s | 3 | 43 | 7% | 2 |  |
| etlnet | build=0 4s scan=0 30s | 1 | 33 | 3% | 2 |  |
| montr | build=0 9s scan=0 31s | 22 | 13 | 63% | 1 |  |
| packt8 | build=1 4s scan=0 81s | 2 | 30 | 6% | 10 |  |
| panda | build=0 10s scan=0 43s | 1 | 12 | 8% | 1 |  |
| yourbrand | build=0 35s scan=0 147s | 3 | 5 | 38% | 1 |  |
| simoona | build=0 41s scan=0 131s | 11 | 25 | 31% | 1 |  |
| officeimo | build=0 257s scan=1 146s | 0 | 0 | - | 0 |  |
| eppsamples | build=0 1s scan=0 11s | 0 | 474 | 0% | 0 |  |

| Pattern | Status | Count |
| --- | --- | ---: |
| epplus.unsupported | Manual | 492 |
| epplus.styles | Manual | 233 |
| epplus.package | Manual | 163 |
| epplus.dimension | Manual | 121 |
| epplus.layout | Manual | 101 |
| project-file | Manual | 78 |
| project-file | Fixed | 64 |
| epplus.kept | Manual | 32 |
| epplus.load-from | Manual | 28 |
| epplus.license-context | Manual | 13 |
| usings | Manual | 2 |


## TAAK-327 step 7 re-measure (Linux), 2026-10-28

Tool built from `epplus-corpus-wip` 024b12e (`main` 74cdc50 merged in), `claude/tools/corpus-epplus-run.sh base|after` (corpus-am.sh:
before = restore, build, test x2; after = `fix --fix epplus --force`, clean, restore, build, test, per-test compare). Own NuGet cache, TMPDIR on
the corpus disk. The harness has no cell compare, so only build and per-test results are compared. The machine was heavily loaded (load 70-90 from
other runs): several builds hit the 1800 s step timeout and are marked **not finished**, not failed. A session restart stopped the first after run;
the last 8 repos were rerun with `-m:2` and a 60 min time-box per repo.

**Result: no regression and no tool bug found.** Every repo whose build fails after the fix fails the same way before it (same error codes in
`build0` and `build1`); every repo with tests has the same pass/fail per test before and after. No new tasks (TAAK-364..367 left unused).

| Repo | After status | Fixed | Manual | Tests before → after (pass/fail) | Note |
| --- | --- | ---: | ---: | --- | --- |
| bookfx | ok | 2 | 164 | 547/1 → 547/1 | |
| coreframework | ok | 0 | 19 | 22/7 → 22/7 | |
| dataextractor | ok | 0 | 37 | 31/0 → 31/0 | |
| excelcake | ok | 1 | 57 | 1/0 → 1/0 | |
| excelchop | ok | 0 | 16 | 4/0 → 4/0 | |
| pivotdata | ok | 0 | 0 | 53/0 → 53/0 | |
| salesapi | ok | 0 | 10 | 21/0 → 21/0 | |
| tunnetcom | ok | 3 | 26 | 705/8 → 705/8 | |
| alizhou, dotnettools, filecontextcore, haohao, lazyfurs, lesi, openmirroring, xlobjmapper, eppsamples | ok | 1/0/3/2/0/0/0/0/0 | | no tests | lesi now loads (SDK issue of round 1 gone) |
| endurance, griddly, manga, nopframework, zserver | build fails | 1/3/3/0/0 | | - | same errors before the fix (CS0619 MudBlazor, MSB4019, CA2017, rzc, MSB3021) |
| aistory, closetcode, dorisoy, etlast, geekconfig, newdcms, qpas, zamin, simoona | restore fails | - | - | - | environment: workloads, Windows TFM, website project, NU1605/NU1008; same in before run |
| dexiom | fix exits 1 | - | - | - | net40 reference assemblies missing: clean "could not be loaded" |
| montr | not finished | 22 | 13 | - | build after fix hit the 1800 s timeout (built green in the TAAK-326 round) |
| etlnet, panda, packt8, yourbrand, officeimo | not finished | | | - | step timeout under load |

Results: `<corpus>/_results/{base,after}/<name>/` (`compare.json`, `steps.txt`, logs).
