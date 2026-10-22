# Verification: moq-properties

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-properties-plan.md`](moq-properties-plan.md) |
| **Status**    | Deviations found                                          |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton)              |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/Moq/SetupReturns/setup-get.*` |
| REQ-2 | Met | `Proofs/Moq/Verify/verify-get.*` |
| REQ-3 | Met | `Proofs/Moq/Verify/verify-set.*` |
| REQ-4 | Met | `Proofs/Moq/Create/setup-all-properties.*` |
| REQ-5 | Met | `Proofs/Moq/Create/setup-property-initial.*` |
| REQ-6 | Met | `Create/setup-set.manual.txt`, `SetupReturns/property-order.manual.txt`, `Create/setter.manual.txt` |
| REQ-7 | Met | `_Runtime/props-all-properties`, `props-setup-property`, `props-setup-property-default`, `props-must-fail` |
| REQ-8 | Met | `CreatePattern.RewriteTracking`; `MockUseRules` accepts tracking statements in both modes |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Scope | Queue row 5 as its own pattern `moq.properties` | No pattern class of its own: findings carry the ids `moq.setup-returns`, `moq.verify` and `moq.create` | Low: report grouping |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed (counts in the TAAK-307 progress log) |
