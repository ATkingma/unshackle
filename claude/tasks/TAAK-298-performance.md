# TAAK-298 — Performance of a fix run

**Status**: step 3/7 — steps 1 and 2 done (on the TAAK-298 agent branch, not pushed); next: step 3, pre-filter per file  
**Created**: 2026-10-20  
**Goal**: a measurably faster `scan --fix` on large solutions, with identical output (same findings in the same order, same rewritten files) and 0 corpus regressions.

## Context for the next agent

- Audit on 2026-10-20 (main `91d1216`), found by reading the code; nothing measured yet. Measure first, optimise afterwards.
- Hotspots found:
  - `Fixer.cs:243`: files are processed one by one, although every file is fixed against a fixed baseline (TAAK-130), so they are independent. Parallel is the biggest possible win and the biggest risk (order of `findings`, shared `List`/`HashSet`, the Roslyn `Solution` that is built up).
  - No pre-filter: every file goes through every pattern (`FixFileAsync`, `Fixer.cs:425`), also files without any trace of the old library. A cheap syntactic check (namespaces/identifiers/swapped types) must still respect global usings and `UnboundCalls`/`HasUnusedRemovedUsingAsync`.
  - Repeated work: `GetCompilationAsync` per project in `UnreachableFilesAsync`, `ProjectPackages`, `VendoredPackages`, `SwapGlobalUsingsAsync`; `UsingDirectives` asks for a semantic model 13 times; `NewCode` re-parses the file text per earlier pass.
  - Small: `group.Count()` in `UsingDirectives.cs:379`, repeated `DescendantNodes()` over the same tree (up to 9× in `ConfigurationSyntax.cs`).
