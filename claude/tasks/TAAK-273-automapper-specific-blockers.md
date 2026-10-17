# TAAK-273 — AutoMapper specific blockers: cycles, unflattening, null sources, private setters

**Status**: step 1/5 — not started (waits for TAAK-271, ideally after TAAK-272)
**Created**: 2026-10-17
**Goal**: the AutoMapper causes found in the blind corpus become Fixed wherever AutoMapper's behaviour can be reproduced exactly, each with probe + proofs; the rest gets a precise "principally manual" reason.

## Context for the next agent

- Causes from `claude/tasks/CORPUSRUN-blind.md`: culinary (cycle, null sources), msb (cycle, unflattening), fiapgames (`UsuarioModel.Nome { get; private set; }`), restapi/baseapi (see TAAK-271 table).
- Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Approach (per cause, measure first)

- **Cycles**: what does AutoMapper 14 do on a self-referencing map without `PreserveReferences`/`MaxDepth` (stack overflow, depth limit, reference detection)? If the data cannot be cyclic (tree) a recursive `ToB()` is equivalent; otherwise reproduce the measured behaviour or Manual.
- **Unflattening** on `ReverseMap`: reproduce AutoMapper's member creation for `CustomerName` → `Customer.Name` (creates intermediate objects?) exactly, else Manual.
- **Null sources**: extend the TAAK-263 null-safe chains where AutoMapper's null substitution is provable.
- **Private setters**: AutoMapper sets them via reflection. Options: matching constructor (TAAK-266 rules), or generated code is impossible → principally manual (state so). Measure.

## Steps

- [ ] 1. KingmaDoc plan per touched pattern; read TAAK-271 for counts
- [ ] 2. Probe programs per cause (AutoMapper 14.0.0)
- [ ] 3. Implementation + proofs + unit tests per cause
- [ ] 4. Corpus scan/after on culinary, msb, fiapgames, restapi, baseapi: 0 regressions
- [ ] 5. Verify, models, commit

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/TAAK-272-automapper-partial-migration.md`
