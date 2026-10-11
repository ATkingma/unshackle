# Modellen: mediatr.exception-handler

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/ExceptionHandlerPattern.cs` en `MediatRSyntax.cs`.

## Figuur 1: klassen

**Figure 1.** `ExceptionHandlerPattern`, zijn helper en de MediatR- en Mediator-typen.

<!-- kingmadoc:diagram img/mediatr-exception-handler-modellen-1.d2 -->
![Figuur 1: klassen](img/mediatr-exception-handler-modellen-1.png)

| Element | Rol |
| ------- | --- |
| `ExceptionHandlerPattern` | Vindt klassen die `MediatR.Pipeline.IRequestExceptionHandler<,,>` zelf declareren en herschrijft ze |
| `MediatRSyntax` | `DeclaredInterface`, `RejectExplicit`, `IsTaskMember`, `AsHandleOverride`, `WithBaseClass`, `CommaList`, `WithConstraint` |
| `MessageExceptionHandler<,,>` | Mediator-basisklasse met `Handled(x)` en `NotHandled` |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `Rewrite` naar `Fixed` of `Manual`.

<!-- kingmadoc:diagram img/mediatr-exception-handler-modellen-2.d2 -->
![Figuur 2: sequence](img/mediatr-exception-handler-modellen-2.png)

| Stap | Methode in de code | Wat gebeurt er |
| ---- | ------------------ | -------------- |
| Vinden | `MediatRSyntax.DeclaredInterface` (`IRequestExceptionHandler\`3`) | Alleen klassen die de interface zelf declareren |
| `state` | `GetSymbolInfo` per identifier | Elk gebruik moet `state.SetHandled(x);` direct gevolgd door een `return` zijn |
| Herschrijven | `ReplaceNodes`, `Result`, `AsHandleOverride` | `return new(Handled(x))` / `new(NotHandled)`, `state`-parameter weg, `protected override ValueTask<ExceptionHandlingResult<TRes>>` |
| Basisklasse | `WithBaseClass`, `WithConstraint` | `MessageExceptionHandler<TReq, TRes, TEx>`, `IMessage` op een typeparameter-request |
| Fix | `IFix.AllOrNothing` | Eén Manual in de solution: niets geschreven |

## Figuur 3: beslisboom

**Figure 3.** Vragen van `Rewrite`.

<!-- kingmadoc:diagram img/mediatr-exception-handler-modellen-3.d2 -->
![Figuur 3: beslisboom](img/mediatr-exception-handler-modellen-3.png)

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Manual "Abstract exception handler or Handle not declared" | abstract of `Handle` in een basisklasse | geen proof |
| Manual "explicit interface" | `Handle` expliciet geïmplementeerd | geen proof |
| Manual "async or has no statement body" | `async`, expression body of niet 4 parameters | geen proof |
| Manual "state is used other than" | `state` doorgegeven of opgeslagen | `ExceptionHandler/state-passed` |
| Manual "not Task.CompletedTask" | andere return | geen proof |
| Manual "already has a base class" | eigen basisklasse | geen proof |
| Fixed | anders | `ExceptionHandler/set-handled` |

## Zie ook

- [mediatr-exception-handler-plan.md](mediatr-exception-handler-plan.md)
- [mediatr-exception-handler-verify.md](mediatr-exception-handler-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/ExceptionHandlerPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/ExceptionHandler/`
