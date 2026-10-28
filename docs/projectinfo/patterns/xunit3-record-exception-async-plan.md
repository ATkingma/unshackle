---
kingmadoc: 1
feature: "xunit3-record-exception-async"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/XunitV3/Patterns/RecordExceptionAsyncPattern.cs
    - src/Unshackle.Core/Fixes/XunitV3/XunitV3Fix.cs
    - src/Unshackle.Tests/Proofs/XunitV3/RecordExceptionAsync/
---

# Feature: xunit3.record-exception-async: Record.ExceptionAsync(..) not awaited → .AsTask()

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-341 step 5 and the scout report `docs/projectinfo/architectuur/candidate-testfw.md`.

## One-sentence summary

v3 `Record.ExceptionAsync` returns `ValueTask<Exception>` instead of `Task<Exception>`; a call that is not directly awaited gets `.AsTask()`, which gives back exactly the v2 type.

## Scope (in / out)

**In scope**: calls of v2 `Xunit.Record.ExceptionAsync` whose result is not directly awaited (variable, `var`, return, argument, `.Result`).

**Out of scope**: awaited calls (`await`, `await ..ConfigureAwait(..)`, in parentheses): they build on v3 unchanged; a method group (Manual `UnsupportedShape`).

## Requirements

- **REQ-1**: WHEN a call of `Record.ExceptionAsync` is not directly awaited THE SYSTEM SHALL append `.AsTask()` (proof `as-task`).
- **REQ-2**: WHEN the call is awaited (also with `ConfigureAwait` or parentheses) THE SYSTEM SHALL leave it alone (proof `as-task` (`Awaited_StaysAsItIs`); unit test `RecordExceptionAsync_Find_OwnRecordClassOrParenthesizedAwait_NotFound`).
- **REQ-3**: WHEN `Record.ExceptionAsync` is used as a method group THE SYSTEM SHALL report Manual `UnsupportedShape` (proof `method-group`).
- **REQ-4**: WHEN `ExceptionAsync` belongs to another `Record` class THE SYSTEM SHALL leave it alone (unit test `RecordExceptionAsync_Find_OwnRecordClassOrParenthesizedAwait_NotFound`).
- **REQ-5**: WHEN the not-awaited result is stored and awaited more than once THE SYSTEM SHALL still append `.AsTask()` (Fixed, council 22 X10), and both awaits give the same exception (proof `awaited-twice`).

## Assumptions

- Unlike the lifetime methods (council 18 X6), no declaration changes here, so `.AsTask()` is type-preserving at every use; no "used exactly once" limit is needed.
- Why X10 differs from X6 (council 22): X6 changes a declaration to `ValueTask`, so every caller then holds a ValueTask, which must be awaited at most once; a caller that awaits its stored result twice would break silently. Here `.AsTask()` converts the ValueTask once at the call site and hands the caller exactly v2's `Task<Exception>`, which may be awaited any number of times. So the rewrite is always Fixed, however often the result is used.
- The rewrite does not build on v2 (`Task<T>` has no `AsTask`); the fix is `AllOrNothing`.

## Models

[xunit3-record-exception-async-modellen.md](xunit3-record-exception-async-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-341-xunit-v3.md`.
