---
kingmadoc: 1
feature: "moq-callback"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
  - src/Unshackle.Core/Fixes/Moq/Patterns/CallbackPattern.cs
  - src/Unshackle.Core/Fixes/Moq/SetupReturnsSyntax.cs
  - src/Unshackle.Core/Fixes/Moq/SetupChain.cs
  - src/Unshackle.Core/Fixes/Moq/CallLambda.cs
  - src/Unshackle.Tests/Proofs/Moq/Callback/
---
# Feature: moq.callback: `Setup(..).Callback(f)` becomes `AndDoes(ci => ..)` or `When(..).Do(ci => ..)`

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton) |

> Drawn up from `claude/tasks/moq-queue.md` row 8; council rounds 8 (general rule), 9 (Q2 run-time proofs) and 12 (M6: SetupsOverlap stays Manual).

## One-sentence summary

A Setup with a `Callback(lambda)` and a plain `Returns(value)` (before or after it) becomes `m.M(..).Returns(value).AndDoes(ci => ..)`.
A Callback without a value becomes `m.When(x => x.M(..)).Do(ci => ..)`. Lambda parameters become `ci.ArgAt<T>(i)` (`CallLambda`).

## Scope (in / out)

**In scope**

- `Setup(..).Callback(f).Returns(v)`, `Setup(..).Returns(v).Callback(f)`, `Setup(..).Callback(f)`, each optionally `.Verifiable()`; methods only.

**Out of scope (Manual)**

- Callback with a Returns function (`CallbackOrder`): NSubstitute runs `AndDoes` before the function, Moq after it, in either source order. The run-time harness proved this.
- Callback with Throws (`CallbackOrder`).
- Callback on a member with another setup (`SetupsOverlap`): NSubstitute runs an `AndDoes`/`Do` for every matching call, including calls a later setup answers. The run-time harness proved this too.
- Callback with a method group, or on a property (`CallbackShape`).

## Requirements

- **REQ-1**: WHEN `Returns(v).Callback(f)` THE SYSTEM SHALL write `m.Object.M(..).Returns(v).AndDoes(ci => f')` (proof `returns-then-callback`).
- **REQ-2**: WHEN `Callback(f).Returns(v)` with a plain value THE SYSTEM SHALL write the same (proof `callback-then-returns`).
- **REQ-3**: WHEN `Callback(f)` has no value THE SYSTEM SHALL write `m.Object.When(x => x.M(..)).Do(ci => f')`, leaving the answer to Moq's (pinned) default (proof `void-callback`; run-time `callback-only-value`).
- **REQ-4**: IF a Returns function or Throws is in the chain THEN Manual `CallbackOrder` (proofs `callback-before-returns-function`, `returns-function-then-callback`, `callback-with-throws`).
- **REQ-5**: IF the member has another setup THEN Manual `SetupsOverlap` (proof `overlapping-setups`).
- **REQ-6**: IF the callback is a method group THEN Manual `CallbackShape` (proof `callback-method-group`).
- **REQ-7**: The whole fix SHALL run the same callbacks with the same arguments (run-time `callback-pass`, `callback-only-value`; `callback-must-fail` throws under both).

## Assumptions

- `When(..)` is a specification, not a call, so it does not run earlier configurations and needs no `Configure()`.

## Risks

- None known beyond the Manual cases.

## Models

[moq-callback-modellen.md](moq-callback-modellen.md).

## Open questions

- None.
