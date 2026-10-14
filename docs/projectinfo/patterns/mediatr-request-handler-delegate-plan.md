---
kingmadoc: 1
feature: "mediatr-request-handler-delegate"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Core/Fixer.cs
    - src/Unshackle.Core/ProjectPackages.cs
    - src/Unshackle.Tests/Proofs/MediatR/RequestHandlerDelegate/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/
---

# Feature: mediatr.request-handler-delegate: mocked `next` of a behavior in test code

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (by hand, kingmadoc missing) |

> Drafted from TAAK-180 (corpus run 2026-10-12, amantinband clean-architecture).

## One-sentence summary

Test code that configures `Substitute.For<RequestHandlerDelegate<TRes>>()` and passes it to `behavior.Handle(cmd, next, ct)` gets `MessageHandlerDelegate<TReq, TRes>` and `next.Invoke(Arg.Any<TReq>(), Arg.Any<CancellationToken>()).Returns(x)`; every other use of the delegate type is Manual with a line number.

## Scope (in / out)

**In scope**: a private field or local variable of type `RequestHandlerDelegate<T>` (or `var` with `Substitute.For`), used as the `next` argument of a `Handle` of an `IPipelineBehavior`, as the `Invoke()`/`()` receiver of `Returns`/`ReturnsForAnyArgs`, or in `Received()`/`DidNotReceive()`.

**Also in scope (safety net)**: every MediatR reference that remains after all patterns and has no finding becomes a Manual on that line (`unmatched-use`), so that "MediatR is still used in this project" on the project file is not the only clue.

**Out of scope**: lambdas as `next` (`behavior.Handle(cmd, () => Task.FromResult(x), ct)`); the delegate type of a behavior's own `Handle` (that is handled by `mediatr.pipeline-behavior`).

## Requirements

- **REQ-1**: WHEN a mocked `RequestHandlerDelegate<TRes>` is passed to a behavior `Handle` THE SYSTEM SHALL make the type `MessageHandlerDelegate<TReq, TRes>` (TReq from the `Handle` parameter), give `Invoke()` `Arg.Any` arguments and turn `Task.FromResult(x)` in `Returns` into `ValueTask.FromResult(x)` (proof `_Combined/request-handler-delegate`).
- **REQ-2**: IF the delegate is a real delegate (lambda), a parameter, is called directly, or `Returns` gets a Task/callback THEN THE SYSTEM SHALL report Manual with the line (proofs `lambda`, `parameter`, `invoke-call`, `callback-returns`).
- **REQ-3**: IF the delegate is not passed to any behavior `Handle` THEN THE SYSTEM SHALL report Manual, because the request type cannot be derived then (proof `no-handle-call`).
- **REQ-5** (TAAK-191): WHEN one delegate field is passed to `Handle` of behaviors with different request types THE SYSTEM SHALL type the field as `MessageHandlerDelegate<IMessage, TRes>`, use `Arg.Any<IMessage>()` and replace every `next` argument with an adapter lambda `(message, cancellationToken) => veld(message, cancellationToken)` (proof `_Combined/request-handler-delegate-shared-field`).
- **REQ-4**: WHEN, after the patterns, a MediatR symbol without a finding remains in a project that keeps the package THE SYSTEM SHALL report a Manual on that line (unit test `FixerProjectFileTests`).

## Assumptions

- Mediator 3.0.2: `delegate ValueTask<TResponse> MessageHandlerDelegate<TMessage, TResponse>(TMessage message, CancellationToken cancellationToken)`; NSubstitute 6 configures `ValueTask<T>` with `Returns(T)`.
- The pattern comes before `mediatr.pipeline-behavior` in `MediatRFix.Patterns`: if behavior and test are in one file, the `Handle` call still binds to the MediatR version.
- `MessageHandlerDelegate<TMessage, TResponse>` is invariant in `TMessage` (measured with a probe project: `MessageHandlerDelegate<IA<int>,int>` is not assignable to `<R1,int>`; a lambda is). Hence an adapter lambda per `Handle` call; `IMessage` as the common type is always valid (every request is an `IMessage`) and avoids accessibility and constraint problems of a user-defined base.
- Lambda parameter names (`message`, `cancellationToken`) get a `_` suffix if the name already occurs in the type.
- Each file is fixed against the original solution; the behavior in another file is therefore still the MediatR version.

## Models

Decision tree and steps: [mediatr-request-handler-delegate-modellen.md](mediatr-request-handler-delegate-modellen.md).

## See also

- [mediatr-request-handler-delegate-verify.md](mediatr-request-handler-delegate-verify.md)
- `claude/tasks/archief/TAAK-180-mediatr-test-request-handler-delegate.md`
- [mediatr-pipeline-behavior-plan.md](mediatr-pipeline-behavior-plan.md)
