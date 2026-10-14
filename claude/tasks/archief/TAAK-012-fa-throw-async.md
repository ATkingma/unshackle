# TAAK-012 — Pattern fa.throw-async

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `await act.Should().ThrowAsync<T>()` (+ `.WithMessage`) becomes `await Should.ThrowAsync<T>(act)` (+ message check), with proofs.

## Context

Row 7. Builds on TAAK-008 (`fa.throw`). Sample: `OrderHandlerTests.cs:47`.

## Analysis

FA 7.2.2: `ThrowAsync<T>` on `AsyncFunctionAssertions` (subject `Func<Task>` / `Func<Task<T>>`; `Func<ValueTask>` has no `Should()`), returns `Task<ExceptionAssertions<T>>`; `WithMessage` is an extension method on that task. Shouldly 4.3.0: `Should.ThrowAsync<T>(Func<Task>|Task, string customMessage)`; no `Func<Task<T>>` overload, but `Func<Task<ref>>` converts via covariance. Derived exceptions are accepted (like `Throw`).

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | Find returns the surrounding `await` (or the call itself without await) | With a message check the await must be parenthesized: `(await Should.ThrowAsync<T>(act)).Message...` |
| 2 | Only `await` as a standalone statement is Fixed; without await, `.Where`, `var x = await` → Manual ("not directly awaited" / "is used") | No unambiguous form |
| 3 | `ThrowExactlyAsync` → Manual | Shouldly accepts derived types |
| 4 | `Func<Task>` and `Func<Task<refType>>` Fixed; `Func<Task<valueType>>` → Manual ("reference type") | No implicit conversion to `Func<Task>` |
| 5 | `Awaiting(...)` subject → Manual ("Invoking") | As with Throw |
| 6 | Wildcard translation and argument formatting shared in `ThrowMessage` (also used by `ThrowPattern`) | Avoids duplication; Throw proofs stay green |
| 7 | `TryGetArguments` skips arguments without `ArgumentSyntax` (receiver of an extension method) | Otherwise InvalidCastException on `WithMessage` on a task |

## Steps

- [x] Claim, research, plan (13 REQ), models, pattern + registration, 18 proofs, verify, build/test green

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs`, `ThrowMessage.cs` (new), `ThrowPattern.cs` (uses helper), `FluentAssertionsSyntax.cs` (skip receiver), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/`
- `docs/projectinfo/patterns/fa-throw-async-{plan,modellen,verify}.md` + `img/`, `docs/projectinfo/README.md`

## See also

- `claude/tasks/archief/TAAK-008-fa-throw.md`
- `claude/tasks/patroon-queue.md`
