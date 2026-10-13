---
kingmadoc: 1
feature: "mediatr-request-processor-caller"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorCallerPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/RequestProcessorCaller/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/
---

# Feature: mediatr.request-processor-caller: aanroepers van een herschreven request-processor

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (met de hand, kingmadoc ontbreekt) |

> Opgesteld uit TAAK-190 (corpusrun 2026-10-12, jason v8.0.0: 4x CS0305 in `RequestLoggerTests`).

## One-sentence summary

Code die een door `mediatr.request-processor` herschreven processor aanmaakt of aanroept (`new P<Cmd>(...)`, `await p.Process(req, ct)`) wordt meeverhuisd naar Mediator's `P<Cmd, TRes>` en de publieke pipeline-`Handle`; alles wat niet exact vertaalbaar is wordt Manual met regelnummer, nooit een stille buildbreuk.

## Scope (in / out)

**In scope**:
- Elke verwijzing `P<TReq>` (typeargument, `new`, veld, lokale variabele, `Substitute.For<...>`) naar een pre-processorklasse met open berichttype: `TRes` volgt uit `TReq : IRequest<TRes>` (`IRequest` zonder resultaat geeft `Unit`).
- `await p.Process(req, ct);` als losse statement, voor pre- (`Handle(req, static (_, _) => default, ct)`) en post-processors (`Handle(req, (_, _) => new ValueTask<TRes>(res), ct)`).

**Out of scope**: registraties in de `AddMediatR`-lambda (`mediatr.add-mediatr`); `Process`-aanroepen waarvan het resultaat gebruikt wordt (`Task t = ...`, `return ...`, `.Wait()`), mocks van `Process`, aanroepen via de MediatR-interface en open of onherleidbare typeargumenten: Manual.

## Requirements

- **REQ-1**: WHEN code `new P<Cmd>(...)` (of een andere typeverwijzing) schrijft voor een herschreven pre-processor over een open berichttype THE SYSTEM SHALL het response-argument toevoegen (`Cmd : IRequest<int>` geeft `P<Cmd, int>`, `IRequest` geeft `P<Ping, Unit>`) (proof `_Combined/request-processor-callers`).
- **REQ-2**: WHEN een `await p.Process(...)`-statement een processor aanroept THE SYSTEM SHALL `Handle` met een `next`-delegate schrijven die de publieke pipeline-methode aanroept, bij een post-processor met de response als waarde (proof `_Combined/request-processor-callers`).
- **REQ-3**: IF `Process` anders gebruikt wordt dan als losse `await`-statement, of via de MediatR-interface THEN THE SYSTEM SHALL Manual melden (proofs `RequestProcessorCaller/result-used`, `through-interface`).
- **REQ-4**: IF het response-type van het typeargument niet volgt (type zonder `IRequest`, onbeperkte typeparameter) of het argument open is THEN THE SYSTEM SHALL Manual melden (proof `RequestProcessorCaller/unknown-response`).

## Assumptions

- Mediator 3.0.2: `MessagePreProcessor`/`MessagePostProcessor` hebben een publieke `Handle(message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken)`; die roept bij een pre-processor eerst de eigen `Handle` en dan `next`, bij een post-processor eerst `next` en dan de eigen `Handle`. Het `default`-resultaat van `next` is voor de aanroeper onzichtbaar (`await` zonder waarde). Bewezen doordat de `after` van de proof compileert.
- Het patroon staat in `MediatRFix.Patterns` vóór `mediatr.request-processor`: de aanroepers binden dan nog aan de MediatR-klasse. Het patroon zoekt per bestand in de hele solution, dus ook in een testproject.
- De fix is alles-of-niets: een Manual hier laat de hele solution ongemoeid.

## Modellen

[mediatr-request-processor-caller-modellen.md](mediatr-request-processor-caller-modellen.md).

## Zie ook

- [mediatr-request-processor-caller-verify.md](mediatr-request-processor-caller-verify.md)
- [mediatr-request-processor-plan.md](mediatr-request-processor-plan.md)
- `claude/tasks/archief/TAAK-190-mediatr-request-processor-aanroepers.md`
