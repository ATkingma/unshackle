# Pattern loop

Every 15 minutes the main session starts a background agent that picks up one pattern from
`claude/tasks/patroon-queue.md`: first research in a task (`/taak-specifiek` format), then implementation.
Agents run in parallel. Progress and choices are in the task files; the queue says which task belongs to which pattern.

## Tick (main session, every 15 min)

1. Read `claude/tasks/patroon-queue.md`.
2. All rows `klaar`? → stop the loop (`CronDelete`), tell the user. Done.
3. Pick the first row with `open`, or `geblokkeerd` whose blocking task is done, or `mislukt` with attempt < 3.
   No candidate (everything `bezig`/blocked)? → start nothing.
4. Set the row to `bezig (wordt geclaimd)` so the next tick skips it.
5. Start one background agent with the **Agent brief** below plus the row number.
   Model: row 0 (foundation) → `opus`; patterns → `sonnet`; `mislukt` attempt 2 → `opus`, attempt 3 → `fable`.
6. When an agent has finished a pattern (row `klaar`): first start one background agent (`sonnet`) with the
   **Models brief** below and wait for its report. Then check `dotnet build`, `dotnet test` and
   `dotnet test --solution samples/SampleShop/SampleShop.slnx` yourself. All green → `git add -A` (without files of pattern agents still running), commit
   (`Add <patroon-ID> pattern`, English, no Claude mention) and `git push origin main` (standing instruction from the
   user, 2026-10-10). Run the checks on the commit itself in a separate worktree (`git worktree add --detach <scratchpad>/verify HEAD`), so that half-finished work of running agents does not count. Anything red → do not push (`claude/docs/agent-werkwijze.md` §6), report it.
7. Give the user a short report (what, where, choices, test result, commit hash).
8. **Keep going immediately**: after the push, do not wait for the next tick. If there is a candidate (step 3), claim and start it
   right away (steps 4–5). The cron also keeps starting an extra agent every 15 min (in parallel). (User, 2026-10-11.)

## Agent brief

You work in the repository root. First read `CLAUDE.md`, `docs/projectinfo/README.md` (+ `architectuur/core.md`),
`claude/docs/code-standaarden.md` and `claude/docs/agent-werkwijze.md`. Docs in Dutch, code in English.
Do not commit or push (see `claude/docs/agent-werkwijze.md` §6: main stays green). Other agents work in the same folder at the same time.

1. **Claim**: set your row in `claude/tasks/patroon-queue.md` to `bezig (TAAK-NNN)`. Was there already a task file
   for this row (blocked/failed)? Resume it according to `.claude/skills/taak-oppakken/SKILL.md` instead of creating a new one.
2. **Research → task**: create the task according to `.claude/skills/taak-specifiek/SKILL.md`. Research:
    - the FluentAssertions 7.2.2 API (overloads, `because` arguments, chains with `.And`/`.Which`);
    - the Shouldly 4.3.0 equivalent and differences in behavior;
    - cases that depend on the type, and cases that must become `Manual`;
    - occurrences in `samples/SampleShop/SampleShop.Tests/`.
      Record every choice with its reason in the task (section **Choices**).
3. **KingmaDoc plan** (slug = pattern ID with `.` → `-`, e.g. `fa-be-null`): if `docs/projectinfo/patterns/<slug>-plan.md`
   does not exist yet, `kingmadoc plan --no-input -o docs/projectinfo/patterns/<slug>-plan.md "<patroon-ID>: <één zin>"`.
   Then fill in scope, requirements (EARS, one per proof case or group), assumptions, risks and open questions from
   your task; leave no `_TODO_` where the task has the answer. `files_expected` = your pattern file,
   `FluentAssertionsFix.cs` and the proof folder. Put `feature: "<slug>"` in the frontmatter (otherwise the CLI makes a long slug from the description). Then `kingmadoc check <slug>` and `kingmadoc approve <slug>` (the loop has been
   approved by the user). The plan gets **no C4** (disabled in `.featuredoc.yml`; if there is a C4 section anyway, replace it with a link to `<slug>-modellen.md`). Add a row to the pattern table in `docs/projectinfo/README.md` (columns Plan, Modellen, Verificatie, Taak).
4. **Blocker**: is row 0 (foundation) not `klaar` yet? Set the implementation step to `[!]`, the row to
   `geblokkeerd (TAAK-NNN, wacht op fundament)` and stop.
