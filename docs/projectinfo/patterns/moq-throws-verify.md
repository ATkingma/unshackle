# Verification: moq-throws

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-throws-plan.md`](moq-throws-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton)              |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/Moq/Throws/` (`ProofTests`) and `src/Unshackle.Tests/Proofs/Moq/_Runtime/throws-*.cs` (`MoqRuntimeProofTests`).

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `throws-value.*` |
| REQ-2 | Met | `throws-generic.*` |
| REQ-3 | Met | `throws-async.*`; run-time `throws-pass` (faulted task) |
| REQ-4 | Met | `throws-void.*` |
| REQ-5 | Met | `throws-void-call.manual.txt` |
| REQ-6 | Met | `_Runtime/throws-pass`, `throws-must-fail` |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| - | - | - | none | - |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed (counts in the TAAK-307 progress log) |
