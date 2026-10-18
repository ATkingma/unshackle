---
name: taak-oppakken
description: Resume an existing task after an account switch or a new session. Use when the user says 'pick up task X' / 'pak taak X op', 'continue with TAAK-003' / 'ga verder met TAAK-003', or 'where were we' / 'waar waren we gebleven'.
---

# Task-Resume Skill

Reads an existing task file from `claude/tasks/` and resumes the task exactly at the last step.

Project root: the repository root (run the commands below from there)

## Step 1: Find the task file

If the user mentions a number or name:

```bash
ls claude/tasks/*.md   # actief; voltooid staat in archief/
```

If it is unclear which task: show the list and ask which one.

## Step 2: Read the task file fully

```bash
cat claude/tasks/TAAK-<NNN>-<slug>.md
```

Identify:

- The **current step** (marked with `[→]`, or the first `[ ]` after all `[x]`)
- The **context** for the next agent
- **Changed files** so far

## Step 3: Verify the current state

Read the relevant files that should already have been changed:

```bash
git log --oneline -5
git status --short
```

Check whether the state matches what the task file describes. If there are discrepancies: report them and ask for confirmation before continuing.

## Step 4: Mark the current step as in progress

Replace `[ ]` of the current step with `[→]` in the task file.  
Update the **Status** line: `step <N>/<total> — in progress`.  
Add under **Context**: which account/model is taking over now, and when.

## Step 5: Carry out the step

Get to work. After finishing each step:

- Replace `[→]` with `[x]`
- Set the next step to `[→]`
- Add changed files to the list
- Update **Status**: `step <N+1>/<total> — in progress`

## Step 6: When the task is done

Replace all `[→]` with `[x]`, update Status to `✅ done — <date>`.  
Move the file to `claude/tasks/archief/` if that folder exists; otherwise leave it in place.

## Step 7: When stopping early (account switch)

Write in the **Context** section:

- What was done in this session
- What the next step is and what it needs
- Any blockers or open questions

The next agent picks it up via `/taak-oppakken TAAK-<NNN>`.

## See also

- `.claude/skills/taak-start/SKILL.md` — create a new task
- `claude/docs/agent-werkwijze.md` — rules for task management