- IL / metadata level (added 2026-10-20 after the user's question):
  - `MsBuildSolutionStore.cs` (~line 118) calls `compilation.GetDiagnostics()` for **every project at load**: that binds every method body in the solution, and the patterns later bind again with their own semantic models. Probably the largest single cost. Option: only check the using directives/declarations (`GetDeclarationDiagnostics`, or binding only the `using` names), measure whether the same projects are flagged.
  - `ProjectPackages`, `VendoredPackages` and `UnreachableFilesAsync` build a compilation only for `ReferencedAssemblyNames`. The references do not change during a fix: read them from the original solution (cached compilation) or straight from the metadata (`Project.MetadataReferences` → `PortableExecutableReference.FilePath` → `System.Reflection.Metadata` `AssemblyName`), without compiling the modified solution again.
  - Startup: publish the tool with ReadyToRun (`PublishReadyToRun`) so less is JIT-compiled per run; check the NuGet tool package size. Native AOT is not possible (MSBuildWorkspace, reflection in Roslyn).
  - Not useful: rewriting via IL (Mono.Cecil etc.). Unshackle rewrites source code; it needs the Roslyn syntax trees and semantic models, so IL cannot replace them.
- Output must not change: the tests pin the rendered text, and the corpus is the proof (harness `claude/tools/corpus-am.sh`, blind set `claude/tasks/CORPUSRUN-blind.md`).
- A design choice with a trade-off (e.g. parallelism level, cache lifetime) → council (`claude/docs/council.md`).

## Steps

- [x] 1. Baseline: time `scan --fix` (fa, mediatr, automapper) on 3 large corpus repos + SampleShop; record wall time and, if possible, a trace (`dotnet-trace`/`dotnet-counters`) of where the time goes. Numbers in this task.
- [x] 2. Quick wins without behaviour risk: the load check without full `GetDiagnostics()`, assembly names from the original solution or the metadata, ReadyToRun for the tool, cache the compilation per project per run, fewer semantic model fetches in `UsingDirectives`, `Count()` → a counter, one `DescendantNodes()` pass where one tree is walked several times. Measure again.
- [ ] 3. Pre-filter per file: skip the patterns for a file without any old-library trace; tests for global usings, swapped types and the unused-using case. Measure again.
- [ ] 4. Proposal for parallel file processing (council); `Fixer.cs` is split along the `FileFixer` seam (done, see `claude/tasks/archief/TAAK-299-split-large-files.md` for the shared state left in it): collect results per file, merge in the original order; thread safety of what is shared.
- [ ] 5. Implement parallel processing behind the merge in the same order; tests that compare findings order with the sequential run.
- [ ] 6. Corpus round (all 36 repos): 36/36 build, 0 regressions, identical findings; final timings next to the baseline.
- [ ] 7. Worktree check → commit → push → CI green; numbers in `claude/docs/overdracht.md`.

## Measurements

Wall time of `unshackle scan <sln> --fix <lib>` (Release build, run as `dotnet bin/Release/net10.0/unshackle.dll`, so no `dotnet run` build overhead), Windows dev machine, median of 3 runs in seconds. Scan writes no code, so the repos stay untouched (only restored: `obj/`). Another agent may have been building at the same time: treat differences under ~10% as noise. No `dotnet-trace` installed, so no trace.

| Repo | Lib | Before (main `3db6f94`) | After step 2 |
| --- | --- | --- | --- |
| SampleShop | fluentassertions | 10.7 (10.0/10.7/11.6) | 8.9 (9.0/8.9/8.2) |
| SampleShop | mediatr | 9.7 (9.7/8.5/10.1) | 8.6 (8.4/8.6/8.8) |
| SampleShop | automapper | 8.2 (8.2/8.2/7.1) | 6.5 (6.6/6.5/6.3) |
| `<corpus>/corpus-mediatr/src/eshop` `eShop.sln` | mediatr | 35.0 (38.4/35.0/31.7) | 28.8 (28.8/27.1/29.8) |
| `<corpus>/corpus-automapper/src/nop` `src/NopCommerce.sln` | automapper | 86.5 (86.5/86.5/89.2) | 81.5 (80.0/82.4/81.5) |
| `<corpus>/corpus-fa/src/akka` `Akka.slnx` | fluentassertions | 547.2 (601.5/547.2/537.2) | 506.6 (515.4/502.7/506.6) |

Akka is the outlier: 7043 fixed, 4366 manual, ~9 minutes.

## Progress log

- 2026-10-21: step 1 done. Baseline above (eshop and akka restored first with `dotnet restore`).
- 2026-10-21: step 2 done. Kept: (a) `MsBuildSolutionStore.LoadAsync` builds the compilations and computes the CS0246/CS0234 diagnostics of all projects in parallel (`Task.WhenAll`), then evaluates them in solution order, so the same projects are flagged/left out with the same messages; (b) `ProjectPackages.PlanAsync` reads `ReferencedAssemblyNames` from the original solution's compilation (built at load) instead of the modified solution's. Scan output byte-identical to the baseline on all 6 repo/lib pairs; 1648 tests and the sample tests green. Gain 6–20%; akka ~7%.
- Step 2 items not done, with reason:
  - Load check with `GetDeclarationDiagnostics` instead of full `GetDiagnostics`: not done. CS0246/CS0234 inside method bodies (an unimported or fully qualified type in a body) are only found by binding bodies, so fewer projects could be flagged: a behaviour change. Parallel binding keeps the check exact.
  - `UnreachableFilesAsync` and `VendoredPackages`: already run on the original solution, whose compilations are cached since the load; nothing to win.
  - Fewer semantic model fetches in `UsingDirectives`: `Document.GetSemanticModelAsync` is cached per document by Roslyn; the repeated fetches are on different documents (`updated`, `withMissing`, `candidate`), so they are real work. No change.
  - `group.Count()` in `UsingDirectives.cs`: a `GroupBy` grouping implements `ICollection`, so `Count()` is O(1). No change.
  - ReadyToRun: `PublishReadyToRun` needs a RuntimeIdentifier and the tool is packed portable. It needs RID-specific tool packages (`ToolPackageRuntimeIdentifiers`, .NET 10): a packaging change, and it only saves JIT at startup. Left for later.
  - `ConfigurationSyntax.cs` repeated `DescendantNodes()`: not touched (another agent owned `Fixes/AutoMapper/` at the time). Still open.
- Where the time goes (inferred, no trace): SampleShop is mostly MSBuildWorkspace load and JIT; akka (~500 s, 11k findings) is the per-file fix loop in `Fixer`, so steps 3 (pre-filter) and 5 (parallel files) hold the big gains.

## Changes

| Field     | Content                                                                 |
| --------- | ----------------------------------------------------------------------- |
| **Where** | `Unshackle.Core/Fixer.cs`, `UsingDirectives.cs`, `ProjectPackages.cs`, `VendoredPackages.cs`, `Fixes/AutoMapper/Patterns/ConfigurationSyntax.cs` |
| **How**   | Measure, cache, pre-filter, then parallelise with ordered merge |
| **Why**   | Large solutions take long to fix; repeated work and sequential processing are the cause |

## See also

- `claude/tasks/archief/TAAK-297-code-standards-cleanup.md` — cleanup from the same audit
- `docs/projectinfo/architectuur/core.md` — Fixer → Fix → Pattern
- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/README.md` — architecture overview
