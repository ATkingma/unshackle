# Verification: specflow-string-step-call

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/specflow-string-step-call-plan.md`](specflow-string-step-call-plan.md) |
| **Status**    | Verified                                                          |
| **Generated** | 2026-10-27 by KingmaDoc                                           |

> Verified by an AI agent (TAAK-340 first slice) against the code on branch `taak-340`.

## Requirements

Code: `src/Unshackle.Core/Fixes/SpecFlow/Patterns/StringStepCallPattern.cs`, registration in `SpecFlowFix.cs`.
Proofs: `src/Unshackle.Tests/Proofs/SpecFlow/StringStepCall/` (`ProofTests`; an after proof runs the whole fix, `ProofFix.AfterRunsWholeFix`, and compiles against Reqnroll 3.3.4 only).

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | steps-base, SpecFlowFixTests.FixAsync_StringStepCallInOneFile_LeavesTheWholeSolutionOnSpecFlow |

## Deviations

None.

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed |
| Tests | `dotnet test` | all green |

## Council round 18 (S1, S4)

The reason names `Reqnroll.SpecFlowCompatibility` as the other hand route. When the first argument is a constant and exactly one step
definition of the compilation (`[Given]`/`[When]`/`[Then]`/`[StepDefinition]`; `And`/`But` match every kind) matches the whole text (regex
anchored at both ends, 100 ms timeout, an invalid regex never matches), the reason ends with `Possible match: Type.Method.` (a hint, no
rewrite). Proof: `StringStepCall/possible-match`.

## See also

- [specflow-string-step-call-plan.md](specflow-string-step-call-plan.md)
- [specflow-string-step-call-modellen.md](specflow-string-step-call-modellen.md)
