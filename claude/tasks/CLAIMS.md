# File claims — agent pool 2026-10-22

Live register of who owns which files. **Only the main session edits this file.** Agents read it (absolute path
`D:/playground/projects/unshackle/claude/tasks/CLAIMS.md`) before touching any file; a file not claimed by you is
read-only for you. Need another file? Put a `CLAIM REQUEST: <path> — <why>` line in your final report; the main session
grants it to you or your successor. Budget: 50 agents in total across the roadmap; at most 3 CPU-heavy at once.

## Always main-session only

`claude/docs/INDEX.md`, `claude/docs/overdracht.md`, `claude/docs/council.md`, `docs/projectinfo/project.md`,
`docs/projectinfo/README.md`, `Directory.Packages.props`, `Unshackle.slnx`, `src/Unshackle.Interface/Cli.cs`,
`claude/tasks/POOL-2026-10-22.md`, this file.

## Active claims

| # | Agent | Task | Files (owned, write) | Reserved task nrs | Since |
| --- | --- | --- | --- | --- | --- |
| 1 | docs | TAAK-301 | `README.md`, `docs/` except `docs/projectinfo/`, `claude/tasks/TAAK-301-*.md` | — | 2026-10-22 |
| 2 | tooling | TAAK-302 | `claude/tools/verify-head.sh`, `.claude/hooks/check-pattern-docs.sh`, `claude/tools/check_pattern_docs.py` (new), `claude/tasks/TAAK-302-*.md` | — | 2026-10-22 |
| 3 | analysis | TAAK-306 st 1–2 | `claude/tasks/ANALYSE-manual-round-2.md` (new), `claude/tasks/TAAK-306-*.md` | 311–319 | 2026-10-22 |
| 4 | moq | TAAK-307 st 1 | `docs/projectinfo/architectuur/corpus-moq.md`, `claude/tools/corpus-moq-repos.txt`, `claude/tasks/TAAK-307-*.md` | 320–339 | 2026-10-22 |
| 5 | polly | TAAK-308 st 1–2 | `docs/projectinfo/architectuur/corpus-polly.md`, `claude/tools/corpus-polly-repos.txt`, `claude/tasks/TAAK-308-*.md` | 340–349 | 2026-10-22 |
| 6 | epplus | TAAK-309 st 1 | `docs/projectinfo/architectuur/corpus-epplus.md`, `claude/tools/corpus-epplus-repos.txt`, `claude/tasks/TAAK-309-*.md` | 350–359 | 2026-10-22 |
| 7 | masstransit | TAAK-310 st 1 | `docs/projectinfo/architectuur/corpus-masstransit.md`, `claude/tools/corpus-masstransit-repos.txt`, `claude/tasks/TAAK-310-*.md` | 360–369 | 2026-10-22 |
| — | main | TAAK-303, 305 st 1 | git branches/worktrees, `claude/tasks/TAAK-303-*.md`, `claude/tasks/TAAK-305-*.md` | — | 2026-10-22 |

## Released claims

| Agent | Task | Result | Released |
| --- | --- | --- | --- |

## Spawn log

Agents spawned: 7 / 50.

## See also

- `claude/tasks/POOL-2026-10-22.md` — waves and rules
- `claude/docs/agent-werkwijze.md` — task rules
