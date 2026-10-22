# TAAK-302 — verify-head checks pattern docs without jq

**Status**: step 4/4 — steps 1–3 done (commit 3691027), push by the main session  
**Created**: 2026-10-22  
**Goal**: `claude/tools/verify-head.sh` never reports VERIFY OK when the pattern-docs check did not run.

## Context for the next agent

- On 2026-10-22 `verify-head.sh` printed `jq: command not found` from `.claude/hooks/check-pattern-docs.sh` and still ended with `VERIFY OK`: the check is silently skipped on machines without `jq` (Git Bash on Windows).
- Options: fail when `jq` is missing, or port the check to Python (`python3 -I`, already used by the corpus tools) so it runs everywhere. Prefer the port; keep the hook's behaviour identical.

## Steps

- [x] 1. Read `.claude/hooks/check-pattern-docs.sh` and list what it checks
- [x] 2. Make the check run without `jq` (Python port or a jq-free path); a missing tool fails the run
- [x] 3. Prove it: a fake pattern without docs makes verify fail; the clean HEAD passes
- [ ] 4. Push, CI green

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `claude/tools/verify-head.sh`, `.claude/hooks/check-pattern-docs.sh`, `claude/tools/check_pattern_docs.py` (new) |
| **How**   | Tooling fix |
| **Why**   | A guard that silently does not run gives false confidence |

## Progress log

- 2026-10-22 (tooling agent): step 1 — the hook checks, per `Fixes/*/Patterns/*Pattern.cs` with an `Id => "..."`:
  `<slug>-plan.md`, `<slug>-modellen.md`, `<slug>-verify.md` in `docs/projectinfo/patterns/` and `patterns/<slug>-plan.md`
  in `docs/projectinfo/README.md`; modes post-edit (context hint), stop (warn), subagent-stop (block), skips when
  `stop_hook_active`. Step 2 — logic ported to `claude/tools/check_pattern_docs.py` (same messages/JSON); the hook is a
  wrapper that probes `python3`/`python -I` (python3 is a Store stub here) and exits 2 when none runs; new mode `check`
  exits 1 on missing docs. `verify-head.sh` now uses the exit code of `check` instead of grepping output. Step 3 — fake
  `Zz.Fake` pattern: `check` rc=1 with 4 lines, subagent-stop blocks; no python on PATH: rc=2 (verify fails);
  `stop_hook_active` rc=0 silent; `verify-head.sh` on clean HEAD: VERIFY OK (0 warnings, 1673 + 38 tests green).

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from
