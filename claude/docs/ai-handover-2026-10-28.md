# AI handover 2026-10-28 — continue the roadmap loop

For the next AI session (any machine). Read this first, then `CLAUDE.md`, `claude/docs/overdracht.md` ("State on 2026-10-28")
and `claude/docs/INDEX.md`. Talk to the user in Dutch; everything written in the repo is English.

## 1. What the user asked for

Standing assignment (2026-10-27): "make new tasks from the roadmap/handover, run them with sub-agents, use the council for
design choices, merge per agent after verify-head + green CI, keep the handover up to date; keep going until tokens run out".
Agent cap: max 8 sub-agents at once (the temporary 10 ended at 02:10). The council decides open design questions; the user
only decides external actions (release 0.3.0 = TAAK-304, publishing, posts).

## 2. State of main

`main` is green at `85d5d8d` (handover commit on top of `ab8ae4d`, CI green). Everything merged on 2026-10-27 is listed in
`claude/docs/overdracht.md`. Council rounds 16-22 are in `claude/tasks/COUNCIL-round-16.md` … `-22.md` and summarised in
`claude/docs/council.md` (22 is on branch `council-22`, not yet on main).

## 3. Open branches on origin (not on main) — do these first

| Order | Branch | State | Action |
| --- | --- | --- | --- |
| 1 | `taak-341c` | xUnit 5f + `record-exception-async`, `type-name-attribute`, `extensibility`; VERIFY OK on its own base | merge |
| 2 | `council-22` | round 22 decisions X10-X14, P11; docs only | merge |
| 3 | `taak-355` | Polly kept row names the added Resilience package (P11); finished, verify state unknown | verify, merge |
| 4 | `taak-341d` | WIP steps 5g-5j of `claude/tasks/TAAK-341-xunit-v3.md`; unverified | finish (new agent), verify, merge |
| 5 | `efbulk-corpus` | WIP EF bulk corpus harness (TAAK-342 step 9); round not finished | rerun round, record `CORPUSRUN-efbulk.md` |
| 6 | `moq-rerun-wip` | only a harness change; no results (TAAK-330 step 5) | rerun round, record in `CORPUSRUN-moq.md` |
| 7 | `epplus-corpus-wip` | WIP EPPlus re-measure (TAAK-327 step 7) | rerun round, record in `CORPUSRUN-epplus.md` |

Expect merge conflicts mostly in `claude/docs/INDEX.md`, `claude/docs/council.md` and task files moved to `archief/`
(keep both sides, keep the `archief/` path, no duplicate rows). Code conflicts happened in `Cli.cs` (keep every fix
registration) and `IConsoleView`/`ScanCommand` (already unified in `integ-1`).

## 4. Next work after that (roadmap)

1. First corpus rounds for the new fixes: SpecFlow→Reqnroll (TAAK-340 step 7), xUnit v3 (TAAK-341 step 7: tests must be
   discovered, same count before and after — council X2), EF bulk (TAAK-342 step 9).
2. TAAK-340 remaining: `<Using Include="TechTalk.SpecFlow" />` items, `specflow.json` → `reqnroll.json` (G5),
   cucumber-expression risk; backlog TAAK-353 (per-project SpecFlow migration).
3. Update the roadmap table in `docs/projectinfo/project.md`: add step 11 SpecFlow→Reqnroll, 12 xUnit v3, 13 EF bulk; refresh
   steps 7-9.
4. Regenerate `docs/projectinfo/architectuur/proofs.md` with `claude/tools/proofs-doc.py`, then convert it to LF (the script
   writes CRLF on Windows and rewrites ~300 lines; do it when no other branch touches it).
5. Release 0.3.0 (TAAK-304) only on the user's go. Polly fee starts 2026-11-16: worth mentioning to the user.

## 5. How the merge loop works (what worked)

- Each agent works in its own worktree (`isolation: "worktree"`), commits on a named branch, pushes the branch, never touches
  main. The brief always says: read CLAUDE.md + code-standaarden; LF line endings; no `git stash`; warnings are errors;
  `dotnet build` + `dotnet test` green; `bash claude/tools/verify-head.sh D:/tmpNNN/verify` **without a pipe** must print
  VERIFY OK; TEMP/TMP on D:; never clear `~/.nuget/packages`; never kill other dotnet processes; Co-Authored-By line on every
  commit including merges; undecided design choices → conservative option + "Council questions" in the task file.
- Main session merges one branch at a time: `git merge --no-ff origin/<branch>`, check no file turned CRLF, run
  `verify-head.sh <scratch-dir-on-D:>` (C:-temp blocks the test exe: "Access is denied"), push, `gh run watch` until green.
  Only then the next branch. Stop the chain on the first conflict, failed verify or red CI.
- When several branches overlap (same files, interface changes), spawn one integration agent that merges them one at a time
  onto `integ-N`, resolves conflicts, runs verify-head; then merge `integ-N` once.
- Council: one agent per round; have it spawn three sub-agents (Guardian, Pragmatist, Architect) and run a second round when
  they split. Rounds written by a single agent are proposals, not real council rounds.
- Reserve task numbers in the brief (next free: TAAK-356; TAAK-353 = SpecFlow per-project backlog, TAAK-349 = transitive pin
  reasons — one collision happened, check `claude/tasks` and `archief/` before assigning).

Helper scripts used on the Windows laptop (outside the repo, recreate if needed):

```bash
# merge.sh <branch> <message> <run-id>: merge, CRLF check, verify-head; exit non-zero on conflict or failed verify
git fetch -q origin
git merge --no-ff "origin/$1" -m "$2

Co-Authored-By: <model attribution>" || { echo CONFLICT; exit 1; }
bash claude/tools/verify-head.sh D:/vh/$3 > D:/vh/$3.log 2>&1; rc=$?
grep -q "VERIFY OK" D:/vh/$3.log && [ $rc -eq 0 ]

# pushci.sh: push main and wait for its CI run; exit non-zero when red
git push -q origin main
id=$(gh run list --branch main --limit 1 --json databaseId -q '.[0].databaseId')
gh run watch $id --exit-status
```

## 6. Pitfalls seen on 2026-10-27

- Agents sometimes convert files to CRLF; check every merge and convert back to LF.
- D: ran full (2 GB free) once; corpus harnesses must stop below 20 GB free (council H1). Clean old worktrees.
- `rm -rf` on top-level paths like `/d/vh` is blocked by a safety check; use a fresh subfolder instead.
- Too many parallel bash chains on Windows gave "fork: Resource temporarily unavailable" and broke a merge chain.
- Corpus agents report "waiting on background work" many times; only the final hand-back counts.
- A merge of a branch that an integration branch already contains can conflict; merge the integration branch instead and
  abort the single-branch merge.

## See also

- `claude/docs/overdracht.md` — state per day
- `claude/docs/agent-werkwijze.md` — task and document rules (§6 push rules)
- `claude/docs/council.md` — council decisions per round
- `docs/projectinfo/project.md` — roadmap
