# TAAK-302 — verify-head checks pattern docs without jq

**Status**: step 1/4 — not started  
**Created**: 2026-10-22  
**Goal**: `claude/tools/verify-head.sh` never reports VERIFY OK when the pattern-docs check did not run.

## Context for the next agent

- On 2026-10-22 `verify-head.sh` printed `jq: command not found` from `.claude/hooks/check-pattern-docs.sh` and still ended with `VERIFY OK`: the check is silently skipped on machines without `jq` (Git Bash on Windows).
- Options: fail when `jq` is missing, or port the check to Python (`python3 -I`, already used by the corpus tools) so it runs everywhere. Prefer the port; keep the hook's behaviour identical.

## Steps

- [ ] 1. Read `.claude/hooks/check-pattern-docs.sh` and list what it checks
- [ ] 2. Make the check run without `jq` (Python port or a jq-free path); a missing tool fails the run
- [ ] 3. Prove it: a fake pattern without docs makes verify fail; the clean HEAD passes
- [ ] 4. Push, CI green

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `claude/tools/verify-head.sh`, `.claude/hooks/check-pattern-docs.sh` |
| **How**   | Tooling fix |
| **Why**   | A guard that silently does not run gives false confidence |

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from
