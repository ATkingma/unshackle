# TAAK-191 — MediatR: one `RequestHandlerDelegate` mock shared by multiple request types

**Status**: ✅ Done
**Created**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`, Repetition after TAAK-180..184)
**Goal**: amantin migrates fully (build + 85/85).

## Context for the next agent

- amantin (`5822811`, MediatR 12.2.0), only root Manual after TAAK-180: `tests/CleanArchitecture.Application.UnitTests/Common/Behaviors/AuthorizationBehaviorTests.cs:9`, `mediatr.request-handler-delegate`: "_mockNextBehavior is not handed to exactly one request type of a behavior's Handle".
- The field `RequestHandlerDelegate<ErrorOr<Response>> _mockNextBehavior` goes to `AuthorizationBehavior<RequestWithNoAuthorizationAttribute, …>`, `<RequestWithSingleAuthorizationAttribute, …>` and `<RequestWithTonsOfAuthorizationAttribute, …>`; all three are records implementing `IAuthorizeableRequest<ErrorOr<Response>>`.
- `ValidationBehaviorTests` (one request type) is already covered.

## Analysis

- Option A: field type `MessageHandlerDelegate<TCommon, R>` with `TCommon` = most specific common base (here `IAuthorizeableRequest<ErrorOr<Response>>`, otherwise `IMessage`). Only works if `TMessage` in Mediator's delegate is contravariant (`in`) and all request types are reference types: check Mediator.Abstractions 3.x first.
- Option B: per call an adapter lambda `(m, ct) => _mockNextBehavior(m, ct)`; still requires a field type that all three fit, so the same question.
- `Invoke(Arg.Any<TCommon>(), Arg.Any<CancellationToken>())` keeps matching for all calls.
- No common base, or value types → Manual remains (current behavior).

## Choices

- Probe (Mediator.Abstractions 3.0.2): `MessageHandlerDelegate<TMessage, TResponse>` is invariant; `MessageHandlerDelegate<IA<int>,int>` is not assignable to `<R1,int>` (CS0029), a lambda `(m, ct) => f(m, ct)` is.
- Options A+B combined: the field/mock gets `MessageHandlerDelegate<IMessage, TRes>`, every `next` argument of a `Handle` becomes an adapter lambda. The common type is always `IMessage` (not the most specific base): always valid, also with value types (boxing) and without constraint/accessibility risk.
- A single request type keeps the old path (no lambda). No `Handle` call: Manual with new text.
- Lambda parameter names are chosen freely (`_` suffix on collision).

## Steps

1. [x] Variance measured (probe `<probe-dir>/probe-191`, cleaned up).
2. [x] Proof `_Combined/request-handler-delegate-shared-field` (before/after).
3. [x] Implementation in `RequestHandlerDelegatePattern` + plan/models/verify.
4. [ ] Harness not run on amantin (corpus project not present); mimicked via the proof.

## See also

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs`
- `claude/tasks/archief/TAAK-180-mediatr-test-request-handler-delegate.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
