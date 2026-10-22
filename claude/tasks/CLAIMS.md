# File claims — agent pool 2026-10-22

Live register of who owns which files. **Only the main session edits this file.** Agents read it (absolute path
`D:/playground/projects/unshackle/claude/tasks/CLAIMS.md`) before touching any file; a file not claimed by you is
read-only for you. Need another file? Put a `CLAIM REQUEST: <path> — <why>` line in your final report; the main session
grants it to you or your successor. Budget: 25 agents in total (user, 2026-10-22; was 50) across the roadmap; at most 3 CPU-heavy at once.

## Always main-session only

`claude/docs/INDEX.md`, `claude/docs/overdracht.md`, `claude/docs/council.md`, `docs/projectinfo/project.md`,
`docs/projectinfo/README.md`, `Directory.Packages.props`, `Unshackle.slnx`, `src/Unshackle.Interface/Cli.cs`,
`claude/tasks/POOL-2026-10-22.md`, this file.

## Active claims

| # | Agent | Task | Files (owned, write) | Reserved task nrs | Since |
| --- | --- | --- | --- | --- | --- |
| 4 | moq | TAAK-307 st 1 | `docs/projectinfo/architectuur/corpus-moq.md`, `claude/tools/corpus-moq-repos.txt`, `claude/tasks/TAAK-307-*.md` | 320–339 | 2026-10-22 |
| 5 | polly | TAAK-308 st 1–2 | `docs/projectinfo/architectuur/corpus-polly.md`, `claude/tools/corpus-polly-repos.txt`, `claude/tasks/TAAK-308-*.md` | 340–349 | 2026-10-22 |
| 6 | epplus | TAAK-309 st 1 | `docs/projectinfo/architectuur/corpus-epplus.md`, `claude/tools/corpus-epplus-repos.txt`, `claude/tasks/TAAK-309-*.md` | 350–359 | 2026-10-22 |
| 12 | scout-swashbuckle | candidate: Swashbuckle → Microsoft.AspNetCore.OpenApi | `docs/projectinfo/architectuur/candidate-swashbuckle.md` (new), `claude/tasks/TAAK-410-scout-swashbuckle.md` | 411–419 | 2026-10-22 |
| — | main | TAAK-303, 305 st 1 | git branches/worktrees, `claude/tasks/TAAK-303-*.md`, `claude/tasks/TAAK-305-*.md` | — | 2026-10-22 |

## Released claims

| Agent | Task | Result | Released |
| --- | --- | --- | --- |
| docs | TAAK-301 | done, merged 470c734 | 2026-10-22 |
| analysis | TAAK-306 st 1–2 | done, TAAK-311..315 created, merged 470c734 | 2026-10-22 |
| tooling | TAAK-302 | done, merged | 2026-10-22 |
| masstransit | TAAK-310 st 1–2 | done, waits for council | 2026-10-22 |
| scout-identityserver | TAAK-380 | no-go fix, go report-only (council) | 2026-10-22 |
| scout-specflow | TAAK-370 | conditional go (council) | 2026-10-22 |
| scout-imagesharp | TAAK-390 | conditional no-go (council) | 2026-10-22 |
| scout-newtonsoft | TAAK-420 | no-go blanket (council) | 2026-10-22 |
| scout-efbulk | TAAK-430 | conditional go narrow (council) | 2026-10-22 |
| scout-testfw | TAAK-440 | xUnit go / NUnit no-go (council) | 2026-10-22 |
| scout-pdf | TAAK-400 | iTextSharp slice conditional, rest no-go (council) | 2026-10-22 |

## Spawn log

Agents spawned: 15 / 25 (7 wave 1, 8 library scouts).

## See also

- `claude/tasks/POOL-2026-10-22.md` — waves and rules
- `claude/docs/agent-werkwijze.md` — task rules
