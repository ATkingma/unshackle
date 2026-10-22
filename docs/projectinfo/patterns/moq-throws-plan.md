---
kingmadoc: 1
feature: "moq-throws"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
  - src/Unshackle.Core/Fixes/Moq/Patterns/ThrowsPattern.cs
  - src/Unshackle.Core/Fixes/Moq/SetupReturnsSyntax.cs
  - src/Unshackle.Tests/Proofs/Moq/Throws/
---
# Feature: moq.throws: `m.Setup(..).Throws(ex)` becomes `m.M(..).Throws(ex)`

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton) |

> Drawn up from `claude/tasks/moq-queue.md` row 6; council rounds 8 (general rule) and 9 (Q2 run-time proofs).

## One-sentence summary

A `Setup(..).Throws(..)` / `.ThrowsAsync(..)` statement on a mock that converts as a whole becomes NSubstitute's
`ExceptionExtensions.Throws`/`ThrowsAsync`; on a void member it becomes `When(..).Do(_ => throw ex)`.

## Scope (in / out)

**In scope**

- `.Throws(exception)`, `.Throws<E>()`, `.ThrowsAsync(exception)` (Task/ValueTask members), each optionally followed by `.Verifiable()`.
- Void members: `.Throws(variable | field | new E(..))`, `.Throws<E>()`.

**Out of scope**

- `Throws` with a function, and on a void member an exception expression that may differ per call (a method call): Manual `ThrowsShape`.
- `ThrowsAsync` on a void member (Manual `ThrowsShape`).

## Requirements

- **REQ-1**: WHEN `.Throws(ex)` on a member with a value THE SYSTEM SHALL write `m.Object.M(..).Throws(ex);` and add `using NSubstitute.ExceptionExtensions;` (proof `throws-value`).
- **REQ-2**: WHEN `.Throws<E>()` THE SYSTEM SHALL write `.Throws<E>()` (proof `throws-generic`).
- **REQ-3**: WHEN `.ThrowsAsync(ex)` THE SYSTEM SHALL write `.ThrowsAsync(ex)`: a faulted task, not a synchronous throw (proof `throws-async`).
- **REQ-4**: WHEN the member is void THE SYSTEM SHALL write `m.Object.When(x => x.M(..)).Do(_ => throw ex);` (proof `throws-void`).
- **REQ-5**: IF the exception is a function, or on a void member an expression evaluated per call THEN Manual `ThrowsShape` (proof `throws-void-call`).
- **REQ-6**: The whole fix SHALL throw the same exception types, messages and instances where Moq does (run-time `throws-pass`: value, generic, shared instance, void, async, unconfigured; `throws-must-fail` throws under both).

## Assumptions

- On a void member the `new E(..)` in `Do` makes a fresh instance per call, where Moq throws one instance; the test cannot hold a reference to an inline `new`, so only reference identity differs, and that is not observable.

## Risks

- None known beyond the assumption above.

## Models

[moq-throws-modellen.md](moq-throws-modellen.md).

## Open questions

- None.
