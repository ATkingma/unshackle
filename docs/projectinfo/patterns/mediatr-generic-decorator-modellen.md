# Models: mediatr.generic-decorator

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericDecoratorPattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `GenericDecoratorPattern` | `ISolutionPattern`; declaration and `typeof(S<>)` with the whole solution |
| `Kind` (nested) | `Void`, `Result`, `Notification`: kind of decorated handler |
| `IsDecorator` (static) | Used by `UnsupportedPattern` so that the decorator is not reported as a generic type |
| `AutofacMediatorModulePattern` | Its `Populate` filter on `"Mediator.Internals"` counts as "MediatR registered by hand in Autofac" |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find` | Decorator declaration (`Decorated`, not `Result`) and `typeof(S<>)` as the service of `RegisterGenericDecorator` (`VoidService`) |
| Service | `RewriteAsync` | `typeof(S<>)` → `typeof(IRequestHandler<,>)` |
| Registration | `UnregisteredUseAsync` | `SymbolFinder.FindReferencesAsync`: every reference outside the class must be `typeof(D<>)` as the first argument of `RegisterGenericDecorator` |
| Notification | `RegistersMediatRInAutofacAsync` | `Populate(... "Mediator.Internals" ...)` or `RegisterAssemblyTypes(typeof(<MediatR type>)...)` somewhere in the solution |
| Void | `RewriteVoid` | Written decorated types → `IRequestHandler<T, Unit>`; usage must be `Handle(...)` or an assignment |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Not found | decorator of a handler with a response | `Find_DecoratorWithResponse_LeavesItToTheHandlerPatterns` |
| Not found (stays `mediatr.unsupported`) | no decorated parameter | `IsDecorator_OpenGenericHandlerWithoutDecoratedParameter_False` |
| Manual "used other than in RegisterGenericDecorator" | another reference | `used-elsewhere` |
| Manual "every notification" | notification, no Autofac registration of MediatR | `notification-without-autofac` |
| Fixed (unchanged) | notification, MediatR in Autofac | `_Combined/generic-decorators-kgrzybek` |
| Manual "declares members of its own" | own interface with members | `decorated-members` |
| Manual "used other than to call Handle" | other use of the decorated handler | `decorated-other-use` |
| Fixed: `IRequestHandler<T, Unit>` + `typeof(IRequestHandler<,>)` | void decorator, only registered | `_Combined/generic-decorators-kgrzybek` |

## See also

- [mediatr-generic-decorator-plan.md](mediatr-generic-decorator-plan.md)
- [mediatr-generic-decorator-verify.md](mediatr-generic-decorator-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/GenericDecorator/`
