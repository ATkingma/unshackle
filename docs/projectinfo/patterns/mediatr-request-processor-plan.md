---
kingmadoc: 1
feature: "mediatr-request-processor"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/RequestProcessor/
---

# Feature: mediatr.request-processor: IRequestPreProcessor / IRequestPostProcessor → MessagePreProcessor / MessagePostProcessor

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-061.

## One-sentence summary

Een klasse die zelf `IRequestPreProcessor<TReq>` of `IRequestPostProcessor<TReq, TRes>` implementeert wordt een afgeleide van `MessagePreProcessor<TReq, TRes>` / `MessagePostProcessor<TReq, TRes>`; `public Task Process(...)` wordt `protected override ValueTask Handle(...)`.

## Scope (in / out)

**Out of scope**: Abstracte processors, processors met een eigen basisklasse, of klassen die zowel pre als post zijn: Manual. Afgeleiden van een eigen basis-processor worden niet gevonden (de basis is Manual, dus alles-of-niets houdt de solution heel).

## Requirements

- **REQ-1**: WHEN een open pre-processor `Task.CompletedTask` teruggeeft THE SYSTEM SHALL een tweede typeparameter `TResponse` toevoegen, de basisklasse en `IMessage`-constraint zetten en `ValueTask.CompletedTask` schrijven (proof `pre-open`).
- **REQ-2**: WHEN een post-processor `async` is THE SYSTEM SHALL alleen basisklasse, modifiers, naam en returntype aanpassen (proof `post-open-async`).
- **REQ-3**: WHEN een gesloten pre-processor een request met `IRequest<T>` of `IRequest` heeft THE SYSTEM SHALL `T` resp. `Unit` als response gebruiken (proof `pre-closed`).
- **REQ-4**: WHEN `Process` zonder async een andere `Task` teruggeeft (`Task.Delay(...)`, `Task.Run(...)`, expression body of blok) THE SYSTEM SHALL `return new ValueTask(expr)` schrijven; synchrone exception-timing blijft gelijk (proofs `other-return`, `task-run`; TAAK-184). IF iets teruggegeven wordt dat geen `Task` is THEN Manual.
- **REQ-5**: IF een open pre-processor al een typeparameter `TResponse` heeft THEN THE SYSTEM SHALL Manual melden. Gebruik van de klasse elders (`new P<Cmd>()`, `Process`-aanroepen) volgt `mediatr.request-processor-caller` (TAAK-190); de oude vangnetproof `used-elsewhere` is daarmee vervallen.

## Assumptions

- Mediator 3.0.2 (ilspy op de echte assembly): `abstract class MessagePreProcessor<TMessage, TResponse> where TMessage : notnull, IMessage` met `protected abstract ValueTask Handle(TMessage, CancellationToken)`; `MessagePostProcessor` idem met `Handle(TMessage, TResponse, CancellationToken)`.
- De registratie volgt in `mediatr.add-mediatr`; de fix is alles-of-niets (`IFix.AllOrNothing`), dus een Manual elders laat dit patroon niets schrijven.

## Modellen

Klassen, sequence en beslisboom: [mediatr-request-processor-modellen.md](mediatr-request-processor-modellen.md).

## Zie ook

- [mediatr-request-processor-verify.md](mediatr-request-processor-verify.md)
- `claude/tasks/archief/TAAK-061-*.md`
