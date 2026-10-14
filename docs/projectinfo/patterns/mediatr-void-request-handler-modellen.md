# Models: mediatr.void-request-handler

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/VoidRequestHandlerPattern.cs` and `MediatRSyntax.cs`.

## Figure 1: classes

**Figure 1.** `VoidRequestHandlerPattern`, its helper and the MediatR and Mediator types.

<!-- kingmadoc:diagram img/mediatr-void-request-handler-modellen-1.d2 -->
![Figure 1: classes](img/mediatr-void-request-handler-modellen-1.png)

| Element                     | Role                                                                                                  |
| --------------------------- | ----------------------------------------------------------------------------------------------------- |
| `VoidRequestHandlerPattern` | Finds `Handle` of `MediatR.IRequestHandler<TReq>` and turns it into `ValueTask<Unit> Handle`           |
| `MediatRSyntax`             | Shared with `RequestHandlerPattern` and `NotificationHandlerPattern`: finding, `Task`→`ValueTask`, returns |
| `Mediator.IRequestHandler<TReq>` | Is `IRequestHandler<TReq, Unit>`: hence `ValueTask<Unit>` and `Unit.Value`                       |

## Figure 2: sequence

**Figure 2.** From `Fixer` via `Find` and `Rewrite` to `Fixed` or `Manual`.

<!-- kingmadoc:diagram img/mediatr-void-request-handler-modellen-2.d2 -->
![Figure 2: sequence](img/mediatr-void-request-handler-modellen-2.png)

| Step        | Method in the code                                           | What happens                                                     |
| ----------- | ------------------------------------------------------------ | ---------------------------------------------------------------- |
| Find        | `MediatRSyntax.FindHandleMethods` (`IRequestHandler<>`)      | Semantically the `Handle` that implements the interface member   |
| Return type | `MediatRSyntax.ToValueTask` + `WithUnit`                     | `Task` → `ValueTask<Unit>`                                       |
| async       | `RewriteAsync`, `AnalyzeControlFlow`                         | `return;` → `return Unit.Value;`; a reachable end gets one       |
| non-async   | `RewriteSync`, `ReturnedExpressions`, `IsTaskMember`         | `Task.CompletedTask` → `Unit.ValueTask`                          |

## Figure 3: decision tree

**Figure 3.** Questions of `Rewrite`.

<!-- kingmadoc:diagram img/mediatr-void-request-handler-modellen-3.d2 -->
![Figure 3: decision tree](img/mediatr-void-request-handler-modellen-3.png)

| Leaf                                  | Condition                                     | Evidence                                                      |
| ------------------------------------- | --------------------------------------------- | ------------------------------------------------------------- |
| Manual "explicit interface"           | `Task IRequestHandler<A>.Handle`              | `VoidRequestHandler/explicit`                                 |
| Manual "does not return Task"         | return type not in the form `Task` (e.g. an alias) | no proof                                                 |
| Manual "without a statement body"     | `async` with an expression body               | `VoidRequestHandler/async-expression`                         |
| `ValueTask<Unit>` + `return Unit.Value` | `async` with a block                         | `VoidRequestHandler/async`, `early-return`, `throw-at-end`    |
| `ValueTask<Unit>` + `Unit.ValueTask`  | non-async, all returns `Task.CompletedTask`   | `VoidRequestHandler/completed-task`, `completed-task-expression` |
| Manual "not Task.CompletedTask"       | another return, e.g. `Task.Delay`             | `VoidRequestHandler/other-return`                             |

## See also

- [mediatr-void-request-handler-plan.md](mediatr-void-request-handler-plan.md)
- [mediatr-void-request-handler-verify.md](mediatr-void-request-handler-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/VoidRequestHandlerPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/VoidRequestHandler/`