5. **Implement** (only your own files):
    - `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/<Naam>Pattern.cs`;
    - one line in the `Patterns` list of `FluentAssertionsFix`;
    - proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/<Naam>/` (`*.before.cs` + `*.after.cs` or `*.manual.txt`),
      at least: simple case, `because` argument, every type-dependent case, every Manual case.
    - then `python3 claude/tools/proofs-doc.py` (updates `docs/projectinfo/architectuur/proofs.md`).
      Changing contracts in `Abstractions/`, `Models/` or the `Fixer` is **not** allowed; needed? → step `[!]` with reason, row `mislukt`.
6. **Verify**: `dotnet build` and `dotnet test` green. Does it fail because of a file lock or a half-finished file of another
   agent? Wait 30 s and retry (max 5×) before treating it as your own failure.
6b. **Pattern models**: write `docs/projectinfo/patterns/<slug>-modellen.md` according to the **Pattern models brief**
   below and link it from the plan (section "Modellen") and the README row.
7. **KingmaDoc verify**: write `docs/projectinfo/patterns/<slug>-verify.md` according to the KingmaDoc skill (verify mode,
   `~/.claude/skills/kingmadoc/SKILL.md`). `kingmadoc verify` may serve as a start, but judge only your own files
   (other agents are making changes at the same time). Each REQ: met (name the proof) / partially / not. Fill in the real build/test outcome.
8. **Finish**: task `✅ done`, to `claude/tasks/archief/`, row `klaar (TAAK-NNN)`. If it does not work out:
   context in the task, row `mislukt (TAAK-NNN, poging N)`. The SubagentStop hook blocks your stop as long as the plan, verify
   or the README row of your pattern is missing.
9. **Report** (max 8 lines): pattern, changed files, key choices, proofs, test result.

### Pattern models brief (per pattern)

`docs/projectinfo/patterns/<slug>-modellen.md`, in Dutch, according to the explaining-code skill
(`~/.claude-timme/skills/explaining-code/SKILL.md` + only the sections of `reference/models.md` that you draw).
Facts from the code of **this** pattern; invent nothing. Three numbered figures in D2, each with a table:

1. **Class diagram**: the pattern class, `IPattern`, the helpers it uses (e.g. `FluentAssertionsSyntax`),
   `PatternResult`/`FixedResult`/`ManualResult`, and the FA and Shouldly types it recognizes or creates.
2. **Sequence diagram**: `Fixer` → `<Naam>Pattern.Find` → `Rewrite` → helpers/`SemanticModel` → `Fixed`/`Manual`,
   with the real method names from the code.
3. **Decision tree** (UML activity or flowchart): every question `Rewrite` asks (type, overload, because args, chain,
   result used…) with as leaves the exact Shouldly form or the Manual reason. Each leaf names the proof that proves it.

Render with `kingmadoc render docs/projectinfo/patterns/<slug>-modellen.md` (images in `docs/projectinfo/patterns/img/`).
`## See also` at the bottom (plan, verify, pattern file, proof folder).

### Models brief (after every finished pattern)

Update the visual models of the whole project in the repository root, according to the
explaining-code skill (`~/.claude-timme/skills/explaining-code/SKILL.md` + only the needed sections of
`reference/c4-model.md` and `reference/models.md`). In Dutch. Only docs and images; do not change code,
do not commit.

- Facts: `kingmadoc explain facts` (and `--only` for parts). Invent nothing; text must match the figures.
- Location: **not** `docs/explain/`, but `docs/projectinfo/`:
  - `README.md`: C4 Context, Container and Component (Core) as D2 figures (replace the Mermaid blocks), plus a
    "Modellen" table with links to the files below.
  - `modellen/klassen.md`: UML class diagram of contracts and models (`Abstractions/`, `Models/`).
  - `modellen/fix-flow.md`: UML sequence of `FixAsync`: Fixer → IFix → IPattern.Find/Rewrite → ReplaceNode → usings.
  - `modellen/pattern-beslissing.md`: UML activity: how a pattern chooses between Fixed and Manual (generic, with the
    shared steps from `FluentAssertionsSyntax`).
  - `modellen/patronen.md`: overview of all patterns (table + figure: which FA form → which Shouldly form,
    which cases Manual), updated with the newest pattern.
  - Other models only if the signal in the code is clear (see the decision table in `reference/models.md`).
  - Figures numbered per document, max ~15 elements per figure, each figure with a table; `## See also` at the bottom;
    new files in `claude/docs/INDEX.md`.
- Render: `kingmadoc render docs/projectinfo/README.md docs/projectinfo/modellen/*.md` (PNG + SVG + D2 in `img/`).
  On a D2 error: fix the `.d2` and render again. If it already exists: only update what the new pattern touches.
- Report (max 5 lines): changed files, number of figures, what is new.

### Extra for row 0 (foundation)

Build what `docs/projectinfo/architectuur/core.md` describes, minimally and following the code standards:

- `Abstractions/`: `IPattern`, `IFix`, `IFixer` (with XML docs);
- `Models/`: `Finding`, `PatternResult` + `FixedResult` + `ManualResult`, `FixRequest`, `FixReport`;
- `Fixer`: runs all patterns of the chosen fix on a `Solution`, catches exceptions per pattern (Manual), returns a
  new `Solution` + report;
- `Fixes/FluentAssertions/FluentAssertionsFix.cs` with a `Patterns` list;
- tests: `Helpers/TestReferences` (real assemblies per fix), an in-memory solution helper, `ProofTests` that finds all
  proofs and does the compile check (before with FA, after with Shouldly);
- the first pattern `fa.should-be` with proofs, so the whole chain is proven;
- unit tests for the `Fixer` with NSubstitute.
  Record the shape of `IPattern` and how rewrites get into the `Solution` under **Choices**, with reasons.

## See also

- `claude/tasks/patroon-queue.md` — the queue
- `.claude/skills/taak-specifiek/SKILL.md` — task format
- `claude/docs/code-standaarden.md` — code style
