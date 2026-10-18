---
name: taak-start
description: Create a new task file in claude/tasks/ for a project or feature. Use when the user says "create a task" / "maak een taak", "create a task for this project" / "maak een taak aan voor dit project", "describe this in an md" / "beschrijf dit in een md", "record this" / "leg dit vast", or gives a list of to-dos that need tracking. Creates a numbered TAAK-NNN file so any agent can pick up and continue the task.
---

# Task-Start Skill

Creates a numbered task file in `claude/tasks/` so that every agent — even after an account switch — knows exactly where the task stands and how to continue.

Project root: the repository root (run the commands below from there)

## Step 1: Determine the next task number

```bash
ls claude/tasks/TAAK-*.md claude/tasks/archief/TAAK-*.md 2>/dev/null \
  | grep -oP 'TAAK-\K\d+' | sort -n | tail -1
```

New number = last + 1 (or 001 if there are none yet). Always use three digits: 001, 002, …

## Step 2: Ask the user (or use the given context)

What you need:

- **Name/description** of the task (short, slug-friendly)
- **Goal**: what must be done at the end?
- **Steps**: work them out from the task description — number them, be concrete

If the user has already given a description: use it directly, do not ask again.

## Step 3: Write the task file

File name: `claude/tasks/TAAK-<NNN>-<slug>.md`

```markdown
# TAAK-<NNN> — <Name>

**Status**: step 1/<total> — not started  
**Created**: <date>  
**Goal**: <what must be done at the end>

## Context for the next agent

<Describe the background a cold agent needs here:>
- Which files already exist / have already been changed
- Which decisions have already been made, and why
- Open questions or blockers
- Relevant docs: see `claude/docs/INDEX.md`

## Steps

- [ ] 1. <first step — concrete and actionable>
- [ ] 2. <second step>
- [ ] 3. <...>

## Changes

| Field     | Content                                                                 |
| --------- | ----------------------------------------------------------------------- |
| **Where** | _(which files / projects are affected)_                                 |
| **How**   | _(what kind of change: new endpoint, refactor, migration fix, …)_       |
| **Why**   | _(why is this needed — bug, feature request, technical debt, …)_        |

_(extra rows may be added during execution for additional changes)_

## See also

- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/README.md` — architecture overview
```

## Step 4: Report to the user

Print: task number, file path, number of steps, and how the task can be picked up:

```
Task created: claude/tasks/TAAK-<NNN>-<slug>.md
To pick up in a new session: /taak-oppakken TAAK-<NNN>
```

## See also

- `.claude/skills/taak-oppakken/SKILL.md` — resume a task after a switch
- `claude/docs/agent-werkwijze.md` — rules for task management, cross-references, document splitting
