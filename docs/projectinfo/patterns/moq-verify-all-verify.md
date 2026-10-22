# Verification: moq-verify-all

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-verify-all-plan.md`](moq-verify-all-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton)              |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/Moq/VerifyAll/` (`ProofTests`, this pattern alone) and `src/Unshackle.Tests/Proofs/Moq/_Runtime/verify-all-*`,
`verify-verifiable-*` (`MoqRuntimeProofTests`, the whole fix at run time).

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `verify-all.*` |
| REQ-2 | Met | `verify-verifiable.*` |
| REQ-3 | Met | `VerifyAllPattern` sets `FixedResult.Warning` from `VerifyAllPlan.Warning` |
| REQ-4 | Met | `conditional-setup.manual.txt`, `field-mock.manual.txt` |
| REQ-5 | Met | `overlap.manual.txt` |
| REQ-6 | Met | `no-other-calls.manual.txt`; `Verify/verify-all-blocks.manual.txt` |
| REQ-7 | Met | `_Runtime/verify-all-pass`, `verify-verifiable-pass`, `verify-all-must-fail`, `verify-verifiable-must-fail` |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| - | - | - | none | - |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed: 2010 of 2010 |
