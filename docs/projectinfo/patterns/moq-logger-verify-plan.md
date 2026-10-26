---
kingmadoc: 1
feature: "moq-logger-verify"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/Moq/Patterns/LoggerVerifyPattern.cs
    - src/Unshackle.Core/Fixes/Moq/ArgMatchers.cs
    - src/Unshackle.Tests/Proofs/Moq/LoggerVerify/
---

# Feature: moq.logger-verify: `Verify(l => l.Log(.. It.IsAnyType ..))` becomes `Received(n).Log(.. Arg.AnyType ..)`

|               |                                    |
| ------------- | ---------------------------------- |
| **Project**   | Unshackle                          |
| **Status**    | Approved                           |
| **Generated** | 2026-10-22 by KingmaDoc (agent L2) |

> From `claude/tasks/moq-queue.md` row 12.

## One-sentence summary

The Moq way to verify an `ILogger.Log` call (matchers over the generic `TState` via `It.IsAnyType`) becomes the same `Received`/`DidNotReceive`
form as `moq.verify`, with NSubstitute's `Arg.AnyType` in place of `It.IsAnyType`.

## Measurements (Moq 4.18.4 vs NSubstitute 6.2.0)

| Case                                                          | Moq                  | NSubstitute                              |
| ------------------------------------------------------------- | -------------------- | ---------------------------------------- |
| `Log` with level, `Any<EventId>`, `Any`/`Is` over the state   | matches, pass / fail | `Arg.AnyType` matches the same, pass / fail |
| predicate over the state that only calls `ToString()`         | matches              | `Arg.Is<Arg.AnyType>((object v) => ..)` same |
| predicate that casts the state (`IReadOnlyList<..>`)          | matches              | not equal (Manual)                       |
| unconfigured `BeginScope`                                     | `null`               | a substitute (`moq.create` pins `ReturnsNull`) |

## Scope (in / out)

**In**: `Verify` on a mock of `ILogger`/`ILogger<T>` whose lambda is one `Log(..)` call, with `Times.Once/Never/Exactly/AtLeastOnce`.

**Out** (Manual): a state predicate that does anything but `ToString()` on the state (`MatcherUnsupported`); the `Times` ranges and messages of `moq.verify`.

## Requirements

- **REQ-1**: WHEN the matchers are `It.IsAny<It.IsAnyType>()`/`It.IsAny<Func<It.IsAnyType, ..>>()` THE SYSTEM SHALL write `Arg.Any<Arg.AnyType>()`/`Arg.Any<Func<Arg.AnyType, Exception, string>>()` (proof `never`).
- **REQ-2**: WHEN the state predicate only calls `ToString()` THE SYSTEM SHALL write `Arg.Is<Arg.AnyType>((object v) => ..)` (proof `once`).
- **REQ-3**: IF the predicate uses the state in any other way THEN Manual `MatcherUnsupported` (proof `state-cast`).
- **REQ-4**: The rewrite SHALL give the same pass and fail outcome at run time (`_Runtime/logger-pass`, `logger-must-fail`).

## Assumptions

- The formatter argument is always the cast `(Func<It.IsAnyType, Exception, string>)It.IsAny<object>()` or `It.IsAny<Func<..>>()`.

## Risks

- A `Verify` through `LoggerMessage`-generated or custom extension code is not found (no `Log` lambda), it stays untouched.

## Models

[moq-logger-verify-modellen.md](moq-logger-verify-modellen.md).

## Open questions

- None.
