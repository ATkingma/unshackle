# Verification: moq-verify

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-verify-plan.md`](moq-verify-plan.md) |
| **Status**    | Deviations found                                          |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton)              |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/Moq/Verify/` (`ProofTests`, this pattern alone) and `src/Unshackle.Tests/Proofs/Moq/_Runtime/verify-*.cs`
(`MoqRuntimeProofTests`, the whole fix at run time).

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `once.*`, `once-method-group.*` |
| REQ-2 | Met | `never.*` |
| REQ-3 | Met | `exactly.*`, `no-times.*` |
| REQ-4 | Met | `property.*` |
| REQ-5 | Met | `at-least.manual.txt`, `message.manual.txt` |
| REQ-6 | Met | `verify-all-blocks.manual.txt` |
| REQ-7 | Met | `_Runtime/verify-pass`, `verify-with-setup`, `verify-must-fail-count`, `verify-must-fail-never` (VerifyFailed under both) |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Scope | Coordinator: row 4 "council M4" | M4 (`Verifiable`+`Verify()`, `VerifyAll`, `VerifyNoOtherCalls`) is queue row 10; row 4 covers explicit `Verify(lambda, Times)`. Such uses keep the mock Manual | Medium: next row |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed (counts in the TAAK-307 progress log) |
