---
kingmadoc: 1
feature: "mediatr-request-processor-caller"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorCallerPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/RequestProcessorCaller/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/
---

# Feature: mediatr.request-processor-caller: callers of a rewritten request processor

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (by hand, kingmadoc missing) |

> Drafted from TAAK-190 (corpus run 2026-10-12, jason v8.0.0: 4x CS0305 in `RequestLoggerTests`).

## One-sentence summary

Code that creates or calls a processor rewritten by `mediatr.request-processor` (`new P<Cmd>(...)`, `await p.Process(req, ct)`) is moved along to Mediator's `P<Cmd, TRes>` and the public pipeline `Handle`; anything that cannot be translated exactly becomes Manual with a line number, never a silent build break.

## Scope (in / out)

**In scope**:
- Every reference `P<TReq>` (type argument, `new`, field, local variable, `Substitute.For<...>`) to a pre-processor class with an open message type: `TRes` follows from `TReq : IRequest<TRes>` (`IRequest` without a result gives `Unit`).
- `await p.Process(req, ct);` as a standalone statement, for pre-processors (`Handle(req, static (_, _) => default, ct)`) and post-processors (`Handle(req, (_, _) => new ValueTask<TRes>(res), ct)`).

**Out of scope**: registrations in the `AddMediatR` lambda (`mediatr.add-mediatr`); `Process` calls whose result is used (`Task t = ...`, `return ...`, `.Wait()`), mocks of `Process`, calls via the MediatR interface and open or untraceable type arguments: Manual.

## Requirements

- **REQ-1**: WHEN code writes `new P<Cmd>(...)` (or another type reference) for a rewritten pre-processor over an open message type THE SYSTEM SHALL add the response argument (`Cmd : IRequest<int>` gives `P<Cmd, int>`, `IRequest` gives `P<Ping, Unit>`) (proof `_Combined/request-processor-callers`).
- **REQ-2**: WHEN an `await p.Process(...)` statement calls a processor THE SYSTEM SHALL write `Handle` with a `next` delegate that calls the public pipeline method, for a post-processor with the response as the value (proof `_Combined/request-processor-callers`).
- **REQ-3**: IF `Process` is used other than as a standalone `await` statement, or via the MediatR interface THEN THE SYSTEM SHALL report Manual (proofs `RequestProcessorCaller/result-used`, `through-interface`).
- **REQ-4**: IF the response type of the type argument cannot be derived (type without `IRequest`, unconstrained type parameter) or the argument is open THEN THE SYSTEM SHALL report Manual (proof `RequestProcessorCaller/unknown-response`).

## Assumptions

- Mediator 3.0.2: `MessagePreProcessor`/`MessagePostProcessor` have a public `Handle(message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken)`; for a pre-processor it first calls its own `Handle` and then `next`, for a post-processor first `next` and then its own `Handle`. The `default` result of `next` is invisible to the caller (`await` without a value). Proven by the `after` of the proof compiling.
- The pattern comes before `mediatr.request-processor` in `MediatRFix.Patterns`: the callers then still bind to the MediatR class. The pattern searches per file across the whole solution, so also in a test project.
- The fix is all-or-nothing: a Manual here leaves the whole solution untouched.

## Models

[mediatr-request-processor-caller-modellen.md](mediatr-request-processor-caller-modellen.md).

## See also

- [mediatr-request-processor-caller-verify.md](mediatr-request-processor-caller-verify.md)
- [mediatr-request-processor-plan.md](mediatr-request-processor-plan.md)
- `claude/tasks/archief/TAAK-190-mediatr-request-processor-aanroepers.md`
