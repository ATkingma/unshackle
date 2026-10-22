# TAAK-301 — Document the run options

**Status**: step 3/3 — done in worktree, awaiting push by main session  
**Created**: 2026-10-22  
**Goal**: `--sequential` and `--max-degree N` (TAAK-298) are in the user docs (README usage, `docs/` CLI reference) with when to use them; `--check-every-file` stays hidden.

## Context for the next agent

- TAAK-298 added `--sequential`, `--max-degree N` (default min(ProcessorCount, 8)) and hidden `--check-every-file` (`src/Unshackle.Interface/Cli.cs`). Today they are only described in `claude/docs/council.md` (round 7) and the archived task.
- Output is identical either way; `--max-degree` is the knob for memory (akka peak 1.29x at 8).
- Must be done before release 0.3.0 (TAAK-304).

## Steps

- [x] 1. Find every place the CLI options are listed (README, `docs/`, `list --markdown` output, NuGet readme)
- [x] 2. Add `--sequential` and `--max-degree` with one line each: what, default, when (memory, comparing runs)
- [ ] 3. Build, tests, `verify-head.sh`, push, CI green

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | README / docs |
| **How**   | Docs only |
| **Why**   | Users cannot find options that exist |

## Progress log

- 2026-10-22: Options are listed only in `docs/usage.md` §4; README (also the NuGet readme) links to it; `list --markdown` covers patterns, not options. Added `--max-degree` and `--sequential` rows + an "identical output" note to `docs/usage.md`, one out-of-memory hint in `README.md`. `--check-every-file` stays undocumented. Build 0 warnings, 1673 tests green. Push/CI left to main session.

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from
