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

> Drafted from TAAK-062.

## One-sentence summary

`StreamHandlerDelegate<TRes> next` becomes `StreamHandlerDelegate<TReq, TRes> next`, `next()` becomes `next(request, ct)` and a type-parameter request without an `IStreamRequest` constraint gets `IStreamMessage`; return type `IAsyncEnumerable<TRes>` stays.

## Scope (in / out)

**Out of scope**: Registration: `AddOpenStreamBehavior`/`AddStreamBehavior` → `StreamPipelineBehaviors` is in `mediatr.add-mediatr`.

## Requirements

- **REQ-1**: WHEN the request type parameter only has `notnull` THE SYSTEM SHALL add `IStreamMessage` and rewrite `next` (proof `open`).
- **REQ-2**: WHEN the constraint is `IStreamRequest<TResponse>` THE SYSTEM SHALL leave the constraint in place (after the swap that is an `IStreamMessage`) (proof `request-constraint`).
- **REQ-3**: IF `next` is used other than as `next()` THEN THE SYSTEM SHALL report Manual (proof `next-passed`).

## Assumptions

- Mediator 3.0.2: `IStreamPipelineBehavior<TMessage, TResponse> where TMessage : IStreamMessage`, `delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<TMessage, TResponse>(TMessage, CancellationToken)`; `IStreamPipelineBehavior`2` is now in `SwappedTypes`.
- The registration is handled in `mediatr.add-mediatr`; the fix is all-or-nothing (`IFix.AllOrNothing`), so a Manual elsewhere makes this pattern write nothing.

## Models

Classes, sequence and decision tree: [mediatr-stream-pipeline-behavior-modellen.md](mediatr-stream-pipeline-behavior-modellen.md).

## See also

- [mediatr-stream-pipeline-behavior-verify.md](mediatr-stream-pipeline-behavior-verify.md)
- `claude/tasks/archief/TAAK-062-*.md`
