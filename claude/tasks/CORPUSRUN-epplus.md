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
