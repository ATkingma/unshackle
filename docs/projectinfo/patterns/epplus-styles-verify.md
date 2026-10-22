# Verification: epplus-styles

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-styles-plan.md`](epplus-styles-plan.md)         |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of branch `worktree-agent-a516572e1f4f2f6d6` (EPPlus queue row 8).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `StylesHarnessTests.StyleShape_RewrittenByPattern_WritesSameWorkbook` (15 cases, 0 differences) |
| REQ-2 | Met     | same test: the wrong-style variant of every case has differences |
| REQ-3 | Met     | proofs `Styles/underline`, `Styles/border-around`, `Styles/alignment-variable` |
| REQ-4 | Met     | proof `Styles/fill-without-color`; `StylesHarnessTests.FillPatternWithoutColor_IsManual` |
| REQ-5 | Met     | `StylesUnsupportedTests.Rewrite_FixedStatement_CarriesMigratedAnnotation`, `Fix_StyleStatementWithEPPlusReceiver_StaysManualAllOrNothing` |
| REQ-6 | Met     | `ProofTests` for `EPPlus/Unsupported/*` green |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all green (see the agent report).
- No corpus run yet.

## Deviations

- Fixed shapes have no `Proofs/EPPlus/Styles/*.after.cs`: with the receiver still on EPPlus (rows 3–4 open), a file rewritten by this
  pattern alone does not compile on either library, so `ProofTests` cannot check it. The harness test proves the rewritten statement
  with a hand-translated scaffold instead; the proofs folder holds the Manual shapes.
- `epplus.unsupported` skips the style tail (from `.Style` on) of every statement that starts from an `ExcelStyle`
  (`StylesMapping.StyleOf`): StylesPattern rewrote or reported it. Only the receiver is still listed
  (`StylesUnsupportedTests.Fix_MigratedStyleStatement_UnsupportedNamesOnlyTheReceiver`). The `MigratedAnnotationKind` annotation turned out
  not to survive the workspace between patterns, so the skip does not use it; the annotation stays on the rewritten statements.
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-styles-plan.md), [modellen](epplus-styles-modellen.md)
