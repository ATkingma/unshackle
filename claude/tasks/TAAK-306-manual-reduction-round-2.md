# TAAK-306 — Manual reduction round 2: inventory MediatR and AutoMapper blockers

**Status**: step 3/3 — steps 1–2 done (`ANALYSE-manual-round-2.md`), step-3 tasks TAAK-311..315 drafted, council pending  
**Created**: 2026-10-22  
**Goal**: A ranked list of the Manual reasons that block whole MediatR/AutoMapper repos in the corpus, with a task per top blocker.

## Context for the next agent

- Blind rounds showed FA migrates almost everywhere, while many MediatR/AutoMapper repos stay mostly Manual (all-or-nothing per solution, plus root blockers in user code). TAAK-271..278 reduced this; the current share is not re-measured.
- Data is already there: the 36 reports of the TAAK-298 run in `<corpus>/corpus-blind/_results/after-298/*/report.json` (and the AutoMapper/MediatR corpus reports).
- Rule: only reduce Manual with proof (proofs + 0 corpus regressions); a reason that is justified stays.

## Steps

- [x] 1. Count, per library, the Manual rows by `KeptReason`/pattern across the corpus reports; which reasons block a whole repo
- [x] 2. Rank by repos unblocked; mark each reason justified or fixable
- [ ] 3. A task per fixable top blocker (taak-start), design choices to the council

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | analysis; later Core patterns |
| **How**   | Analysis |
| **Why**   | MediatR/AutoMapper users get little changed on unseen code |

## Progress log

- 2026-10-22 (analysis agent): steps 1–2 in `claude/tasks/ANALYSE-manual-round-2.md`; tasks TAAK-311..315 created.
  The mediatr/automapper corpus reports are stale (before TAAK-271), so re-measure before ranks 3–5.

## See also

- `claude/tasks/ANALYSE-manual-round-2.md` — inventory and ranking

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from
