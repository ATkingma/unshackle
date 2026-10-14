# TAAK-258 — AutoMapper corpus run: harness and baseline (row 16, preparation)

**Status**: in progress (preparation done; after-fix round open)
**Created**: 2026-10-14

## Goal

Prepare row 16 of `automapper-queue.md`: harness, baseline tests and a report-only scan on the AutoMapper survey corpus,
so the after-fix round can run as soon as the fix migrates more than the occasional repo.

## Done

- [x] Harness `corpus-am.sh` (base/scan/after/list/summary) + watchdog in `<corpus>/corpus-automapper/tools/`
- [x] Baseline: 14 of 18 repos usable (7851 tests); 4 blocked by npm/yarn/cargo build steps
- [x] Scan with `main` 4661eba: 1 Fixed / 1373 Manual; top reasons in `CORPUSRUN-automapper.md`

## Open

- [ ] After-fix round, once the Manual cascade is reduced (`ProjectTo`/options, collection maps, nullable sources)
- [ ] Optional: solution filters for bitwarden, gnomeshade, lancommander, ombi (skip the front-end/Rust project)
- [ ] Fix the `neozhu` solution name in the corpus `repos.txt` (`CleanArchitecture.Blazor.sln`)

## See also

- `claude/tasks/CORPUSRUN-automapper.md`
- `claude/tasks/automapper-queue.md`
