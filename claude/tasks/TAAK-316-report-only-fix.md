# TAAK-316 — Report-only fix kind

**Status**: step 1/4 — not started
**Created**: 2026-10-22
**Goal**: A fix can declare itself report-only: its patterns return only Manual/Info, the Fixer never touches packages for it, and the report header says "inventory only".

## Context for the next agent

- Decided by council round 8, decision S2 (`claude/docs/council.md`, 2026-10-22). Read that rule first; it is the spec.
- Core change: additive only. Do not change existing members of `IFix`, `IPattern` or `IPartialFix`; other agents build new fixes against them in parallel (Moq, Polly, EPPlus skeletons).
- Follows `claude/docs/code-standaarden.md` (class with `required init` for models, never `sealed`, `CancellationToken` required, GoF patterns).

## Steps

- [ ] 1. Design note in this file: types, where the Fixer calls it, how the report shows it
- [ ] 2. Implement in Core (+ DataAccess for file I/O behind a Core interface)
- [ ] 3. Unit tests (a fake report-only fix: no package edit, header text, not counted as Fixed)
- [ ] 4. Docs (`docs/projectinfo/architectuur/core.md` section), build 0 warnings, all tests green

## Progress log

- 2026-10-22: created after council round 8.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core`, tests |
| **How**   | Engine extension |
| **Why**   | Needed for the MassTransit (TAAK-310) and IdentityServer4 (TAAK-381) detectors |

## See also

- `claude/docs/council.md` — round 8
- `claude/tasks/CLAIMS.md` — file claims
