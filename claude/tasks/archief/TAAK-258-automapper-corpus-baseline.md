# TAAK-258 — AutoMapper corpus run: harness and baseline (row 16, preparation)

**Status**: done (2026-10-15: after-fix round on all 14 usable repos, 0 regressions)
**Created**: 2026-10-14

## Goal

Prepare row 16 of `automapper-queue.md`: harness, baseline tests and a report-only scan on the AutoMapper survey corpus,
so the after-fix round can run as soon as the fix migrates more than the occasional repo.

## Done

- [x] Harness `corpus-am.sh` (base/scan/after/list/summary) + watchdog in `<corpus>/corpus-automapper/tools/`
- [x] Baseline: 14 of 18 repos usable (7851 tests); 4 blocked by npm/yarn/cargo build steps
- [x] Scan with `main` 4661eba: 1 Fixed / 1373 Manual; top reasons in `CORPUSRUN-automapper.md`

## Open

- [x] After-fix round with main 756e2ec on 6 of 14 repos (altinnauth, cats, dasblog, devarch, eshoponweb, jason): one regression source (eshoponweb, TAAK-267)
- [x] After-fix round on all 14 usable repos with main d496488 (TAAK-267 included): 0 regressions, see `CORPUSRUN-automapper.md`
- [x] Harness: `after` builds before the fix (source generators, letterbook) and keeps `node_modules/**/bin` when cleaning (jhipster `ng build`)
- [ ] Optional, not needed for row 16: solution filters for bitwarden, gnomeshade, lancommander, ombi (skip the front-end/Rust project)
- [x] `neozhu` solution name: `CleanArchitecture.Blazor.sln` in `claude/tools/corpus-am-list.txt`

## See also

- `claude/tasks/CORPUSRUN-automapper.md`
- `claude/tasks/automapper-queue.md`
