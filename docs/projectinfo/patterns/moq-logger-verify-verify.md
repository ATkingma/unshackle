# Verification: moq-logger-verify

|               |                                                                                      |
| ------------- | ------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/moq-logger-verify-plan.md`](moq-logger-verify-plan.md)   |
| **Status**    | Verified                                                                             |
| **Generated** | 2026-10-22 by KingmaDoc (agent L2)                                                   |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/Moq/LoggerVerify/` (`ProofTests`) and `_Runtime/logger-pass.cs`, `logger-must-fail.cs` (`MoqRuntimeProofTests`).

| REQ   | Verdict | Evidence                                                    |
| ----- | ------- | ----------------------------------------------------------- |
| REQ-1 | Met     | `never.*`                                                   |
| REQ-2 | Met     | `once.*`                                                    |
| REQ-3 | Met     | `state-cast.manual.txt`                                     |
| REQ-4 | Met     | `_Runtime/logger-pass`, `_Runtime/logger-must-fail`         |

## Deviations

| #   | Area | Plan says | Code does | Impact |
| --- | ---- | --------- | --------- | ------ |
| 1   | -    | -         | -         | none   |

## Validation results

| Check | Command        | Result                       |
| ----- | -------------- | ---------------------------- |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed                       |
