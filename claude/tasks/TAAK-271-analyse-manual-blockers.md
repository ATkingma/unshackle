# TAAK-271 — Analyse every Manual finding that blocks a migration

**Status**: step 1/4 — not started
**Created**: 2026-10-17
**Goal**: a complete, counted list of every Manual finding in the 8 blocked blind-corpus repos and SampleShop, each classified as _solvable with proof_ (and by which task) or _principally manual_ (with the reason).

## Context for the next agent

- Queue and proof rules: `claude/tasks/manual-reduction-queue.md`.
- Blocked repos (`claude/tasks/CORPUSRUN-blind.md`): MediatR fullcart, miru, starter; AutoMapper culinary, msb, fiapgames, restapi, baseapi. Clones/harness: see that file (corpus on the large data disk, `<corpus>`; never `/tmp`).
- SampleShop FA scan: 19 Manual, 10 of them from the custom assertion class `OrderAssertions`.
- The all-or-nothing rule (MediatR/AutoMapper per solution) lives in `src/Unshackle.Core/Fixer.cs`, `Abstractions/IFix.cs`, `Fixes/MediatR/MediatRFix.cs`, `Fixes/AutoMapper/AutoMapperFix.cs`.

## Approach

1. Run `scan --report <corpus>/_results/271/<repo>.json` with current `main` on the 8 repos and SampleShop (all three fixes on SampleShop).
2. Group every Manual finding by root cause (not by symptom): count, example `file:line`, and how many findings it blocks through all-or-nothing.
3. Classify each cause: **solvable** → which task (272–275) and the proof idea; **principally manual** → why no equivalent code exists (measured with a probe program when unsure).
4. Write the table into this task and update the "Depends on"/scope of TAAK-272..275 if the analysis changes them.

## Steps

- [ ] 1. Scans on 8 repos + SampleShop with `main`
- [ ] 2. Root-cause table with counts and blocked-findings
- [ ] 3. Classification (solvable → task / principally manual → reason)
- [ ] 4. Update TAAK-272..275 scope; commit

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`
- `claude/tasks/CORPUSRUN-blind.md`
