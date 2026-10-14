# Handover (updated 2026-10-14)

Read this first if you are taking over this session. Then `CLAUDE.md` and `claude/docs/INDEX.md`.

## Status (2026-10-14)

**CI on main is green again** since `91f85de` (Latin-1 read on every platform; fixed the Linux-only failure of
`SampleShopTests.Fix_FileInLatin1_LeavesItAndMigratesTheRest`). `c35b41a` (TAAK-220..225, -240) is pushed with it.

**2026-10-14:** the corpus regression proofs (step 3b of 2026-10-13: 31 proofs from Akka, Steeltoe, LiteDB, kgrzybek, eShop) got their
`after.cs`/`manual.txt`; one bug found and fixed: TAAK-241 (`.And` chain wrote `ShouldNotBeEmpty()` twice). All docs are now in English
(the last two files, `CORPUSRUN-fa.md` and `corpus-mediatr-automapper.md`, translated too). 1141/1141, SampleShop 38/38.

**First action for the next agent:**
1. Check whether the corpus repetition of TAAK-224/225 is done: `<corpus>/corpus-fa/_results/r5-224/` (akka, steeltoe). The section is still missing from
   `claude/tasks/CORPUSRUN-fa.md`: compare `compare.json` with round 5 (`_results/r5/`): expect fewer Manual, 0 new regressions.
   If `r5-224/akka` is incomplete, rerun `tools/corpus-regression-r4.sh` for Akka and Steeltoe.
2. FA corpus round 5 on all 16 solutions (`tools/corpus-regression-r4.sh`, publish the tool from a clean worktree of HEAD). Goal: 16/16 builds,
   0 regressions except Akka DData `ReplicatorMessageSerializerSpec` (TAAK-200, asymmetric `Equals` in Akka; listed as a warning in the report).
   Green → FA = 100%, update the roadmap in `docs/projectinfo/project.md`.
3. Then step 5: AutoMapper (`claude/tasks/automapper-queue.md`, ~40% done). User decision: first work it out further (split row 7), *then* a large corpus run. Priority FA → MediatR → AutoMapper, each to 100% first.

**Roadmap:** 1 Scan 100% · 2 FA ~97% · 3 MediatR 100% (7/9; evo1/evo2 rightly Manual; kgrzybek archtests: warning TAAK-240) · 4 Launch ~80% (waiting on user) · 5 AutoMapper ~40% (step 6 SharpGrep dropped by the user, 2026-10-14).

**Corpus status:** FA round 4: 16/16 builds, 13/16 green; after TAAK-220..223 (round 5, partial run) LiteDB and Steeltoe also 0 regressions, Akka 1 (known).
MediatR round 6: jason, ardalis, amantin, nadirbad, eShopOnWeb, dotnet/eShop, kgrzybek migrated.

**User decisions today:** TAAK-200 → "Fixed with warning" for possibly asymmetric `Equals`. Chosen by agents themselves (present to the user when in doubt):
TAAK-220/221/222 → Manual (AggregateException unwrapping, cancelled task, meta-test on own helper); TAAK-214 → archtest difference legitimate.

**Way of working that worked:** parallel agents with strictly separated files (MediatR code / FA patterns / `Fixer.cs`+`UsingDirectives.cs`),
reserve task numbers per agent in advance, `TEMP`/`TMP` on a `D:` folder, never kill other people's dotnet processes, one commit+push after all agents.
## Local environment (Windows)

- This machine blocks starting `.exe` files from `%TEMP%` ("Access is denied"). The two `SampleShopTests` integration tests that build and test a fixed copy therefore fail locally.
  Run them with `TEMP`/`TMP` pointing to a folder outside `%TEMP%` (e.g. `TEMP=<map op D:> TMP=<map op D:> dotnet test`); in CI (Linux) this does not apply.
- `jq` is missing in Git Bash: `.claude/hooks/check-pattern-docs.sh` fails; do the check by hand (see the script).

## Push procedure

Tasks to `archief/` → `git add -A` → commit → checks on the commit in a separate worktree, checked out with LF like CI
(`git -c core.autocrlf=false worktree add --detach <map buiten %TEMP%> HEAD`; with the global `autocrlf=true`, 15 tests fail on CRLF) (`dotnet build`, `dotnet test`,
`dotnet test --solution samples/SampleShop/SampleShop.slnx`, docs check) → everything green → `git push origin main`. Anything red → do not push, report it
(`claude/docs/agent-werkwijze.md` §6). Before every commit, grep for local paths (playground folder, user folder, `AppData`); write `<probe-dir>/…` or `<corpus>/…`.

## Working agreements with the user (do not forget)

- Speak Dutch; code, names and commits in English. **No Claude mention** in commits or PRs.
- **Never push to `main` with a failing test.** Pushing after every finished piece of work is a standing instruction, always after the worktree check.
- Every pattern: task (`/taak-specifiek`) → KingmaDoc plan → build + proofs → `<slug>-modellen.md` (class, sequence, decision tree) → verify → update general models → push. See `claude/docs/patroon-loop.md`.
- In a loop: after a finished pattern, start the next one **immediately**; do not wait for the cron.
- Working autonomously with agents is desired; give a short report after each agent. Always ask first before far-reaching external actions (making the repo public, publishing to NuGet, posting).
- Code standards: `claude/docs/code-standaarden.md` (e.g. never `sealed`, classic constructors, `CancellationToken` required). Design patterns: GoF/refactoring.guru.
- Visual models (C4, UML) are appreciated: see `docs/projectinfo/`.

## Known open issues

- Decision trees of some patterns have >12 arrows (not split).
- `.vibe-wise/` (learning notes) is in the repo; agree on this before making it public.
- Repo is private: GitHub does not enforce branch protection (no Pro).

## See also

- `claude/tasks/TAAK-019-lancering.md` — next step
- `claude/docs/patroon-loop.md` — pattern workflow
- `claude/docs/agent-werkwijze.md` — task management, §6 main stays green
