# TAAK-317 — Project-file migrations (IProjectFileMigration)

**Status**: step 1/4 — not started
**Created**: 2026-10-22
**Goal**: Fixes can declare exact, typed project/config-file edits (package rename/remove/add, one MSBuild property, known config file rename/transform), applied by the Fixer after the document pass and only when the project's C# part has 0 blocking Manual rows.

## Context for the next agent

- Decided by council round 8, decision S3 (`claude/docs/council.md`, 2026-10-22). Read that rule first; it is the spec.
- Core change: additive only. Do not change existing members of `IFix`, `IPattern` or `IPartialFix`; other agents build new fixes against them in parallel (Moq, Polly, EPPlus skeletons).
- Follows `claude/docs/code-standaarden.md` (class with `required init` for models, never `sealed`, `CancellationToken` required, GoF patterns).

## Steps

- [ ] 1. Design note in this file: types, where the Fixer calls it, how the report shows it
- [ ] 2. Implement in Core (+ DataAccess for file I/O behind a Core interface)
- [ ] 3. Unit tests with a before/after snapshot proof per edit kind; OutputType/IsTestProject edits warn
- [ ] 4. Docs (`docs/projectinfo/architectuur/core.md` section), build 0 warnings, all tests green

## Progress log

- 2026-10-22: created after council round 8.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core`, tests |
| **How**   | Engine extension |
| **Why**   | Needed for SpecFlow → Reqnroll and for CPM pins (Polly P6); patterns never write files |

## See also

- `claude/docs/council.md` — round 8
- `claude/tasks/CLAIMS.md` — file claims
