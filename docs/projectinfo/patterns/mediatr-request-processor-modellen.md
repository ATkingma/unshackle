# Modellen: mediatr.request-processor

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs` en `MediatRSyntax.cs`.

## Figuur 1: klassen

**Figure 1.** `RequestProcessorPattern`, zijn helper en de MediatR- en Mediator-typen.

<!-- kingmadoc:diagram img/mediatr-request-processor-modellen-1.d2 -->
![Figuur 1: klassen](img/mediatr-request-processor-modellen-1.png)

| Element | Rol |
| ------- | --- |
| `RequestProcessorPattern` | Vindt klassen die `IRequestPreProcessor<>` of `IRequestPostProcessor<,>` zelf declareren en herschrijft ze |
| `MediatRSyntax` | `DeclaredInterface`, `RejectExplicit`, `WithCompletedValueTask`, `AsHandleOverride`, `WithBaseClass`, `CommaList`, `WithConstraint` |
| `MessagePreProcessor<,>` / `MessagePostProcessor<,>` | Mediator-basisklassen met `protected override ValueTask Handle(...)` |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `Rewrite` naar `Fixed` of `Manual`.

<!-- kingmadoc:diagram img/mediatr-request-processor-modellen-2.d2 -->
![Figuur 2: sequence](img/mediatr-request-processor-modellen-2.png)

| Stap | Methode in de code | Wat gebeurt er |
| ---- | ------------------ | -------------- |
| Vinden | `MediatRSyntax.DeclaredInterface` (`IRequestPreProcessor\`1`, `IRequestPostProcessor\`2`) | Alleen klassen die de interface zelf declareren |
| Body | `RejectExplicit`, `WithCompletedValueTask` | `Task.CompletedTask` → `ValueTask.CompletedTask`, andere `Task` → `new ValueTask(expr)`, of async |
| Response | `ResponseOf`, `HasOtherUses`, `IsOpenRegistration` | post: `TRes`; open pre: nieuwe typeparameter `TResponse`; gesloten pre: `T` van `IRequest<T>` of `Unit` |
| Basisklasse | `AsHandleOverride`, `WithBaseClass`, `WithConstraint` | `MessagePre/PostProcessor<TReq, TRes>`, `IMessage` op een typeparameter-request |
| Fix | `IFix.AllOrNothing` | Eén Manual in de solution: niets geschreven |

## Figuur 3: beslisboom

**Figure 3.** Vragen van `Rewrite`.

<!-- kingmadoc:diagram img/mediatr-request-processor-modellen-3.d2 -->
![Figuur 3: beslisboom](img/mediatr-request-processor-modellen-3.png)

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Manual "Abstract or both" | pre en post tegelijk, abstract of static | geen proof |
| Manual "not declared in this class" / explicit | `Process` in een basisklasse of expliciet | geen proof |
| Fixed `new ValueTask(expr)` | andere `Task` (`Task.Delay`, `Task.Run`) zonder async | `RequestProcessor/other-return`, `RequestProcessor/task-run` |
| Manual "TResponse ... used outside AddMediatR" | open pre-processor met `TResponse` al bezet of ander gebruik | `RequestProcessor/used-elsewhere` |
| Manual "response type ... unknown" | gesloten pre-processor zonder `IRequest`/`IRequest<T>` | geen proof |
| Manual "already has a base class" | eigen basisklasse | geen proof |
| Fixed | anders | `pre-open`, `post-open-async`, `pre-closed` |

## Zie ook

- [mediatr-request-processor-plan.md](mediatr-request-processor-plan.md)
- [mediatr-request-processor-verify.md](mediatr-request-processor-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/RequestProcessor/`
