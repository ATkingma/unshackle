---
kingmadoc: 1
feature: "moq-verify"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
  - src/Unshackle.Core/Fixes/Moq/Patterns/VerifyPattern.cs
  - src/Unshackle.Core/Fixes/Moq/VerifySyntax.cs
  - src/Unshackle.Tests/Proofs/Moq/Verify/
---
# Feature: moq.verify: `m.Verify(x => x.M(..), Times.X)` becomes `m.Received(n).M(..)`

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton) |

> Drawn up from `claude/tasks/moq-queue.md` row 4 and council rounds 8 (general rule) and 9 (Q2 run-time proofs).

## One-sentence summary

A `Verify(lambda[, Times])` statement on a mock that converts as a whole becomes an exact NSubstitute `Received` check on
`m.Object` (which `moq.create` then drops), with arguments rewritten by `ArgMatchers`.

## Scope (in / out)

**In scope**

- `Times.Once()` / `Times.Once` → `Received(1)`, `Times.Never` → `DidNotReceive()`, `Times.Exactly(n)` → `Received(n)`, `Times.AtLeastOnce` or no `Times` → `Received()`.
- Methods (`m.Object.Received(1).M(..);`) and properties (`_ = m.Object.Received(1).P;`).

**Out of scope**

- `Times.AtLeast/AtMost/Between` (Manual `TimesRange`: no exact NSubstitute form), a failure message (Manual `VerifyMessage`).
- Council M4: `Verifiable()` + `Verify()`, `VerifyAll`, `VerifyNoOtherCalls` (queue row 10); `VerifyGet`/`VerifySet` (row 5). Such a use keeps the whole mock on Moq.

## Requirements

- **REQ-1**: WHEN `Times.Once()` or `Times.Once` THE SYSTEM SHALL write `Received(1)` (proofs `once`, `once-method-group`).
- **REQ-2**: WHEN `Times.Never` THE SYSTEM SHALL write `DidNotReceive()` (proof `never`).
- **REQ-3**: WHEN `Times.Exactly(n)` THE SYSTEM SHALL write `Received(n)`; WHEN no `Times` or `AtLeastOnce` THE SYSTEM SHALL write `Received()` (proofs `exactly`, `no-times`).
- **REQ-4**: WHEN a property is verified THE SYSTEM SHALL write `_ = m.Object.Received(n).P;` (proof `property`).
- **REQ-5**: IF `Times` is a range or a message is passed THEN Manual `TimesRange` / `VerifyMessage` (proofs `at-least`, `message`).
- **REQ-6**: IF another use keeps the mock on Moq (for example `VerifyNoOtherCalls`) THEN Manual with the mock's reason (proof `verify-all-blocks`).
- **REQ-7**: The whole fix SHALL pass and fail where Moq does (run-time `verify-pass`, `verify-with-setup`; `verify-must-fail-count` and `verify-must-fail-never` fail under both).

## Assumptions

- NSubstitute's `Received(n)` checks exactly n matching calls and `Received()` at least one, like Moq's `Exactly(n)` and `AtLeastOnce`; configuration calls (`Returns`) are not counted by either.

## Risks

- A failed check throws `ReceivedCallsException` instead of `MockException`: code that catches `MockException` keeps Moq in use.

## Models

[moq-verify-modellen.md](moq-verify-modellen.md).

## Open questions

- None.
