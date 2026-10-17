# Agent workflow — rules for documentation and task management

Rules that **all agents** in this project follow for task management, cross-references and document splitting.

See `claude/docs/INDEX.md` for the full documentation index.

---

## 1. Task management

Use the task system (`claude/tasks/`) for every task that:

- may take more than one session
- may switch account or model halfway through
- has several consecutive steps

| Action                           | Skill             | File                                    |
| -------------------------------- | ----------------- | --------------------------------------- |
| New task                         | `/taak-start`     | `claude/tasks/TAAK-<NNN>-<slug>.md`     |
| Task with analysis and reasoning | `/taak-specifiek` | same                                    |
| Resume task                      | `/taak-oppakken`  | same; done → `claude/tasks/archief/`    |

### Step statuses

| Marker | Meaning                                       |
| ------ | --------------------------------------------- |
| `[ ]`  | To do                                         |
| `[→]`  | In progress (current step)                    |
| `[x]`  | Done                                          |
| `[!]`  | Blocked — reason is in the context section    |

### When stopping early

Always write in the **Context** section: what was done, what is still open, which files were changed, and any blockers.

---


### Agents in a worktree: record progress per step

An agent that works in its own worktree updates its task file after **every** finished step (and before every long
corpus run): status line, `[x]`/`[→]`, and under "Context for the next agent" what is done, the next step, changed
files, where results are (`<corpus>/…`) and open questions. It commits that immediately in its branch as a WIP commit
and puts its branch name and worktree path at the top of the task. Then anyone can resume it with `/taak-oppakken`.

---

## 2. Cross-references

Every time an agent writes or updates documentation, cross-references **must** be added.

- New document → add to `claude/docs/INDEX.md`
- Document describes something a skill uses → `## See also` in that skill
- Reference to code → exact path (`src/Unshackle.Core/Fixer.cs`)
- Skill created or extended → reference in `INDEX.md`

### Format of `## See also` (at the bottom of every document or skill)

```markdown
## See also

- `claude/docs/INDEX.md` — volledige documentatie-index
- `docs/projectinfo/architectuur/testen.md` — proofs en types
- `src/Unshackle.Core/Fixer.cs` — implementatie
```

Rules:

- Always **relative paths** from the project root
- **No decorative links**: only if it saves a future agent tokens

---

## 3. Document splitting

| Type                | Consider splitting | Hard maximum  |
| ------------------- | ------------------ | ------------- |
| CLAUDE.md (root)    | > 80 lines         | 120 lines     |
| claude/docs/\*.md   | > 100 lines        | 150 lines     |
| Skill SKILL.md      | > 80 lines         | 120 lines     |
| Implementation plan | > 200 lines        | always split  |

How to split:

1. New file in the most logical place (see `INDEX.md`)
2. Original: short summary + `Zie claude/docs/<onderwerp>.md voor de volledige uitleg.`
3. Add the new file to `INDEX.md`
4. `## See also` in the new file that refers back

Do not split: short pieces that are always read together, `## See also` sections, task files (they grow on purpose).

---

## 4. Behavior rules

### Communication

- Dutch with the user; code, names and commit messages in English
- No bullet points in short answers or refusals: use prose
- If the user says it is done or "stop", do not ask whether there is anything else you can do

### Memory and context

- **Do not retrieve old task context** unless the user refers to it

### Token efficiency

- `grep`/`find` **before** `Read` when you are looking for one line or pattern
- No summary of what you just did: the user sees the diff
- No lengthy explanation of code you just wrote unless asked

---

## 5. Creating documentation — checklist

- [ ] Already documented somewhere? (check `INDEX.md` first)
- [ ] Right location (root `CLAUDE.md` / `docs/projectinfo/` / `claude/docs/`)?
- [ ] `## See also` added?
- [ ] `INDEX.md` updated?
- [ ] Skills that touch this topic updated?
- [ ] Within the splitting threshold?

## 6. Main stays green — hard rule

**Never** push or merge to `main` if even a single test fails, old or new.

Before every push or merge to `main`, locally and all green:

```bash
dotnet build
dotnet test
dotnet test --solution samples/SampleShop/SampleShop.slnx
```

- If anything fails: do not push. Fix the cause first, even if the failure is in a test you did not touch yourself.
- Skipping, deleting or weakening a test to get green is not allowed without the user's approval.
- The same steps run in CI (`.github/workflows/ci.yml`). GitHub does not enforce this yet (private repo without
  Pro), so this rule is the safeguard.
- **Only push what demonstrably works, even if it is only part of the work** (user decision 2026-10-13). Better to push a small, finished and green piece
  than a large piece part of which is doubtful; keep half-finished features behind Manual or out of the pattern list so CI stays green.
- **CI is the yardstick, not the local run.** Green locally on Windows does not prove CI (Linux) is green. After every push: `gh run watch` /
  `gh run list -L 1` until the run is done. Red → fix immediately (or revert the commit) before any other work, and tell the user.
  Before a push, also check that the previous CI run on `main` was green; if main is already red, fix *that* first.

## See also

- `claude/docs/INDEX.md` — documentation index
- `.claude/skills/taak-start/SKILL.md` — create a task
- `.claude/skills/taak-oppakken/SKILL.md` — resume a task
