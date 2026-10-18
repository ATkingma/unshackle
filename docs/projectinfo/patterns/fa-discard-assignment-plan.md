---
kingmadoc: 1
feature: "fa-discard-assignment"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/DiscardAssignmentPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/DiscardAssignment/
---

# Feature: fa.discard-assignment: `_ = x.Should().Be(y);` becomes `x.Should().Be(y);`

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-18 by KingmaDoc |

> Drafted from TAAK-277 (buildenator uses `_ = ...` against the "unused result" analyzer).

## One-sentence summary

A discard assignment of a FluentAssertions call is reduced to the call as a statement, so the assertion patterns (Shouldly returns void) can rewrite it.

## Scope (in / out)

**In scope**

- `_ = x.Should().M(...);` and `_ = await x.Should().MAsync(...);` where the discard is a real discard symbol and the call is FluentAssertions.

**Out of scope**

- A local variable named `_`, discards of other calls or values (left alone).
- The rewrite of the assertion itself (the other `fa.*` patterns).

## Requirements

- **REQ-1**: WHEN a statement is `_ = <FA invocation>;` THE SYSTEM SHALL replace it by the invocation as a statement (proofs `DiscardAssignment/simple.*`).
- **REQ-2**: WHEN the right side is `await <FA invocation>` THE SYSTEM SHALL keep the await (proof `DiscardAssignment/await.*`).
- **REQ-3**: WHEN the discard is followed by assertion patterns THE SYSTEM SHALL let them rewrite the plain statement (proof `_Combined/discard-assignment.*`).

## Assumptions

- A discard has no runtime effect, so pass/fail is unchanged (probed: `_ = x.Should().Be(3)` passes, `Be(4)` fails, both before and after).

## Risks

- A local named `_` is not a discard symbol: unit test `Find_DiscardOfNonAssertionOrRealLocal_FindsNothing`.

## Models

Models: [fa-discard-assignment-modellen.md](fa-discard-assignment-modellen.md).
