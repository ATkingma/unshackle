# Verification: moq-setup-returns

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-setup-returns-plan.md`](moq-setup-returns-plan.md) |
| **Status**    | Deviations found                                          |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton)              |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/Moq/SetupReturns/` (`ProofTests`, this pattern alone, so `m.Object` stays) and
`src/Unshackle.Tests/Proofs/Moq/_Runtime/setup-*.cs` (`MoqRuntimeProofTests`, the whole fix, Moq vs NSubstitute at run time).

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `property.*` |
| REQ-2 | Met | `any-argument.*`; `ArgMatchersTests` |
| REQ-3 | Met | `mixed-arguments.*`, `literal-arguments.*` |
| REQ-4 | Met | `returns-async.*` |
| REQ-5 | Met | `returns-method-group.manual.txt` |
| REQ-6 | Met | `strict.manual.txt`, `other-use-blocks.manual.txt` |
| REQ-7 | Met | `_Runtime/setup-returns`, `setup-override`, `setup-field`, `setup-must-fail` |
| REQ-8 | Met | `_Runtime/setup-returns` (`Count` exempt, `Find` pinned) |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Scope | Queue row 2 as its own pattern `moq.arg-matchers` | Matchers have no pattern of their own: they only exist inside a Setup/Verify lambda, so `ArgMatchers` is a helper of rows 3-4, unit-tested in `ArgMatchersTests` and run-time proven through them | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed (counts in the TAAK-307 progress log) |
