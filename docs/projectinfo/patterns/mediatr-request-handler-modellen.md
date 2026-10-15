# Models: mediatr.request-handler

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerPattern.cs` and `MediatRFix.SwappedTypes`.

## Figure 1: classes

**Figure 1.** `RequestHandlerPattern`, its contract and the MediatR and Mediator types.

<!-- kingmadoc:diagram img/mediatr-request-handler-modellen-1.d2 -->
![Figure 1: classes](img/mediatr-request-handler-modellen-1.png)

| Element                 | Role                                                                                                                               |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `RequestHandlerPattern` | Finds `Handle` of every class that implements `MediatR.IRequestHandler<,>` and rewrites return type and returns                     |
| `MediatRFix`            | Registers the pattern; `SwappedTypes` lets `using MediatR` → `using Mediator` swap while `IRequestHandler<,>` stays in place        |
| `PatternResult`         | `Fixed(method, "System.Threading.Tasks")` or `Manual(reden)`                                                                       |
| MediatR 12.5.0          | `Task<TRes> Handle(TReq, CancellationToken)`                                                                                       |
| Mediator 3.0.2          | `ValueTask<TRes> Handle(TReq, CancellationToken)`                                                                                  |

## Figure 2: sequence

**Figure 2.** From `Find` to the using swap by the `Fixer`.

<!-- kingmadoc:diagram img/mediatr-request-handler-modellen-2.d2 -->
![Figure 2: sequence](img/mediatr-request-handler-modellen-2.png)

| Step        | Method in the code                                                                       | What happens                                                                                    |
| ----------- | ---------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Find        | `MediatRSyntax.FindHandleMethods` (`IRequestHandler<,>`) | Semantic: the `Handle` method that implements the interface member, in the same class           |
| Return type | `MediatRSyntax.ToValueTask`                                                                          | `Task<T>` or `System.Threading.Tasks.Task<T>` → `ValueTask<T>`                                  |
| Returns     | `MediatRSyntax.ReturnedExpressions`, `IsTaskMember(..., "FromResult")`                                                | Expression body or every `return` (not in lambdas/local functions) must be `Task.FromResult` |
| Usings      | `UsingDirectives.UpdateAsync` in `Fixer`                                                 | `using MediatR` → `using Mediator` thanks to `SwappedTypes`                                     |

## Figure 3: decision tree

**Figure 3.** Questions asked by `Rewrite`.

<!-- kingmadoc:diagram img/mediatr-request-handler-modellen-3.d2 -->
![Figure 3: decision tree](img/mediatr-request-handler-modellen-3.png)

| Leaf                                       | Condition                                        | Proof                                                                   |
| ------------------------------------------ | ------------------------------------------------ | ----------------------------------------------------------------------- |
| Rewritten, specifier kept                  | `Task<T> IRequestHandler<A,B>.Handle`            | `RequestHandler/explicit`, `explicit-async`                             |
| Manual "does not return Task<T>"           | return type not in the form `Task<T>` (e.g. alias) | no proof (cannot compile with MediatR without alias)                  |
| `ValueTask<T>`, body unchanged             | `async`                                          | `RequestHandler/async`, `RequestHandler/token-name`                     |
| `ValueTask<T>` + `ValueTask.FromResult`    | non-async, all returns `Task.FromResult`         | `RequestHandler/from-result-expression`, `from-result-block`, `generic` |
| Manual "not Task.FromResult"               | other return, e.g. `Task.Run`                    | `RequestHandler/other-return`                                           |

## See also

- [mediatr-request-handler-plan.md](mediatr-request-handler-plan.md)
- [mediatr-request-handler-verify.md](mediatr-request-handler-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/RequestHandler/`
