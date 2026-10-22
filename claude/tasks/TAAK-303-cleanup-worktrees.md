# TAAK-303 — Clean up stale worktrees and branches

**Status**: step 1/3 — not started  
**Created**: 2026-10-22  
**Goal**: Only branches and worktrees with work not on `main` remain; everything else is removed.

## Context for the next agent

- `git branch` shows 10 `worktree-agent-*` branches (most with a worktree under `.claude/worktrees/`) and `taak-298-perf-wip` (merged into main on 2026-10-22, also on origin).
- Never delete unmerged work without checking: per branch `git log main..<branch>`; if commits are not on main, list them and ask the user.

## Steps

- [ ] 1. Per branch/worktree: `git log --oneline main..<branch>` and `git -C <worktree> status --short`
- [ ] 2. Remove worktrees and branches that hold nothing new (local and `origin/taak-298-perf-wip`)
- [ ] 3. Report anything left with its unmerged commits to the user

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | git branches, `.claude/worktrees/` |
| **How**   | Housekeeping |
| **Why**   | Stale worktrees confuse agents and searches (Grep hits copies under `.claude/worktrees/`) |

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from
