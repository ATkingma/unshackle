# TAAK-320 — Project blockers also stop document rewrites; report-only summary in the console

**Status**: step 3/3 — done (engine-2)
**Created**: 2026-10-22
**Goal**: A project-level blocking row (`ProjectBlockingReason`) keeps that project's documents unchanged (all its rewrites become Manual with a pointer to the blocker), and the console summary says "Inventory only" for report-only fixes.

## Context for the next agent

- Found by the Polly agent (P9): `ProjectBlockers` only stops file edits (G4). C# rewrites and the package swap ran before, so a blocked project (e.g. `TargetFrameworkUnsupported`) would get v8 code while keeping Polly v7: it no longer builds. Polly works around it with a per-rewrite Manual; this task makes the engine do it once for every fix.
- This follows the round-8 general rule (Fixed only when proven; package removal per project only at 0 Manual) and G4 (council round 10). It only makes output more conservative.
- Interface follow-ups from TAAK-316: `SpectreConsoleView.ShowSummary` prints "Inventory only: nothing was changed." for a report-only run; dry-run shows "deleted" for `ProjectFileChange.Deletes`.

## Steps

- [x] 1. Fixer: evaluate `ProjectBlockers` before the document pass; documents of a blocked project are not rewritten, their findings become Manual "blocked by <code> on <project>"
- [x] 2. Console summary + dry-run delete display (Interface, not `Cli.cs`)
- [x] 3. Tests (fake fix), existing output byte-identical for fixes without blockers, `core.md`; build 0 warnings, all tests green

## Progress log

- 2026-10-22: created after Polly P9.
- 2026-10-22: done. Blocker rows are evaluated before the document pass; the documents of a blocked project are restored and their Fixed rows become Manual "blocked by <code> on <project>". Console shows "Inventory only" and one "(deleted)" line per deleted file in a dry run. 2006 tests green.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core` (Fixer/FileFixer), `src/Unshackle.Interface` views |
| **How**   | Engine + view |
| **Why**   | A blocked project must not be half-migrated |

## See also

- `claude/docs/council.md` — rounds 8 and 10 (G4)
- `claude/tasks/TAAK-318-engine-round-10.md`
