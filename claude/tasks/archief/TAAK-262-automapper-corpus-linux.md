# TAAK-262 — AutoMapper corpus on Linux: clone, harness, rescan

**Status**: done
**Created**: 2026-10-14

## Goal

The AutoMapper corpus lived on a Windows machine outside the repo. Rebuild it on Linux under
`/mnt/dikkeschijf/unshackle-corpus/corpus-automapper/` (never `/tmp`: tmpfs in RAM), put the harness in the repo and
rescan with the tool from `main`.

## Why

TAAK-258 prepared the harness and a scan, but only on the Windows machine; Linux has 24 cores and 31 GB RAM, so the
after-fix round can run here. The rescan shows what TAAK-259..261 changed.

## What

- `claude/tools/corpus-am.sh` (modes `clone`, `base|scan|after`, `list`, `summary`), reusing `claude/tools/corpus-regression.py`.
- `claude/tools/corpus-am-repos.txt`: 18 repos with pinned refs (abp and iotsharp skipped); `corpus-am-list.txt`: name + solution.
- Clones are shallow at the pinned commit; an existing `src/<name>` is skipped. Clones are untrusted: only restore/build/test and the tool.
- Tool published from main to `<corpus>/_results/tool-<sha>`; only `scan` (report-only) was run. Results: `CORPUSRUN-automapper.md`, section "Rescan on Linux".

## Done

- [x] Clone 18 repos (kavita, jhipster were already present from TAAK-263/264)
- [x] Harness ported and committed
- [x] Rescan with main, results in `CORPUSRUN-automapper.md`
- [x] `overdracht.md` updated

## See also

- `claude/tasks/CORPUSRUN-automapper.md`
- `claude/tasks/archief/TAAK-258-automapper-corpus-baseline.md`
