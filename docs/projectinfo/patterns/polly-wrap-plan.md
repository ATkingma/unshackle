---
kingmadoc: 1
feature: "polly-wrap"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/WrapPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyWrap.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Tests/Proofs/Polly/Wrap/
---

# Feature: polly.wrap: WrapAsync of retry policies → one builder, outermost first

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308, queue row 10 (`claude/tasks/polly-queue.md`); council round 8, P4.

## One-sentence summary

polly.wrap: `Policy.WrapAsync(a, b, c)` or `a.WrapAsync(b)` whose operands are all retry calls, written in place or in locals used
only there, becomes `new ResiliencePipelineBuilder(<T>)().AddRetry(a).AddRetry(b)...Build()`, held and executed like a single retry.

## Scope (in / out)

**In scope**: the static and instance forms, nested wraps (flattened, outermost first), the operand forms of
`polly.wait-and-retry-async` and `polly.retry-variants`; the wrap inline on `ExecuteAsync`, in a local or a private field.

**Out of scope (Manual `WrapOrderNotProvable`)**: an operand that is not a retry (timeout, circuit breaker: later rows), a field,
parameter or method result, a local used anywhere else, named or array arguments.

## Requirements

- **REQ-1** (P4): THE SYSTEM SHALL add one `AddRetry` per operand in the v7 order, outermost first (Polly.Core's builder makes the
  first added strategy the outermost).
- **REQ-2** (P4): THE SYSTEM SHALL have a proof per arity, 2 (`two-locals`, instance form, operand locals) and 3 (`three-inline`, static
  form), whose run-time trace is equal before and after and differs when the operands are reversed
  (`WrapProof_OrderReversed_DifferentTrace`).
- **REQ-3**: WHEN an operand is a local whose only use is this wrap THE SYSTEM SHALL inline its options and remove its declaration
  (`Fixed([])`), only when the wrap's own rewrite goes ahead.
- **REQ-4**: WHEN any operand is not provable THE SYSTEM SHALL report Manual `WrapOrderNotProvable` on the wrap's nodes and on every
  operand local (proof `operand-reused`).
- **REQ-5**: THE SYSTEM SHALL type the builder by the operands' result type (`Rewrite_GenericWrap_FixedWithTypedBuilder`).
- **REQ-6**: THE retry patterns SHALL leave wrap operands to this pattern (`Find_WrapOperands_LeftToTheWrapPattern`).

## Assumptions

- Each operand's options are proven by the retry patterns' own proofs; this pattern adds only the order.

## Models

[polly-wrap-modellen.md](polly-wrap-modellen.md).

## Open questions

- None.
