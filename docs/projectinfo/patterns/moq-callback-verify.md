# Verification: moq-callback

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-callback-plan.md`](moq-callback-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton)              |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/Moq/Callback/returns-then-callback.*` |
| REQ-2 | Met | `callback-then-returns.*` |
| REQ-3 | Met | `void-callback.*`; `_Runtime/callback-only-value` |
| REQ-4 | Met | `callback-before-returns-function`, `returns-function-then-callback`, `callback-with-throws` (`.manual.txt`) |
| REQ-5 | Met | `overlapping-setups.manual.txt` |
| REQ-6 | Met | `callback-method-group.manual.txt` |
| REQ-7 | Met | `_Runtime/callback-pass`, `callback-only-value`, `callback-must-fail` |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Scope (found by the harness) | Draft queue: Callback → AndDoes in any combination | A Returns function with a Callback, and a Callback on a member with other setups, are Manual: the run-time harness showed NSubstitute runs AndDoes before the function and for every matching configuration | Medium: fewer Fixed, proven same |
| 2 | Rows 3/6/7 (found here) | Setups rewrite to `m.M(..).Returns(..)` | When a member has another setup, the rewrite goes through `m.Configure().M(..)` (`NSubstitute.Extensions`): an NSubstitute configuration call otherwise runs the earlier matching configuration (its function, AndDoes or Throws). Run-time `setup-overlap-side-effects` | Medium: behaviour fix |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed (counts in the TAAK-307 progress log) |
