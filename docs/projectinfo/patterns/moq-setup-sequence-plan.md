---
kingmadoc: 1
feature: "moq-setup-sequence"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/Moq/Patterns/SetupSequencePattern.cs
    - src/Unshackle.Core/Fixes/Moq/SetupSequenceSyntax.cs
    - src/Unshackle.Tests/Proofs/Moq/SetupSequence/
---

# Feature: moq.setup-sequence: `SetupSequence(..).Returns(a).Returns(b)` becomes `Returns(a, b)`

|               |                                    |
| ------------- | ---------------------------------- |
| **Project**   | Unshackle                          |
| **Status**    | Approved                           |
| **Generated** | 2026-10-22 by KingmaDoc (agent L2) |

> From `claude/tasks/moq-queue.md` row 11; council round 8, M2 (Fixed with a warning) and M6 (`SetupsOverlap` stays Manual).

## One-sentence summary

A `SetupSequence` whose steps are all `Returns`/`ReturnsAsync` values becomes one NSubstitute `Returns(a, b, ...)`, with a warning that the
behaviour after the last value differs.

## Measurements (Moq 4.18.4 vs NSubstitute 6.2.0)

| Case                                                 | Moq                                                                | NSubstitute                                   |
| ---------------------------------------------------- | ------------------------------------------------------------------ | --------------------------------------------- |
| values inside the sequence (method, property, async) | in order                                                           | in order (same)                               |
| after the last value, `int` / `string` / `Task<int>` | `0` / `null` / a null task (`NullReferenceException` on `.Result`) | repeats the last value (differs: warning, M2) |
| a later sequence on the same member                  | replaces the first                                                 | replaces the first (same)                     |
| `Throws` step                                        | throws at its turn                                                 | needs a function step (Manual)                |

## Scope (in / out)

**In**: `m.SetupSequence(x => x.M(..))` or `x => x.P` followed by one or more `Returns(v)` / `ReturnsAsync(v)`, as a statement.

**Out** (Manual): `Throws`/`ThrowsAsync`/`Pass`/`CallBase` steps, void members, function steps, `null` steps (`SequenceStep`); another setup of the
same member (`SetupsOverlap`).

## Requirements

- **REQ-1**: WHEN all steps are `Returns` THE SYSTEM SHALL write `m.Object.M(..).Returns(a, b, ..)` (proofs `returns`, `single`, `property`).
- **REQ-2**: WHEN a step is `ReturnsAsync(v)` THE SYSTEM SHALL write `Task.FromResult<R>(v)` / `new ValueTask<R>(v)` (proof `returns-async`).
- **REQ-3**: Every Fixed result SHALL carry the warning about the behaviour after the last value (`SetupSequenceSyntax.Warning`).
- **REQ-4**: IF a step is not a plain value, or the member is void THEN Manual `SequenceStep` (proofs `throws-step`, `void-member`, `function-step`).
- **REQ-5**: IF the member has another setup THEN Manual `SetupsOverlap` (proof `overlap`); the run-time proof `_Runtime/sequence-pass` gives the same answers inside the sequence.

## Assumptions

- Tests do not rely on the answer after the last value (the warning tells the user to check).

## Risks

- A test that does read past the sequence changes behaviour silently apart from the warning (council question in the L2 report: append a default step to make it equal?).

## Models

[moq-setup-sequence-modellen.md](moq-setup-sequence-modellen.md).

## Open questions

- Pin Moq's exhaustion default with a final step (`Returns(a, b, default)`)? Left to the council.
