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
| 8 | scout-specflow | candidate: SpecFlow → Reqnroll | `docs/projectinfo/architectuur/candidate-specflow.md` (new) | 370–379 | 2026-10-22 |
| 9 | scout-identityserver | candidate: IdentityServer4 → OpenIddict/Duende | `docs/projectinfo/architectuur/candidate-identityserver.md` (new) | 380–389 | 2026-10-22 |
| 10 | scout-imagesharp | candidate: ImageSharp → SkiaSharp/Magick.NET | `docs/projectinfo/architectuur/candidate-imagesharp.md` (new) | 390–399 | 2026-10-22 |
| 11 | scout-pdf | candidate: iTextSharp/iText7/QuestPDF → PDFsharp/PdfPig | `docs/projectinfo/architectuur/candidate-pdf.md` (new) | 400–409 | 2026-10-22 |
| 12 | scout-swashbuckle | candidate: Swashbuckle → Microsoft.AspNetCore.OpenApi | `docs/projectinfo/architectuur/candidate-swashbuckle.md` (new) | 410–419 | 2026-10-22 |
| 13 | scout-newtonsoft | candidate: Newtonsoft.Json → System.Text.Json | `docs/projectinfo/architectuur/candidate-newtonsoft.md` (new) | 420–429 | 2026-10-22 |
| 14 | scout-efbulk | candidate: Z.EntityFramework.Extensions/EFCore.BulkExtensions → EF Core ExecuteUpdate/Delete | `docs/projectinfo/architectuur/candidate-efbulk.md` (new) | 430–439 | 2026-10-22 |
| 15 | scout-testfw | candidate: xUnit v2 → v3, NUnit 3 → 4 | `docs/projectinfo/architectuur/candidate-testfw.md` (new) | 440–449 | 2026-10-22 |
| — | main | TAAK-303, 305 st 1 | git branches/worktrees, `claude/tasks/TAAK-303-*.md`, `claude/tasks/TAAK-305-*.md` | — | 2026-10-22 |

## Released claims

| Agent | Task | Result | Released |
| --- | --- | --- | --- |

## Spawn log

Agents spawned: 15 / 50 (7 wave 1, 8 library scouts).

## See also

- `claude/tasks/POOL-2026-10-22.md` — waves and rules
- `claude/docs/agent-werkwijze.md` — task rules
