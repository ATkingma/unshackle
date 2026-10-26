---
kingmadoc: 1
feature: "moq-unsupported"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/Moq/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/Moq/MockUseRules.cs
    - src/Unshackle.Tests/Proofs/Moq/Unsupported/
---

# Feature: moq.unsupported: what has no proven NSubstitute form is Manual with its own reason code

|               |                                    |
| ------------- | ---------------------------------- |
| **Project**   | Unshackle                          |
| **Status**    | Approved                           |
| **Generated** | 2026-10-22 by KingmaDoc (agent L2) |

> From `claude/tasks/moq-queue.md` row 13; council round 8 (M1 strict, M3 protected, M5 AutoMocker).

## One-sentence summary

`Protected()`, `As<T>()`, `Invocations`, `Reset()`, `Raise(..)`, strict behaviour and `AutoMocker` are not rewritten; each is reported Manual with
a code of its own instead of the generic `MockUsedBeyondObject`.

## Scope (in / out)

**In**: a use of a mock (local or private field) through one of those members; `new AutoMocker()` (`Moq.AutoMock`).

**Out**: `Reset` to `ClearReceivedCalls` and `Raise` to `Raise.EventWith`: not proven the same (Reset also clears setups), so they stay Manual.
`It.Ref<T>` stays under `RefOutOrParams` (ref parameters).

## Requirements

- **REQ-1**: WHEN a mock is used through `Protected`, `As`, `Invocations`, `Reset`, `Raise`/`RaiseAsync` THE SYSTEM SHALL leave the mock and report `ProtectedMember`, `MockAs`, `MockInvocations`, `MockReset`, `MockRaise` (proofs `Create/unsupported-*`, `ClassMocks/unsupported-protected`).
- **REQ-2**: WHEN the mock is a class mock the specific code SHALL win over `ClassMockSetup` (proof `ClassMocks/unsupported-protected`).
- **REQ-3**: WHEN `new AutoMocker()` is found THE SYSTEM SHALL report `AutoMocker` (proof `Unsupported/auto-mocker`); `MockBehavior.Strict` keeps `StrictBehaviour` (proof `Create/strict`).

## Assumptions

- A mock that stays Moq keeps its `using Moq`, so the file still compiles after the fix.

## Risks

- The fall-through code `MockUsedBeyondObject` still covers every other unknown member.

## Models

[moq-unsupported-modellen.md](moq-unsupported-modellen.md).

## Open questions

- None.
