---
kingmadoc: 1
feature: "xunit3-async-void-test"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/XunitV3/Patterns/AsyncVoidTestPattern.cs
    - src/Unshackle.Core/Fixes/XunitV3/XunitV3Fix.cs
    - src/Unshackle.Tests/Proofs/XunitV3/AsyncVoidTest/
---

# Feature: xunit3.async-void-test: [Fact]/[Theory] async void → async Task

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-341 (first xunit3 slice) and the scout report `docs/projectinfo/architectuur/candidate-testfw.md`.

## One-sentence summary

An `async void` method with `[Fact]`, `[Theory]` or an attribute deriving from v2 `FactAttribute` returns `Task`: v3 fails such tests at run time (xUnit1049).

## Scope (in / out)

**In scope**: `[Fact]`, `[Theory]` and custom attributes deriving from `FactAttribute`.

**Out of scope**: `async void` methods without a test attribute (untouched); a test method the same file also calls or references (Manual `AsyncVoidCalled`).

## Requirements

- **REQ-1**: WHEN an `async void` test method is found THE SYSTEM SHALL change `void` to `Task` (proof `fact`; unit test `AsyncVoidTest_Find_CustomFactAttribute_Found`).
- **REQ-2**: WHEN an `async void` method has no test attribute THE SYSTEM SHALL leave it alone (proof `fact` (`NotATest`)).
- **REQ-3**: WHEN the file calls or references the test method THE SYSTEM SHALL report Manual `AsyncVoidCalled` (proof `called`).

## Assumptions

- The rewrite also compiles on v2, so it is safe on its own; it is part of the xunit3 fix because only v3 rejects `async void`.
- The fix is `AllOrNothing`: one Manual row anywhere leaves the whole solution on v2 (half a migration does not build).

## Models

[xunit3-async-void-test-modellen.md](xunit3-async-void-test-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-341-xunit-v3.md`.
