---
kingmadoc: 1
feature: "xunit3-async-lifetime"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/XunitV3/Patterns/AsyncLifetimePattern.cs
    - src/Unshackle.Core/Fixes/XunitV3/XunitV3Fix.cs
    - src/Unshackle.Tests/Proofs/XunitV3/AsyncLifetime/
---

# Feature: xunit3.async-lifetime: IAsyncLifetime Task InitializeAsync/DisposeAsync → ValueTask

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-341 (first xunit3 slice) and the scout report `docs/projectinfo/architectuur/candidate-testfw.md`.

## One-sentence summary

`Task InitializeAsync()`/`Task DisposeAsync()` that implement v2 `Xunit.IAsyncLifetime` (directly, explicitly or as an override of an implementation) return `ValueTask`; a non-async body returns `default` for `Task.CompletedTask` and `new ValueTask(task)` for any other task.

## Scope (in / out)

**In scope**: implicit and explicit implementations, overrides of an implementing method, `async` and non-async bodies (block or expression).

**Out of scope**: classes that also implement `IDisposable` or `IAsyncDisposable` (Manual `DualDisposal`: v3 `IAsyncLifetime` extends `IAsyncDisposable` and xUnit then calls only `DisposeAsync`); methods used as a `Task` anywhere in the compilation other than directly awaited or one direct use (Manual `LifetimeResultUsed`); an aliased return type (Manual `UnsupportedShape`).

## Requirements

- **REQ-1**: WHEN an `async Task` lifetime method is found THE SYSTEM SHALL change its return type to `ValueTask` (keeping a qualified form), and an explicit `IAsyncLifetime.DisposeAsync` to `IAsyncDisposable.DisposeAsync` (v3 inherits it from there) (proof `async-methods`, `explicit`).
- **REQ-2**: WHEN a non-async lifetime method returns `Task.CompletedTask` or another task THE SYSTEM SHALL return `default` or `new ValueTask(task)` (proof `returned-task`, `explicit`).
- **REQ-3**: WHEN the class also implements `IDisposable` or `IAsyncDisposable` THE SYSTEM SHALL report Manual `DualDisposal` (proof `disposable`).
- **REQ-4**: WHEN the method (or the interface member) is used other than as `await x.M()` or a direct use (REQ-6) THE SYSTEM SHALL report Manual `LifetimeResultUsed` (proof `result-used`: `.Wait()`, `.ContinueWith`).
- **REQ-6** (council round 18, X6): WHEN a call's result is used once, directly (a variable initializer, the right side of an assignment, a return value or an argument, outside a lifetime method of its own and not returned from a lambda) THE SYSTEM SHALL append `.AsTask()` to the call and keep the declaration Fixed (proof `as-task`).
- **REQ-5**: WHEN a base class implements the interface virtually THE SYSTEM SHALL rewrite the base method and its overrides alike (proof unit test `AsyncLifetime_Find_OverrideOfAnImplementation_Found`).

## Assumptions

- `new ValueTask(task)` awaits the same task; `default(ValueTask)` is a completed ValueTask. The non-awaited scan is cached per `Compilation` (`ConditionalWeakTable`).
- The fix is `AllOrNothing`: one Manual row anywhere leaves the whole solution on v2 (half a migration does not build).

## Models

[xunit3-async-lifetime-modellen.md](xunit3-async-lifetime-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-341-xunit-v3.md`.
