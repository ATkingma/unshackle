# TAAK-191 — MediatR: één `RequestHandlerDelegate`-mock gedeeld door meerdere requesttypen

**Status**: ✅ voltooid
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

## Keuzes

- Probe (Mediator.Abstractions 3.0.2): `MessageHandlerDelegate<TMessage, TResponse>` is invariant; `MessageHandlerDelegate<IA<int>,int>` is niet toewijsbaar aan `<R1,int>` (CS0029), een lambda `(m, ct) => f(m, ct)` wel.
- Optie A+B gecombineerd: veld/mock krijgt `MessageHandlerDelegate<IMessage, TRes>`, elk `next`-argument van een `Handle` wordt een adapter-lambda. Gemeenschappelijk type is altijd `IMessage` (niet de meest specifieke basis): altijd geldig, ook bij waardetypen (boxing) en zonder constraint-/toegankelijkheidsrisico.
- Eén request-type blijft het oude pad (geen lambda). Geen `Handle`-aanroep: Manual met nieuwe tekst.
- Lambda-parameternamen worden vrij gekozen (`_`-suffix bij botsing).

## Stappen

1. [x] Variantie gemeten (probe `<probe-dir>/probe-191`, opgeruimd).
2. [x] Proof `_Combined/request-handler-delegate-shared-field` (before/after).
3. [x] Implementatie in `RequestHandlerDelegatePattern` + plan/modellen/verify.
4. [ ] Harnas op amantin niet gedraaid (corpusproject niet aanwezig); nabootsing via de proof.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs`
- `claude/tasks/archief/TAAK-180-mediatr-test-request-handler-delegate.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
