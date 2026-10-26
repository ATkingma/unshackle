# TAAK-305 — ReadyToRun tool packages

**Status**: step 1/3 done (measured); steps 2-3 wait for a council answer
**Created**: 2026-10-22  
**Goal**: Decide, with measurements, whether RID-specific ReadyToRun tool packages are worth it, and ship them if so.

## Context for the next agent

- From TAAK-298 step 2: `PublishReadyToRun` needs a RuntimeIdentifier; the tool is packed portable. .NET 10 supports RID-specific tool packages (`ToolPackageRuntimeIdentifiers`). It only saves JIT at startup (SampleShop runs are ~7-9 s, mostly load + JIT).
- Trade-off (package size, more packages, release workflow) -> council (`claude/docs/council.md`) if it is not clear-cut.

## Steps

- [ ] 1. Measure: portable vs ReadyToRun build of the tool on SampleShop and eShop (start-up and total)
- [ ] 2. If the gain is real: proposal for packaging + release workflow, council if needed
- [ ] 3. Implement, pack, install-test on Windows and Linux (CI), push, CI green

## Progress log

- 2026-10-22 (L3, step 1 measured; linux-x64, .NET 10.0.113; ms):
    - `--help` start-up: portable 116-143, ReadyToRun 59-86 (about -55 ms).
    - `scan SampleShop.slnx --fix fluentassertions`: portable 4581 / 5074, ReadyToRun 3726 / 3353 (about -1.2 s, -25 to -33 %).
    - Large corpus repo: not measurable. `corpus-mediatr/src/eshop` needs an SDK that is not installed; `eshoponweb` is not restored ("Types do not resolve"); the 4.1 s vs 3.3 s there is load + JIT on a failed load.
    - Size: publish folder 31 MB -> 62 MB; zipped (nupkg proxy) 10.8 MB -> 25.3 MB per RID. RID-specific tool packages mean one package per RID plus a pointer package.
    - Decision: not implemented. Gain is real but small in absolute terms (about 1 s per run); cost is 2.3x package size per RID and a RID matrix in the release workflow. Council question: is about 1 s per run worth that? Status stays step 1 done, steps 2-3 wait for the answer.

## Changes

| Field     | Content                                        |
| --------- | ---------------------------------------------- |
| **Where** | `Unshackle.Interface.csproj`, release workflow |
| **How**   | Packaging                                      |
| **Why**   | Faster start-up for every run                  |

## See also

- `claude/docs/INDEX.md` — documentation index
- `claude/tasks/archief/TAAK-298-performance.md` — where these follow-ups came from

- 2026-10-26: Deferred (council round 13, R4).
