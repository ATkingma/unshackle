# Models: mediatr.send-as-task

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/SendAsTaskPattern.cs`.

## Figure 1: classes

**Figure 1.** `SendAsTaskPattern` and the MediatR and Mediator return types.

<!-- kingmadoc:diagram img/mediatr-send-as-task-modellen-1.d2 -->
![Figure 1: classes](img/mediatr-send-as-task-modellen-1.png)

| Element             | Role                                                              |
| ------------------- | ----------------------------------------------------------------- |
| `SendAsTaskPattern` | Finds non-awaited `Send`/`Publish` from assembly `MediatR`        |
| `IsAwaited`         | `await x`, `await (x)`, `await x.ConfigureAwait(...)` → skip      |
| MediatR             | `Task<T> Send(...)`, `Task Publish(...)`                          |
| Mediator            | `ValueTask<T> Send(...)`, `ValueTask Publish(...)`, `.AsTask()`   |

## Figure 2: sequence

**Figure 2.** From `Fixer` via `Find` and `Rewrite` to `Fixed`.

<!-- kingmadoc:diagram img/mediatr-send-as-task-modellen-2.d2 -->
![Figure 2: sequence](img/mediatr-send-as-task-modellen-2.png)

| Step      | Method in the code                   | What happens                            |
| --------- | ------------------------------------ | --------------------------------------- |
| Find      | `Find` with `GetSymbolInfo`          | `Send`/`Publish` from assembly `MediatR` |
| Filter    | `IsAwaited`                          | A direct await stays in place           |
| Write     | `SyntaxFactory.InvocationExpression` | `x.Send(...).AsTask()`, trivia preserved |

## Figure 3: decision tree

**Figure 3.** Questions asked by `Find`/`Rewrite`.

<!-- kingmadoc:diagram img/mediatr-send-as-task-modellen-3.d2 -->
![Figure 3: decision tree](img/mediatr-send-as-task-modellen-3.png)

| Leaf                   | Condition                         | Proof                      |
| ---------------------- | --------------------------------- | -------------------------- |
| not found              | no `Send`/`Publish` from MediatR  | n/a                        |
| Manual "called with ?." | `x?.Send(...)` (MemberBinding)  | `null-conditional`         |
| stays in place         | directly awaited                  | `task-uses`                |
| `.AsTask()` added      | every other use                   | `task-uses`, `send-object` |

## See also

- [mediatr-send-as-task-plan.md](mediatr-send-as-task-plan.md)
- [mediatr-send-as-task-verify.md](mediatr-send-as-task-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/SendAsTaskPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/SendAsTask/`
