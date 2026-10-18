---
name: taak-specifiek
description: Create an in-depth task file with explicit reasoning: what is changed, where, how, why, and what the improvement is over the current situation. Use when a task is architectural or risky, or touches multiple projects, and a cold agent must immediately understand why each choice was made.
---

# Task-Specific Skill

Variant of `/taak-start` for tasks that require reasoning. Investigates the codebase, compares old vs. new, and records decisions concisely so that no subsequent agent has to derive them again.

Project root: the repository root (run the commands below from there)

## Step 1: Determine the next task number

```bash
ls claude/tasks/TAAK-*.md claude/tasks/archief/TAAK-*.md 2>/dev/null \
  | grep -oP 'TAAK-\K\d+' | sort -n | tail -1
```

New number = last + 1 (or 001). Always three digits.

## Step 2: Investigate — read only what is needed

Do targeted research on the relevant files. Goal: fully understand the **current situation** before writing the plan.

Guidelines:

- Read only files directly affected by the task
- Use `grep` to find patterns; do not read entire directories
- Note: what already exists, what is missing, where the pain is

## Step 3: Reason — decide on the approach

Answer internally (not in the task file):

- Are there several possible solutions? Which is better and why?
- What are the risks or dependencies?
- Do the changes touch other projects (Interface/Core/DataAccess) or conventions?

## Step 4: Write the task file

File name: `claude/tasks/TAAK-<NNN>-<slug>.md`

Write concisely. Each section max 3-5 lines. No stating the obvious.

```markdown
# TAAK-<NNN> — <Name>

**Status**: step 1/<total> — not started
**Created**: <date>
**Goal**: <one sentence: what is done at the end>

## Context for the next agent

<What a cold agent needs to know to start right away — no explanation of known conventions, only the non-obvious:>

- Which files already exist and are relevant
- Which decisions have already been made
- Blockers or dependencies

## Analysis

### Current situation

<What is there now — concrete, with file path:line where relevant>

### Problem

<Why this does not work or is suboptimal — one sentence per point>

### New situation

<What is different after the task — concrete>

### Improvement

<Measurable or observable improvement: faster / safer / less error-prone / more consistent>

## Approach

### What

<Exact change: which file, which function, which pattern>

### Where

<File paths. No wildcards — exact locations based on research>

### How

<Implementation strategy in 2-4 steps. Refer to existing patterns in the project>

### Why this approach

<Why not the alternatives — brief, only if not obvious>

## Steps

- [ ] 1. <first step — actionable and well-scoped>
- [ ] 2. <second step>
- [ ] 3. <...>

## Risks & edge cases

| Risk   | Mitigation          |
| ------ | ------------------- |
| <risk> | <how it is handled> |

## Changed files

_(filled in during execution)_

## See also

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
```

## Step 5: Report to the user

```
Task created: claude/tasks/TAAK-<NNN>-<slug>.md
Investigated: <N files>
Key decision: <one sentence about the chosen approach>
To pick up: /taak-oppakken TAAK-<NNN>
```

## See also

- `.claude/skills/taak-start/SKILL.md` — lighter variant without reasoning
- `.claude/skills/taak-oppakken/SKILL.md` — resume a task after a session switch
- `claude/docs/agent-werkwijze.md` — task management rules
