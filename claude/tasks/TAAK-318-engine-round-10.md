# TAAK-318 — Engine follow-ups from council round 10

**Status**: step 5/5 — done; Cli.cs line and C1 moves listed for the main session
**Created**: 2026-10-22
**Goal**: The engine supports typed Manual reason codes, fix-level Info rows, a clean first line after using removal, `IsPackable`, and the round-10 project-file rules, with all existing output byte-identical.

## Context for the next agent

- Spec: `claude/docs/council.md`, round 10 (C1, C2, C3, G1, G2, G3, G4, G5, R1). Engine code from TAAK-316/317 is on main (`core.md` sections).
- Additive where possible; existing fixes' rendered output must stay byte-identical (tests pin it). Moq, Polly and EPPlus agents are working in their own `Fixes/` folders in parallel: do not touch those.

## Steps

- [x] 1. C1: reason-code field on `ManualResult` (separate from `Kept`), rendered text unchanged; migrate the Polly/EPPlus/MediatR text prefixes only via a list for those agents (do not edit their folders)
- [x] 2. C2: optional fix-level Info hook; C3: blank first line after removed first-line using
- [x] 3. G1: distinct header kind excluded from counts (+ test); G2/G3/G4/G5 rules in the project-file step
- [x] 4. R1: `IsPackable` per project from DataAccess (SDK default when unset)
- [x] 5. Build 0 warnings, all tests + SampleShop green, `core.md` updated

## Progress log

- 2026-10-22: created after council round 10.
- 2026-10-22: C1, C2, C3, G1–G5, R1 and P8 implemented (engine-2); 1836 tests and 38 SampleShop tests green. C3 changes five pinned outputs (FixerTests, AutoMapper empty-factory-class, three EPPlus LicenseContext proofs): only the leading blank line is removed.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core` root + Abstractions + Models, `src/Unshackle.DataAccess` |
| **How**   | Engine extension |
| **Why**   | Three fixes were working around missing reason codes with text prefixes |

## See also

- `claude/docs/council.md` — round 10
- `claude/tasks/TAAK-316-report-only-fix.md`, `claude/tasks/TAAK-317-project-file-migrations.md`
