# Modellen: mediatr.generic-decorator

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericDecoratorPattern.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `GenericDecoratorPattern` | `ISolutionPattern`; declaratie en `typeof(S<>)` met de hele solution |
| `Kind` (genest) | `Void`, `Result`, `Notification`: soort gedecoreerde handler |
| `IsDecorator` (statisch) | Gebruikt door `UnsupportedPattern` om de decorator niet als generiek type te melden |
| `AutofacMediatorModulePattern` | Zijn `Populate`-filter op `"Mediator.Internals"` telt als "MediatR met de hand in Autofac" |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Vinden | `Find` | Decorator-declaratie (`Decorated`, niet `Result`) en `typeof(S<>)` als service van `RegisterGenericDecorator` (`VoidService`) |
| Service | `RewriteAsync` | `typeof(S<>)` → `typeof(IRequestHandler<,>)` |
| Registratie | `UnregisteredUseAsync` | `SymbolFinder.FindReferencesAsync`: elke verwijzing buiten de klasse moet `typeof(D<>)` als eerste argument van `RegisterGenericDecorator` zijn |
| Notification | `RegistersMediatRInAutofacAsync` | `Populate(... "Mediator.Internals" ...)` of `RegisterAssemblyTypes(typeof(<MediatR-type>)...)` ergens in de solution |
| Void | `RewriteVoid` | Geschreven gedecoreerde typen → `IRequestHandler<T, Unit>`; gebruik moet `Handle(...)` of toewijzing zijn |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Niet gevonden | decorator van handler met response | `Find_DecoratorWithResponse_LeavesItToTheHandlerPatterns` |
| Niet gevonden (blijft `mediatr.unsupported`) | geen gedecoreerde parameter | `IsDecorator_OpenGenericHandlerWithoutDecoratedParameter_False` |
| Manual "used other than in RegisterGenericDecorator" | andere verwijzing | `used-elsewhere` |
| Manual "every notification" | notification, geen Autofac-registratie van MediatR | `notification-without-autofac` |
| Fixed (ongewijzigd) | notification, MediatR in Autofac | `_Combined/generic-decorators-kgrzybek` |
| Manual "declares members of its own" | eigen interface met members | `decorated-members` |
| Manual "used other than to call Handle" | ander gebruik van de gedecoreerde handler | `decorated-other-use` |
| Fixed: `IRequestHandler<T, Unit>` + `typeof(IRequestHandler<,>)` | void-decorator, alleen geregistreerd | `_Combined/generic-decorators-kgrzybek` |

## Zie ook

- [mediatr-generic-decorator-plan.md](mediatr-generic-decorator-plan.md)
- [mediatr-generic-decorator-verify.md](mediatr-generic-decorator-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/GenericDecorator/`
