# TAAK-305 — ReadyToRun tool packages

**Status**: step 1/3 — not started  
**Created**: 2026-10-22  
**Goal**: Decide, with measurements, whether RID-specific ReadyToRun tool packages are worth it, and ship them if so.

## Context for the next agent

- From TAAK-298 step 2: `PublishReadyToRun` needs a RuntimeIdentifier; the tool is packed portable. .NET 10 supports RID-specific tool packages (`ToolPackageRuntimeIdentifiers`). It only saves JIT at startup (SampleShop runs are ~7-9 s, mostly load + JIT).
- Trade-off (package size, more packages, release workflow) -> council (`claude/docs/council.md`) if it is not clear-cut.

## Steps

- [ ] 1. Measure: portable vs ReadyToRun build of the tool on SampleShop and eShop (start-up and total)
- [ ] 2. If the gain is real: proposal for packaging + release workflow, council if needed
- [ ] 3. Implement, pack, install-test on Windows and Linux (CI), push, CI green

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `Unshackle.Interface.csproj`, release workflow |
| **How**   | Packaging |
| **Why**   | Faster start-up for every run |

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from
