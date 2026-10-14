# Models: mediatr.behavior-di-registration

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/BehaviorDiRegistrationPattern.cs`.

## Figure 1: classes

**Figure 1.** `BehaviorDiRegistrationPattern` and the types it recognises.

<!-- kingmadoc:diagram img/mediatr-behavior-di-registration-modellen-1.d2 -->
![Figure 1: classes](img/mediatr-behavior-di-registration-modellen-1.png)

| Element                              | Role                                                                                 |
| ------------------------------------ | ------------------------------------------------------------------------------------ |
| `BehaviorDiRegistrationPattern`      | Finds the DI registration, returns the node unchanged as `Fixed`                     |
| `IPattern`                           | `Find` + `Rewrite`                                                                   |
| `FixedResult`                        | `NewNodes` = the original call, no `Warning`                                         |
| `ServiceCollectionServiceExtensions` | `AddTransient`/`AddScoped`/`AddSingleton(Type, Type)`                                |
| `MediatR.IPipelineBehavior<,>`       | Recognised type; `MediatRFix.SwappedTypes` turns it into `Mediator.IPipelineBehavior<,>` |

## Figure 2: sequence

**Figure 2.** From `Fixer` via `Find` and `Rewrite` to `Fixed`.

<!-- kingmadoc:diagram img/mediatr-behavior-di-registration-modellen-2.d2 -->
![Figure 2: sequence](img/mediatr-behavior-di-registration-modellen-2.png)

| Step      | Method in the code              | What happens                                          |
| --------- | ------------------------------- | ----------------------------------------------------- |
| Find      | `Find`, `GetSymbolInfo`         | `Add*` from `Microsoft.Extensions.DependencyInjection` |
| Type      | `GetTypeInfo(serviceType.Type)` | the first `typeof` is `IPipelineBehavior\`2` from `MediatR` |
| Write     | `PatternResult.Fixed(node)`     | the node stays; the Fixer then swaps the using        |

## Figure 3: decision tree

**Figure 3.** Questions of `Find`/`Rewrite`.

<!-- kingmadoc:diagram img/mediatr-behavior-di-registration-modellen-3.d2 -->
![Figure 3: decision tree](img/mediatr-behavior-di-registration-modellen-3.png)

| Leaf          | Condition                                                                       | Evidence                        |
| ------------- | ------------------------------------------------------------------------------- | ------------------------------- |
| not found     | not two `typeof` arguments, another `Add*` or another type                      | unit test `Find_Registration_…` |
| stays         | `AddTransient/AddScoped/AddSingleton(typeof(IPipelineBehavior<..>), typeof(X))` | `transient`, `lifetimes`        |

## See also

- [mediatr-behavior-di-registration-plan.md](mediatr-behavior-di-registration-plan.md)
- [mediatr-behavior-di-registration-verify.md](mediatr-behavior-di-registration-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/BehaviorDiRegistrationPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/BehaviorDiRegistration/`
