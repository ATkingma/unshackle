# TAAK-191 — MediatR: één `RequestHandlerDelegate`-mock gedeeld door meerdere requesttypen

**Status**: open
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`, Herhaling na TAAK-180..184)
**Doel**: amantin migreert volledig (build + 85/85).

## Context voor de volgende agent

- amantin (`5822811`, MediatR 12.2.0), enige root-Manual na TAAK-180: `tests/CleanArchitecture.Application.UnitTests/Common/Behaviors/AuthorizationBehaviorTests.cs:9`, `mediatr.request-handler-delegate`: "_mockNextBehavior is not handed to exactly one request type of a behavior's Handle".
- Het veld `RequestHandlerDelegate<ErrorOr<Response>> _mockNextBehavior` gaat naar `AuthorizationBehavior<RequestWithNoAuthorizationAttribute, …>`, `<RequestWithSingleAuthorizationAttribute, …>` en `<RequestWithTonsOfAuthorizationAttribute, …>`; alle drie zijn records die `IAuthorizeableRequest<ErrorOr<Response>>` implementeren.
- `ValidationBehaviorTests` (één requesttype) wordt al wel gedekt.

## Analyse

- Optie A: veldtype `MessageHandlerDelegate<TCommon, R>` met `TCommon` = meest specifieke gemeenschappelijke basis (hier `IAuthorizeableRequest<ErrorOr<Response>>`, anders `IMessage`). Werkt alleen als `TMessage` in Mediator's delegate contravariant (`in`) is en alle requesttypen referentietypen zijn: eerst controleren in Mediator.Abstractions 3.x.
- Optie B: per aanroep een adapter-lambda `(m, ct) => _mockNextBehavior(m, ct)`; vereist wel een veldtype waarop alle drie passen, dus dezelfde vraag.
- `Invoke(Arg.Any<TCommon>(), Arg.Any<CancellationToken>())` blijft voor alle aanroepen matchen.
- Geen gemeenschappelijke basis of waardetypen → Manual blijft (huidig gedrag).

## Stappen

1. [ ] Variantie van `MessageHandlerDelegate` controleren (probe of metadata).
2. [ ] Proof `Proofs/MediatR/RequestHandlerDelegate/` met één veld en twee requesttypen.
3. [ ] Implementatie in `RequestHandlerDelegatePattern` + docs.
4. [ ] Bewijs: harnas op amantin → Fixed, build groen, 85/85.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs`
- `claude/tasks/archief/TAAK-180-mediatr-test-request-handler-delegate.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
