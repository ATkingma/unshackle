---
kingmadoc: 1
feature: "mediatr-mediator-implementation"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/MediatorImplementationPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/MediatorImplementation/
---

# Feature: mediatr.mediator-implementation: own no-op `IMediator` → Mediator's interface

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-13 (by hand) |

> Drawn up from TAAK-197 (ardalis v9.1.0, `tests/Clean.Architecture.UnitTests/NoOpMediator.cs` broke the build with CS0535/CS0738).

## One-sentence summary

An own class that implements MediatR's `IMediator`, `ISender` or `IPublisher` and contains only no-ops gets the members of Mediator 3.0.2
(`ValueTask`, `Send` for `IRequest`/`ICommand`/`IQuery`/`object`, four `CreateStream` overloads, two `Publish`), each again as a no-op.

## Scope (in / out)

**In**: a class with MediatR's `IMediator`/`ISender`/`IPublisher` in its own base list, of which every implementing method is in that declaration and only
completes: `return Task.CompletedTask;`, `return Task.FromResult(default | default! | null | default(T));`, `await Task.CompletedTask;`, `yield break;`, or empty.

**Out (Manual)**: a member with logic, an explicit interface implementation, a member from a base class or another `partial` part.

## Requirements

- **REQ-1**: WHEN a no-op `IMediator` is found THE SYSTEM SHALL replace the old members with Mediator's `ISender` and `IPublisher` members as no-ops (`=> default`, an empty async iterator) at the place of the first old member; other members stay (proof `no-op`).
- **REQ-2**: WHEN only `IPublisher` (or `ISender`) is implemented THE SYSTEM SHALL write only those members and add only the needed usings (proof `publisher`).
- **REQ-3**: THE SYSTEM SHALL write `object?` where nullable annotations are on, otherwise `object`.
- **REQ-4**: IF a member does more than complete THEN Manual (proof `with-logic`).

## Assumptions

- Mediator 3.0.2: `IMediator : ISender, IPublisher` without members of its own; names and defaults of parameters measured with reflection (TAAK-197).
- `default` for `ValueTask<TResponse>` is a completed task with `default(TResponse)`, equal to `Task.FromResult<TResponse>(default!)`.
- MediatR's `Send<TRequest>(TRequest) where TRequest : IRequest` has no counterpart: Mediator's void request is `IRequest<Unit>`.

## Models

[mediatr-mediator-implementation-modellen.md](mediatr-mediator-implementation-modellen.md).

## See also

- [mediatr-mediator-implementation-verify.md](mediatr-mediator-implementation-verify.md)
- `claude/tasks/archief/TAAK-197-mediatr-eigen-imediator-implementatie.md`
