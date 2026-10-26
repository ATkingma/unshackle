# Verification: moq-class-mocks

|               |                                                                                |
| ------------- | ------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/moq-class-mocks-plan.md`](moq-class-mocks-plan.md) |
| **Status**    | Verified                                                                       |
| **Generated** | 2026-10-22 by KingmaDoc (agent L2)                                             |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/Moq/ClassMocks/` (`ProofTests`) and `_Runtime/class-callbase-*.cs` (`MoqRuntimeProofTests`).

| REQ   | Verdict | Evidence                                                        |
| ----- | ------- | --------------------------------------------------------------- |
| REQ-1 | Met     | `callbase.*`, `callbase-behavior.*`                             |
| REQ-2 | Met     | `callbase-inline.*`, `callbase-field.*`                         |
| REQ-3 | Met     | `no-callbase.manual.txt`, `abstract.manual.txt`                 |
| REQ-4 | Met     | `setup.manual.txt`                                              |
| REQ-5 | Met     | `other-options`, `mock-of`, `delegate` `.manual.txt`            |
| REQ-6 | Met     | `_Runtime/class-callbase-pass`, `class-callbase-ctor-must-fail` |

## Deviations

| #   | Area | Plan says | Code does | Impact |
| --- | ---- | --------- | --------- | ------ |
| -   | -    | -         | none      | -      |

## Validation results

| Check | Command        | Result                                       |
| ----- | -------------- | -------------------------------------------- |
| Build | `dotnet build` | passed: 0 warnings, 0 errors                 |
| Tests | `dotnet test`  | passed (counts in the TAAK-307 progress log) |
