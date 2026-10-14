# Models: mediatr.mediator-implementation

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/MediatorImplementationPattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `MediatorImplementationPattern` | `IPattern`; finds the class, rewrites it in one go |
| `MediatRSyntax.IsMediatR` / `IsTaskMember` | Recognises MediatR interfaces and `Task.CompletedTask`/`Task.FromResult` |
| `MediatRFix.SwappedTypes` | Swaps `IMediator`, `IRequest<T>`, `INotification`, `IStreamRequest<T>` via the using |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find` | Classes with MediatR `IMediator`/`ISender`/`IPublisher` in their own base list |
| Members | `Rewrite` | Per interface method `FindImplementationForInterfaceMember`: in this declaration, not explicit, `IsNoOp` |
| Write | `Members` | Mediator's members as text, with the file's indentation and line ending; the first gets the trivia of the first old member |
| Usings | `PatternResult.Fixed(..., namespaces)` | `System.Threading(.Tasks)`, with `ISender` also `System.Collections.Generic` and `System.Runtime.CompilerServices` |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Manual "implemented in a base class" | member not in this declaration | — |
| Manual "implements … explicitly" | explicit implementation | — |
| Manual "does more than complete" | logic in a member | `with-logic` |
| Fixed: sender + publisher | `IMediator` | `no-op` |
| Fixed: only publisher | `IPublisher` | `publisher` |

## See also

- [mediatr-mediator-implementation-plan.md](mediatr-mediator-implementation-plan.md)
- [mediatr-mediator-implementation-verify.md](mediatr-mediator-implementation-verify.md)
