# Verification: moq-returns-lambda

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-returns-lambda-plan.md`](moq-returns-lambda-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton)              |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/Moq/ReturnsLambda/parameter.*`, `two-parameters.*` |
| REQ-2 | Met | `no-parameters.*` |
| REQ-3 | Met | `returns-async.*` |
| REQ-4 | Met | `_Runtime/returns-lambda-block` (parameter named `ci`) |
| REQ-5 | Met | `SetupReturns/returns-method-group.manual.txt`, `changes-parameter.manual.txt`, `async-block.manual.txt` |
| REQ-6 | Met | `_Runtime/returns-lambda-pass`, `returns-lambda-block`, `returns-lambda-must-fail` |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| - | - | - | none | - |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed: 2211 of 2211 |
