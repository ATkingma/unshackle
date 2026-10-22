# TAAK-306 — Manual reduction round 2: inventory MediatR and AutoMapper blockers

**Status**: step 1/3 — not started  
**Created**: 2026-10-22  
**Goal**: A ranked list of the Manual reasons that block whole MediatR/AutoMapper repos in the corpus, with a task per top blocker.

## Context for the next agent

- Blind rounds showed FA migrates almost everywhere, while many MediatR/AutoMapper repos stay mostly Manual (all-or-nothing per solution, plus root blockers in user code). TAAK-271..278 reduced this; the current share is not re-measured.
- Data is already there: the 36 reports of the TAAK-298 run in `<corpus>/corpus-blind/_results/after-298/*/report.json` (and the AutoMapper/MediatR corpus reports).
- Rule: only reduce Manual with proof (proofs + 0 corpus regressions); a reason that is justified stays.

## Steps

- [ ] 1. Count, per library, the Manual rows by `KeptReason`/pattern across the corpus reports; which reasons block a whole repo
- [ ] 2. Rank by repos unblocked; mark each reason justified or fixable
- [ ] 3. A task per fixable top blocker (taak-start), design choices to the council

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | analysis; later Core patterns |
| **How**   | Analysis |
| **Why**   | MediatR/AutoMapper users get little changed on unseen code |

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from
