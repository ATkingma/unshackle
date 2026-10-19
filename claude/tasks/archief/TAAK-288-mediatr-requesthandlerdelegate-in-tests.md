# TAAK-288 — MediatR: `RequestHandlerDelegate` with a real delegate in tests

**Status**: ✅ done
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- afripay (0/16) and cleanmicro (0/20) stop because tests create `RequestHandlerDelegate<T>` from a real lambda (not a mock) to call a pipeline behavior directly. fullcart keeps 15 handlers on MediatR because of a decorator: confirm whether that is principally manual.
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- A first attempt (branch `worktree-agent-a4ddfa0b990b7a5bb`) was lost with another machine; this is a fresh implementation.

## Measurement (step 1)

- **afripay** `tests/Application/Common/ValidationBehaviorTests.cs`: five locals `RequestHandlerDelegate<string> next = _ => { called = true; return Task.FromResult("ok"); };` / `_ => Task.FromResult("ok")`, handed to `behavior.Handle(request, next, default)` (also inside `async () => await ...`). MediatR `12.*` (12.5: the delegate takes an optional `CancellationToken`).
- **cleanmicro** `tests/Application.UnitTests/Pipelines/ExceptionEnrichingTests.cs`: Moq field `Mock<RequestHandlerDelegate<Result<ProductDto>>> _handlerMock = new();`, `Setup(x => x.Invoke()).ThrowsAsync(..)` / `.ReturnsAsync(dto)`, `_handlerMock.Object` handed to `Handle`. MediatR 12.2.0, Moq 4.20.70 (has `ReturnsAsync`/`ThrowsAsync` for `ValueTask<T>`).
- **fullcart**: the "Autofac decorator" is in fact Scrutor: `services.Decorate(typeof(INotificationHandler<>), typeof(DispatchingIntegrationEventDecorator<>))`.

## Choices

- **Real lambda** → `MessageHandlerDelegate<TReq, TRes>` with parameters `(_, token)` (`(_, _)` when the old parameter was `_` or absent). `TReq` comes from the behavior's `Handle` the delegate is handed to (as for mocks). Not async: every returned `Task.FromResult/FromException/FromCanceled` → `ValueTask.X`, another `Task<T>` → `new ValueTask<T>(task)`, `throw` stays. Async lambdas only change the parameters (the async return type follows the target).
  Manual: method group, typed/extra lambda parameters, a lambda whose body uses the parameter `_` (it would become a discard), a returned non-Task.
- **Moq** `Mock<RequestHandlerDelegate<T>>` (private field or local, `new()` or `new Mock<..>()`): type → `Mock<MessageHandlerDelegate<TReq, T>>`; `Setup/Verify(x => x.Invoke(..))` or `x => x(..)` → `(It.IsAny<TReq>(), token or It.IsAny<CancellationToken>())`; after `Setup` only `ReturnsAsync`, `ThrowsAsync`, `Throws`, `Callback`, `Verifiable` and `Returns(Task.FromResult(x))` (→ `ValueTask.FromResult`) are accepted; `.Object` must be the `next` argument of a behavior's `Handle`. Anything else → Manual with the line.
- `using NSubstitute;` is only added when NSubstitute calls were rewritten (it was added always before; with Moq it would not compile).
- **fullcart decorator: principally manual (justified partial).** Mediator's source generator registers every `INotificationHandler<T>` implementation it sees, an open generic one as a handler of every notification; the Scrutor decorator would then also run as a plain handler (with itself as `inner`), so notification semantics change. The only case solved is MediatR registered by hand in Autofac (`AutofacMediatorModulePattern`, TAAK-196). fullcart stays partial as in TAAK-274: requests on Mediator, notifications + decorator on MediatR.

## Steps

- [x] 1. Measure on the corpus repo(s) and probe the old library (above)
- [x] 2. Implementation + proofs: `RequestHandlerDelegatePattern` (real lambda, Moq); `_Combined/request-handler-delegate-lambda`, `_Combined/request-handler-delegate-moq`; Manual `method-group` (was `lambda`), `lambda-discard-parameter`, `moq-returns-value`. 1604 tests green.
- [x] 3. Corpus `after`: afripay 0/16 → 25/0 (264/264), cleanmicro 0/20 → 19/0 (67/67), fullcart 27/20 → 27/20 (9/9); 0 regressions (CORPUSRUN-blind.md). The first run exposed two build breaks, fixed with proofs:
  - cleanmicro: `FluentActions.Invoking(() => _sut.Handle(..)).Should().ThrowAsync()` got a `ValueTask` → a behavior `Handle` call that gets the delegate and is not awaited on the spot now gets `.AsTask()` (`_Combined/request-handler-delegate-moq`).
  - afripay: `record Q : MediatR.IRequest<R>` in files without `using MediatR;` had no finding at all and broke after the package went (CS0246) → new pattern `mediatr.qualified-swapped-type` (`QualifiedSwappedType/record-base`, `_Combined/qualified-request-afripay`); `MediatRFix.SwappedTypeNames` is now static so the pattern shares the list. `docs/patterns.md` got its row.
- [x] 4. Docs: plan/verify/models of `mediatr.request-handler-delegate` (REQ-6..8) and `mediatr.qualified-swapped-type` (new, README row); `verify-head.sh` result in the handover report.

**Status**: ✅ done (2026-10-19)

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs`
