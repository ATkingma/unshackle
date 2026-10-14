# Models: mediatr.stream-pipeline-behavior

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/StreamPipelineBehaviorPattern.cs` and `MediatRSyntax.cs`.

## Figure 1: classes

**Figure 1.** `StreamPipelineBehaviorPattern`, its helper and the MediatR and Mediator types.

<!-- kingmadoc:diagram img/mediatr-stream-pipeline-behavior-modellen-1.d2 -->
![Figure 1: classes](img/mediatr-stream-pipeline-behavior-modellen-1.png)

| Element | Role |
| ------- | ---- |
| `StreamPipelineBehaviorPattern` | Finds the type with `Handle` of `MediatR.IStreamPipelineBehavior<,>` and rewrites the type |
| `MediatRSyntax` | `FindHandleMethods`, `RejectExplicit`, `CommaList`, `WithConstraint` |
| `Mediator.IStreamPipelineBehavior<,>` | Same name, `StreamHandlerDelegate<TMessage, TResponse>` and `IStreamMessage` constraint |

## Figure 2: sequence

**Figure 2.** From `Fixer` via `Find` and `Rewrite` to `Fixed` or `Manual`.

<!-- kingmadoc:diagram img/mediatr-stream-pipeline-behavior-modellen-2.d2 -->
![Figure 2: sequence](img/mediatr-stream-pipeline-behavior-modellen-2.png)

| Step | Method in the code | What happens |
| ---- | ------------------ | ------------ |
| Find | `MediatRSyntax.FindHandleMethods` (`IStreamPipelineBehavior\`2`) | The type (Parent of `Handle`), because the constraint is on the type |
| `next` | `GetDeclaredSymbol` / `GetSymbolInfo` | Every use of the parameter must be `next()` |
| Rewrite | `ReplaceNodes`, `CommaList` | `next(request, token)`, `StreamHandlerDelegate<TReq, TRes>` |
| Constraint | `WithConstraint` | `IStreamMessage` on a type-parameter request, not if the constraint is already `IStreamRequest<T>` |
| Fix | `IFix.AllOrNothing` | One Manual in the solution: nothing written |

## Figure 3: decision tree

**Figure 3.** Questions asked by `Rewrite`.

<!-- kingmadoc:diagram img/mediatr-stream-pipeline-behavior-modellen-3.d2 -->
![Figure 3: decision tree](img/mediatr-stream-pipeline-behavior-modellen-3.png)

| Leaf | Condition | Proof |
| ---- | --------- | ----- |
| Manual "explicit interface" | `Handle` implemented explicitly | no proof |
| Manual "does not have the form" | other parameters | no proof |
| Manual "next is used other than as next()" | `next` passed on, stored or with arguments | `StreamPipelineBehavior/next-passed` |
| Fixed + `where T : IStreamMessage` | request is a type parameter without `IStreamRequest` constraint | `StreamPipelineBehavior/open` |
| Fixed, constraint untouched | constraint already `IStreamRequest<TResponse>` | `StreamPipelineBehavior/request-constraint` |

## See also

- [mediatr-stream-pipeline-behavior-plan.md](mediatr-stream-pipeline-behavior-plan.md)
- [mediatr-stream-pipeline-behavior-verify.md](mediatr-stream-pipeline-behavior-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/StreamPipelineBehaviorPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/StreamPipelineBehavior/`
