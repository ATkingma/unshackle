# Models: mediatr.pipeline-behavior

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/PipelineBehaviorPattern.cs` and `MediatRSyntax.cs`.

## Figure 1: classes

**Figure 1.** `PipelineBehaviorPattern`, its helper and the MediatR and Mediator types.

<!-- kingmadoc:diagram img/mediatr-pipeline-behavior-modellen-1.d2 -->
![Figure 1: classes](img/mediatr-pipeline-behavior-modellen-1.png)

| Element                         | Role                                                                                            |
| ------------------------------- | ----------------------------------------------------------------------------------------------- |
| `PipelineBehaviorPattern`       | Finds the type with `Handle` of `MediatR.IPipelineBehavior<,>` and rewrites the type            |
| `MediatRSyntax`                 | Finding (now also with the type itself as the root), `Task`→`ValueTask`, returns, `Task.X`→`ValueTask.X`, `RejectExplicit`, `CommaList`, `WithConstraint` |
| `Mediator.IPipelineBehavior<,>` | Same name (now in `SwappedTypes`), a different `Handle` and an `IMessage` constraint            |

## Figure 2: sequence

**Figure 2.** From `Fixer` via `Find` and `Rewrite` to `Fixed` or `Manual`.

<!-- kingmadoc:diagram img/mediatr-pipeline-behavior-modellen-2.d2 -->
![Figure 2: sequence](img/mediatr-pipeline-behavior-modellen-2.png)

| Step         | Method in the code                                         | What happens                                                                         |
| ------------ | ---------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Find         | `MediatRSyntax.FindHandleMethods` (`IPipelineBehavior\`2`) | The type (Parent of `Handle`), because the constraint is on the type                 |
| `next`       | `GetDeclaredSymbol` / `GetSymbolInfo`                      | Every use of the parameter must be a call                                            |
| Rewrite      | `ReplaceNodes`, `CommaList`, `OnValueTask`                 | `next(request, token)`, `MessageHandlerDelegate<TReq, TRes>`, `ValueTask.FromResult` |
| Constraint   | `WithMessageConstraint` → `MediatRSyntax.WithConstraint`   | `IMessage` on a type-parameter request, unless the constraint is already a MediatR message (TAAK-081) |

## Figure 3: decision tree

**Figure 3.** Questions of `Rewrite`.

<!-- kingmadoc:diagram img/mediatr-pipeline-behavior-modellen-3.d2 -->
![Figure 3: decision tree](img/mediatr-pipeline-behavior-modellen-3.png)

| Leaf                                   | Condition                               | Evidence                                                      |
| -------------------------------------- | --------------------------------------- | ------------------------------------------------------------- |
| Manual "explicit interface"            | `IPipelineBehavior<,>.Handle` explicit  | `PipelineBehavior/explicit`                                   |
| Manual "does not have the form"        | no `Task<T>`, other parameters          | no proof                                                      |
| Manual "next is used other than"       | `next` stored or passed on              | `PipelineBehavior/next-stored`                                |
| Manual "not next() or Task.FromResult" | non-async, another return               | `PipelineBehavior/next-passed`                                |
| Fixed + `where T : IMessage`           | the request is a type parameter         | `open-async`, `no-constraint`, `new-constraint`, `next-token`, `edge-notnull-constraint` |
| Fixed without an extra constraint      | the constraint is already `IRequest<T>`/`INotification` (or an own interface on it) | `PipelineBehavior/edge-request-constraint` |
| Fixed without a constraint             | a closed request type                   | `PipelineBehavior/closed`                                     |

## See also

- [mediatr-pipeline-behavior-plan.md](mediatr-pipeline-behavior-plan.md)
- [mediatr-pipeline-behavior-verify.md](mediatr-pipeline-behavior-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/PipelineBehaviorPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/PipelineBehavior/`
