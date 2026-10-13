# TAAK-212 — kgrzybek bouwt niet na `fix --fix mediatr` (MSG0001, SA1210, CS1503, CS0122)

**Status**: ✅ voltooid (de vier foutsoorten); build kgrzybek nog rood op een volgende laag → TAAK-213
**Aangemaakt**: 2026-10-13 (uit TAAK-210, corpus-ronde 4, `claude/tasks/CORPUSRUN-mediatr.md`)
**Afgerond**: 2026-10-13
**Doel**: kgrzybek `91c8ef2` migreert volledig: 0 Manual (gehaald), build ok, tests 246/287 → 246/287.

## Context

Na TAAK-210 (`_results/r4/kgr`): 217 Fixed, 0 Manual, build faalt op 88× MSG0001, 26× SA1210, 6× CS1503, 4× CS0122.

## Stappen

- [x] 1. Per foutsoort een repro (MSG0001: twee mini-projecten in scratch + gedecompileerde generator; rest: proof/unittest).
- [x] 2. MSG0001: valse melding van Mediator 3.0.2 → `WarningsNotAsErrors`.
- [x] 3. SA1210: toegevoegde usings in een gesorteerde lijst op alfabetische plek.
- [x] 4. CS1503 en CS0122.
- [x] 5. Bewijs: harnas op kgrzybek (`_results/r5/kgr`): de vier foutsoorten weg; nieuwe laag (CS0436/CS0121) → TAAK-213.

## Bevindingen en keuzes

| Fout | Oorzaak | Keuze |
| ---- | ------- | ----- |
| MSG0001 (88×) | Mediator 3.0.2 (`CompilationAnalyzer.ProcessInterface`, gedecompileerd) loopt `AllInterfaces` van een handler af. Voor een void-handler uit een **ander project** achter een eigen marker (`ICommandHandler<T> : IRequestHandler<T>`) telt hij `IRequestHandler<T>` en `IRequestHandler<T, Unit>` als twee handlers: MSG0001 (severity Warning, onder TWAE een error). Het genoemde "message type" is de handler zelf. De handler wordt wel geregistreerd (gecontroleerd in de gegenereerde `Mediator.g.cs`). Decorators spelen geen rol; in hetzelfde project geen melding. | `MSG0001` naast MSG0003/MSG0005 in `WarningsNotAsErrors` van `Mediator.SourceGenerator` (`MediatRFix`, `MediatRFixPackageTests`). Geen Manual: het gedrag is gelijk. |
| CS1503 (6×) | `RequestHandlerPattern` wikkelde `return _decorated.Handle(...)` in `new ValueTask<T>(...)`; na de wissel is dat al een `ValueTask<T>`. | Een return van `Handle(...)` op een (afgeleide) `IRequestHandler<,>` blijft staan. Proof `RequestHandler/decorator-inner-handle`. |
| CS0122 (4×) | De generator noemt elk message dat hij ziet, ook een internal domain event zonder handler (`MeetingGroupProposalRejectedDomainEvent` in Administration.Domain). | `InternalHandlerAccessPattern` geeft ook projecten met alleen internal messages `InternalsVisibleTo` naar de host. Proof `_Combined/internal-message-access`. |
| SA1210 (26×) | Nieuwe `using Mediator;` werd achteraan toegevoegd. | `UsingDirectives`: na het toevoegen wordt een lijst die gesorteerd was (System eerst of niet, minstens 2 usings) opnieuw gesorteerd; trivia blijft per positie. 11 bestaande proofs aangepast (`using System.Linq;` e.d. nu op hun plek). |

## Resultaat

- `dotnet build` ok; `dotnet test` 1062/1062 groen (+3 `MediatRFixPackageTests`, +2 proofs).
- Corpus kgrzybek (`_results/r5/kgr`): 218 Fixed, 0 Manual, 0× MSG0001/SA1210/CS1503/CS0122; build nog rood op 410× CS0436 + 2× CS0121 in
  `Registrations.Infrastructure`, dat naar `UserAccess.Infrastructure` (ook een generator-host) verwijst → TAAK-213.

## Zie ook

- `claude/tasks/TAAK-213-mediatr-host-verwijst-naar-host.md`
- `claude/tasks/archief/TAAK-210-mediatr-generieke-decorator-zonder-using.md`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Herhaling ronde 4" en "Herhaling ronde 5"
- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/patterns/mediatr-request-handler-plan.md`, `docs/projectinfo/patterns/mediatr-internal-handler-access-plan.md`
