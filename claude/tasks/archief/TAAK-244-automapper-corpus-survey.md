# TAAK-244 — AutoMapper corpus survey

**Status**: done (7/7)  
**Created**: 2026-10-14  
**Goal**: a larger AutoMapper ≤14 corpus (12–20 repos, pinned, preferably with tests) and API frequencies per project, so rows 9–15 of `claude/tasks/automapper-queue.md` can be ordered by data.

## Context for the next agent

- Row 7 of `claude/tasks/automapper-queue.md`. The old corpus (`docs/projectinfo/architectuur/corpus-mediatr-automapper.md`) is mainly nopCommerce.
- Repos are cloned shallowly into `D:\playground\corpus-automapper\src\<short-name>\` (outside the repo, untrusted: only read, never run their scripts). List: `D:\playground\corpus-automapper\repos.txt` (`<name> <url> <ref> <solution path>`).
- Builds (`dotnet restore`/`build`) only with `TEMP`/`TMP` = `<temp-dir>` (this machine blocks exes from `%TEMP%`). Max a few minutes per repo; otherwise "no build".
- Measurement: regex over `*.cs` (no Roslyn), like the existing corpus doc.
- Another agent works in `src/Unshackle.Core/Fixes/AutoMapper/` (TAAK-245): do not touch `src/`.

- **Result**: 20 repos (12 build cleanly, 6 partially: only a front-end/AppHost/Rust project fails, 1 no build, abp not tried).
  Numbers, method and recommended order: `docs/projectinfo/architectuur/corpus-automapper.md`. Queue updated: Corpus column of rows 8–15,
  new proposed rows 17–20 (`member-list`, `configuration-test`, `convert-using`, `value-resolver`), recommended order under the table.
- Measurement script + raw JSON: `D:\playground\corpus-automapper	ools\` (outside the repo). Build logs: `D:\playground\corpus-automapper\logs\`.

## Steps

- [x] 1. Task file + INDEX row
- [x] 2. Choose 12–20 repos on AutoMapper ≤14 (gh search), pin at a commit/tag
- [x] 3. Clone shallowly, write `repos.txt`, check restore/build per repo
- [x] 4. Measure API frequencies per repo (regex script)
- [x] 5. Write `docs/projectinfo/architectuur/corpus-automapper.md` (+ INDEX, See also, link from `corpus-mediatr-automapper.md`)
- [x] 6. Update `claude/tasks/automapper-queue.md` (Corpus column rows 8–15, recommended order, row 7 done)
- [x] 7. Finish: status done, move to `claude/tasks/archief/`, fix the INDEX path

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `docs/projectinfo/architectuur/corpus-automapper.md`, `claude/tasks/automapper-queue.md`, `claude/docs/INDEX.md`; corpus outside the repo |
| **How**   | survey: clone + regex measurement, no code change |
| **Why**   | the remaining AutoMapper rows were ordered by a corpus skewed to nopCommerce |

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/automapper-queue.md` — the queue this survey orders
- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` — the old, small corpus
