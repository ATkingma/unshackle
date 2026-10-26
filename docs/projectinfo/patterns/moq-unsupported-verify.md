# Verification: moq-unsupported

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-unsupported-plan.md`](moq-unsupported-plan.md)     |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc (agent L2)                                                 |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/Moq/{Create,ClassMocks,Unsupported}/` (`ProofTests`), unit test `UnsupportedPatternTests`.

| REQ   | Verdict | Evidence                                                              |
| ----- | ------- | --------------------------------------------------------------------- |
| REQ-1 | Met     | `Create/unsupported-{as,invocations,reset,raise}`, `ClassMocks/unsupported-protected`, `UnsupportedPatternTests` |
| REQ-2 | Met     | `ClassMocks/unsupported-protected`                                    |
| REQ-3 | Met     | `Unsupported/auto-mocker`, `Create/strict`                            |

## Deviations

| #   | Area           | Plan says                          | Code does | Impact                                              |
| --- | -------------- | ---------------------------------- | --------- | --------------------------------------------------- |
| 1   | Reset, Raise   | queue: rewrite "only if proven"    | Manual    | not proven the same, so nothing is rewritten (safe) |

## Validation results

| Check | Command        | Result                       |
| ----- | -------------- | ---------------------------- |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed                       |
