---
kingmadoc: 1
feature: "mediatr-stream-pipeline-behavior"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/StreamPipelineBehaviorPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/StreamPipelineBehavior/
---

# Feature: mediatr.stream-pipeline-behavior: IStreamPipelineBehavior: next() → next(request, ct)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-062.

## One-sentence summary

`StreamHandlerDelegate<TRes> next` wordt `StreamHandlerDelegate<TReq, TRes> next`, `next()` wordt `next(request, ct)` en een typeparameter-request zonder `IStreamRequest`-constraint krijgt `IStreamMessage`; returntype `IAsyncEnumerable<TRes>` blijft.

## Scope (in / out)

**Out of scope**: Registratie: `AddOpenStreamBehavior`/`AddStreamBehavior` → `StreamPipelineBehaviors` zit in `mediatr.add-mediatr`.

## Requirements

- **REQ-1**: WHEN de request-typeparameter alleen `notnull` heeft THE SYSTEM SHALL `IStreamMessage` toevoegen en `next` herschrijven (proof `open`).
- **REQ-2**: WHEN de constraint `IStreamRequest<TResponse>` is THE SYSTEM SHALL de constraint laten staan (na de swap is dat een `IStreamMessage`) (proof `request-constraint`).
- **REQ-3**: IF `next` anders gebruikt wordt dan `next()` THEN THE SYSTEM SHALL Manual melden (proof `next-passed`).

## Assumptions

- Mediator 3.0.2: `IStreamPipelineBehavior<TMessage, TResponse> where TMessage : IStreamMessage`, `delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<TMessage, TResponse>(TMessage, CancellationToken)`; `IStreamPipelineBehavior`2` staat nu in `SwappedTypes`.
- De registratie volgt in `mediatr.add-mediatr`; de fix is alles-of-niets (`IFix.AllOrNothing`), dus een Manual elders laat dit patroon niets schrijven.

## Modellen

Klassen, sequence en beslisboom: [mediatr-stream-pipeline-behavior-modellen.md](mediatr-stream-pipeline-behavior-modellen.md).

## Zie ook

- [mediatr-stream-pipeline-behavior-verify.md](mediatr-stream-pipeline-behavior-verify.md)
- `claude/tasks/archief/TAAK-062-*.md`
