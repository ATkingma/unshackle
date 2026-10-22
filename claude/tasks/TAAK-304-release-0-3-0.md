# TAAK-304 — Release 0.3.0

**Status**: step 1/3 — not started  
**Created**: 2026-10-22  
**Goal**: Unshackle 0.3.0 on nuget.org with the TAAK-298 speed-up, after the user's go.

## Context for the next agent

- nuget.org has 0.1.0 and 0.2.0 (tag `v0.2.0`, 2026-10-19). Since then on main: TAAK-295, 297, 299, 300 (kept-reason rendering), 298 (parallel files, pre-filter: akka 547 s -> 201 s; corpus 36/36, reports identical to main).
- Release flow: see `claude/tasks/TAAK-019-lancering.md` (tag push -> release workflow, Trusted Publishing). Publishing is an external action: **only after the user's explicit go**.
- Depends on TAAK-301 (docs of the new options).

## Steps

- [ ] 1. Changelog / release notes since v0.2.0 (user-visible: speed, new options, report wording changes from TAAK-300)
- [ ] 2. Version to 0.3.0, verify-head, push, CI green
- [ ] 3. User go -> tag `v0.3.0`, release run green, install from nuget.org checked

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `Unshackle.Interface.csproj` (Version), release notes |
| **How**   | Release |
| **Why**   | The speed-up only helps users once it is published |

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from
