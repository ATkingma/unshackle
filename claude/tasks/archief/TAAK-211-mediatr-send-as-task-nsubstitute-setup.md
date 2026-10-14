# TAAK-211 — `mediatr.send-as-task` rewrites NSubstitute setups (`mock.Send(...).Returns(...)`) to `.AsTask().Returns(...)`

**Status**: ✅ completed (2026-10-13)
**Created**: 2026-10-13 (from corpus round 3, `claude/tasks/CORPUSRUN-mediatr.md`)
**Goal**: dotnet/eShop `59f1a6c` builds after `fix --fix mediatr` (now 52 Fixed, 0 Manual, but 12× NS1000 in `tests/Ordering.UnitTests`).

## Context for the next agent

- Round 3 (main `9189df4`, libman bypassed with `LibraryRestore=false`): eShop migrates fully (0 Manual, `IdentifiedCommand<T,R>` → `IdentifiedCommandOf…`),
  but the build after the fix breaks silently: 12× `error NS1000: Member AsTask can not be intercepted` (NSubstitute.Analyzers, TWAE).
- Before: `_mediator.Send(Arg.Any<IRequest<bool>>(), default).Returns(Task.FromResult(true));` (`_mediator = Substitute.For<IMediator>()`).
  After: `_mediator.Send(Arg.Any<IRequest<bool>>(), default).AsTask().Returns(Task.FromResult(true));`
  → `AsTask()` is a non-virtual `ValueTask` method; the setup no longer configures anything (NS1000, and without the analyzer a runtime error/silent default).
- Places: `tests/Ordering.UnitTests/Application/IdentifiedCommandHandlerTest.cs` (27, 49) and `OrdersWebApiTest.cs` (28, 43, 58, 74).
  `await _mediator.Received().Send(...)` is rightly left alone.

## New situation (proposal)

- `SendAsTaskPattern` skips a `Send/Publish` call whose result is the receiver of NSubstitute `Returns`/`ReturnsForAnyArgs`/`Throws*`
  (or a Moq `Setup` lambda), and also calls inside `Received()`/`DidNotReceive()`.
- Move the `Returns` argument along: `Task.FromResult(x)` → `new ValueTask<T>(x)` (or `ValueTask.FromResult(x)`), `Task.CompletedTask` → `ValueTask.CompletedTask`;
  an unknown form → Manual with a reason.

## Steps

- [x] 1. Proofs `SendAsTask/nsubstitute-returns` (eShop form, `ReturnsForAnyArgs` with two values and a type argument, qualified `Task`,
  `Publish(...).Returns(Task.CompletedTask)`, `Throws`, `Received().Send`) and `nsubstitute-returns-lambda` (Manual). Unit tests `SendAsTaskPatternTests`.
- [x] 2. `SendAsTaskPattern`: a `Send`/`Publish` that is the receiver of NSubstitute `Returns`/`ReturnsForAnyArgs` → the setup call becomes the finding;
  `Task.FromResult(x)` → `ValueTask.FromResult(x)`, `Task.CompletedTask` → `ValueTask.CompletedTask` (only with a non-generic return, so `Publish`);
  other values → Manual. `Throws*` and a standalone `Received()/DidNotReceive().Send(...)` statement: untouched.
- [x] 3. Docs: send-as-task plan/verify REQ-5/6.
- [x] 4. Proof (`_results/r4/eshop`): 52 Fixed, 0 Manual, **build after fix ok** (was 12× NS1000); unit 32/32 → 32/32, 0 regressions
  (6 functional tests without Docker ran further until the time-out after the fix and are red; before the fix they were not reached).

## Choices

- `ValueTask.FromResult(x)` instead of `new ValueTask<T>(x)`: keeps an explicit type argument and the qualifier, no type name needed.
- `Task.CompletedTask` on a void `Send` → Manual: Mediator gives `ValueTask<Unit>` there.
- Moq (`Setup(m => m.Send(...))`) is out of scope: no corpus occurrence.

## See also

- `src/Unshackle.Core/Fixes/MediatR/Patterns/SendAsTaskPattern.cs`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Repeat round 3"
- `claude/tasks/mediatr-queue.md`
