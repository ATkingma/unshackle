---
kingmadoc: 1
feature: "using-removal-edge-cases"
status: verified
plan: using-removal-edge-cases-plan.md
---

# Verify: removing an unused old using without breaking the build

|              |                                                                    |
| ------------ | ------------------------------------------------------------------ |
| **Plan**     | [using-removal-edge-cases-plan.md](using-removal-edge-cases-plan.md) |
| **Verified** | 2026-10-18                                                         |

## Built vs planned

| Planned                                    | Built                                                                                                                                                  |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `UsingDirectives.UpdateAsync` two passes   | as planned (`KeepLeadingTrivia` for usings with directives / disabled text)                                                                            |
| `FluentAssertionsFix.RemovedNamespaces`    | plus Collections, Common, Data, Equivalency, Events, Formatting, Numeric, Specialized, Streams, Types, Xml                                              |
| `FileFixer.Replace` directive guard            | `InnerDirectives`; Manual reason "has #if/#else/#region directives inside it"                                                                          |
| `SolutionChecks.UnreachableFilesAsync`              | as planned; reuses the `encoding` row path with its own text                                                                                           |

## Requirements

| Id    | Evidence                                                                                                                       |
| ----- | ------------------------------------------------------------------------------------------------------------------------------ |
| REQ-1 | proof `_Combined/using-in-conditional`; `FixerTests.FixAsync_UnusedRemovedUsingInConditionalBranch_KeepsDirectivesPaired`      |
| REQ-2 | proof `_Combined/using-sub-namespace`; `FixerTests.FixAsync_UnusedUsingOfRemovedSubNamespace_IsRemoved`                         |
| REQ-3 | existing "one stays, all stay" rule (`UsedRemovedNamespaces`); not given its own proof                                         |
| REQ-4 | proof `_Combined/directives-in-chain`; `FixerTests.FixAsync_FixedNodeHasDirectiveInside_ReportsManualAndKeepsCode`             |
| REQ-5 | corpus only (serilogmem); no unit test (needs a multi-project solution with references)                                       |

`dotnet build` and `dotnet test`: 1564 passed, 0 failed.

## Corpus

Tool = this branch, `after` mode (fix, rebuild, own tests, compare with baseline).

| Repo       | Before (main `db05908`)                | After                         | Tests before → after | Regressions |
| ---------- | -------------------------------------- | ----------------------------- | -------------------- | ----------: |
| serilogmem | build breaks (48x CS1028)              | builds; 17 Fixed / 399 Manual | 570/570 → 570/570    |           0 |
| opentws    | build breaks (CS0246)                  | builds; 118 Fixed / 0 Manual  | 17/56 → 17/56 ¹      |           0 |

¹ 36 baseline tests fail in this environment (IB gateway); same set before and after.

serilogmem is mostly Manual because it is itself a FluentAssertions extension library (`unmatched-use` 378): by design, not migrated.

## See also

- [plan](using-removal-edge-cases-plan.md), [models](using-removal-edge-cases-modellen.md), `claude/tasks/archief/TAAK-281-using-in-conditional.md`, `claude/tasks/archief/TAAK-283-using-sub-namespace.md`
