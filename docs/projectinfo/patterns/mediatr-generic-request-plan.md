---
kingmadoc: 1
feature: "mediatr-generic-request"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/GenericRequestPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/GenericRequest/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/identified-command-eshop.before.cs
---

# Feature: mediatr.generic-request: generiek request → abstracte basis + concrete subklasse per gesloten type

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (met de hand) |

> Opgesteld uit TAAK-193 (dotnet/eShop `IdentifiedCommand<T, R>`, kgrzybek `DomainNotificationBase<T>`).

## One-sentence summary

`class G<T, R> : IRequest<R>` wordt `abstract`, krijgt per gesloten type uit de solution een `class GOfXAndY : G<X, Y>` met dezelfde constructors,
elke gesloten `G<X, Y>` (handlers, `new`, validators, DI-sleutels) wordt `GOfXAndY`, en een concrete handler die `IRequestHandler<G<X, Y>, Y>` van een
abstracte generieke basis erft krijgt `IRequestHandler<GOfXAndY, Y>` met een `Handle` die de basis aanroept.

## Scope (in / out)

**In**: niet-abstracte, niet-statische, top-level generieke klasse in de bron die een MediatR-request is (geen stream-request); een generiek request of
generieke notification die alleen basis is van concrete subklassen (wordt alleen `abstract`).

**Out (Manual)**: record, partial of file-local; `typeof(G<>)`; `new G<TParam>` in generieke code; typeargument dat geen gewoon benoemd type is;
gesloten verwijzing in een override-signatuur; gesloten type dat al een subklasse heeft; naambotsing; gesloten type niet zichtbaar vanuit het project van `G`;
alleen private constructors of constructors met `ref`/`params`/optionele parameters; geërfde `Handle` die expliciet, overridden of niet public is.

## Requirements

- **REQ-1**: WHEN gesloten `G<X, Y>` in de solution staan THE SYSTEM SHALL `G` abstract maken en per gesloten type `{G}Of{X}And{Y}` schrijven (CLR-naam voor speciale typen: `Boolean`), constructors door (proof `closed-uses`, `_Combined/identified-command-eshop`).
- **REQ-2**: THE SYSTEM SHALL elke gesloten verwijzing buiten `G` en buiten de basislijst van een bestaande subklasse hernoemen, met behoud van de qualifier (proof `closed-uses`).
- **REQ-3**: WHEN een concrete handler `IRequestHandler<G<X, Y>, Y>` erft THE SYSTEM SHALL `IRequestHandler<GOfXAndY, Y>` en `Handle(GOfXAndY, ct) => base.Handle(...)` toevoegen (proof `_Combined/identified-command-eshop`).
- **REQ-4**: WHEN een gesloten type alleen in een ander project gebruikt wordt THE SYSTEM SHALL zijn subklasse toch bij `G` schrijven; staat het type zelf in een project dat `G` niet ziet THEN Manual (unittests `RewriteAsync_ClosedTypeOnlyUsedInOtherProject_WritesItsSubclass`, `RewriteAsync_ClosedTypeDeclaredInOtherProject_Manual`).
- **REQ-5**: WHEN een generiek message alleen basis is van concrete subklassen THE SYSTEM SHALL het alleen `abstract` maken (unittest `Find_OnlyBaseOfConcreteSubclass_FindsTheDeclaration`, `_Combined/generic-decorators-kgrzybek`).
- **REQ-6**: IF `typeof(G<>)`, open creatie, een niet-gewoon typeargument of een record THEN Manual (proofs `unbound-typeof`, `open-creation`, `not-plain-argument`, `derived-generic-record`).
- **REQ-7**: THE SYSTEM SHALL bij de declaratie een `Warning` geven: `GetType()` is nu de subklasse (logteksten veranderen, gedrag niet).

## Assumptions

- Gemeten (probe p1, TAAK-193): de generator van Mediator 3.0.2 ziet alleen `G<T, R>` (MSG0005) en dispatcht `G<X, bool>` niet (MissingMessageHandlerException);
  een handler van een basistype wordt voor een afgeleid request níet gebruikt (anders dan bij notifications), dus de handler moet het subtype zelf implementeren.
- Validators en DI-sleutels als `IValidator<G<X, Y>>` moeten mee hernoemd worden, anders valt validatie stil weg.

## Modellen

[mediatr-generic-request-modellen.md](mediatr-generic-request-modellen.md).

## Zie ook

- [mediatr-generic-request-verify.md](mediatr-generic-request-verify.md)
- [mediatr-generic-notification-plan.md](mediatr-generic-notification-plan.md) — zelfde aanpak voor notifications (TAAK-192)
- `claude/tasks/archief/TAAK-193-mediatr-generieke-decorators-messages.md`
