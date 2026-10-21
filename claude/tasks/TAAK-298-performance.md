# TAAK-298 — Performance of a fix run

**Status**: step 2/7 — baseline done, quick wins in progress  
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
- [ ] 2. Quick wins without behaviour risk: the load check without full `GetDiagnostics()`, assembly names from the original solution or the metadata, ReadyToRun for the tool, cache the compilation per project per run, fewer semantic model fetches in `UsingDirectives`, `Count()` → a counter, one `DescendantNodes()` pass where one tree is walked several times. Measure again.
- [ ] 3. Pre-filter per file: skip the patterns for a file without any old-library trace; tests for global usings, swapped types and the unused-using case. Measure again.
- [ ] 4. Proposal for parallel file processing (council); first split `Fixer.cs` along the `FileFixer` seam proposed in TAAK-297: collect results per file, merge in the original order; thread safety of what is shared.
- [ ] 5. Implement parallel processing behind the merge in the same order; tests that compare findings order with the sequential run.
- [ ] 6. Corpus round (all 36 repos): 36/36 build, 0 regressions, identical findings; final timings next to the baseline.
- [ ] 7. Worktree check → commit → push → CI green; numbers in `claude/docs/overdracht.md`.

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
