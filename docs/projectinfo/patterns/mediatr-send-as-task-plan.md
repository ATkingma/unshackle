---
kingmadoc: 1
feature: "mediatr-send-as-task"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/SendAsTaskPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/SendAsTask/
---

# Feature: mediatr.send-as-task: Send/Publish als Task → .AsTask()

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-029.

## One-sentence summary

mediatr.send-as-task: een `Send`/`Publish` van MediatR waarvan het resultaat niet direct ge-`await` wordt, krijgt `.AsTask()`, omdat Mediator `ValueTask` teruggeeft.

## Scope (in / out)

**In scope**

- `ISender`/`IPublisher`/`IMediator` `.Send(...)` en `.Publish(...)`, ook `Send(object)`/`Publish(object)`, als het symbool uit assembly `MediatR` komt.
- Elk gebruik dat geen directe `await` is: return in een Task-methode, `Func<Task>`-lambda, argument van `Task.WhenAll`, `.ContinueWith`, toekenning.

**Out of scope**

- `await x.Send(...)` en `await x.Send(...).ConfigureAwait(...)`: werken ongewijzigd op `ValueTask`.
- `CreateStream`: geeft in beide bibliotheken `IAsyncEnumerable<T>`.

## Requirements

- **REQ-1**: WHEN `Send`/`Publish` als Task gebruikt wordt (return, lambda, `Task.WhenAll`, `.ContinueWith`) THE SYSTEM SHALL `.AsTask()` toevoegen (proof `task-uses`).
- **REQ-2**: WHEN het resultaat direct ge-`await` wordt, ook met `.ConfigureAwait(...)` THE SYSTEM SHALL het laten staan (proof `task-uses`, laatste regel).
- **REQ-3**: WHEN `Send(object)` als Task teruggegeven wordt THE SYSTEM SHALL `.AsTask()` toevoegen (proof `send-object`).
- **REQ-4**: THE SYSTEM SHALL nooit Manual melden: `.AsTask()` behoudt elke Task-semantiek.
- **Uitbreiding review B L4**: IF `Send`/`Publish` met `?.` aangeroepen wordt (`_sender?.Send(x)`) THEN THE SYSTEM SHALL Manual melden: de null-case moet met de hand (proof `null-conditional`). Dit is de enige uitzondering op REQ-4.
- **REQ-5** (TAAK-211): WHEN `Send`/`Publish` de ontvanger is van NSubstitute `Returns`/`ReturnsForAnyArgs` THE SYSTEM SHALL geen `.AsTask()` toevoegen (dat configureert niets, NS1000) maar de waarden meeverhuizen: `Task.FromResult(x)` → `ValueTask.FromResult(x)` (ook met typeargument of gekwalificeerd), `Task.CompletedTask` → `ValueTask.CompletedTask` (alleen `Publish`/niet-generiek); elke andere waarde (lambda, variabele, `Task.CompletedTask` op een void-`Send`, dat in Mediator `ValueTask<Unit>` geeft) → Manual (proofs `nsubstitute-returns`, `nsubstitute-returns-lambda`). Tweede uitzondering op REQ-4.
- **REQ-6** (TAAK-211): WHEN `Send`/`Publish` de ontvanger is van NSubstitute `Throws*`, of als losse statement achter `Received()`/`DidNotReceive()` staat THE SYSTEM SHALL hem ongemoeid laten (proof `nsubstitute-returns`, unittest `Find_NSubstituteThrowsSetupAndReceivedCheck_FindsNothing`). Moq-`Setup`-lambda's vallen buiten scope.

## Assumptions

- Mediator 3.0.2 `ISender.Send` geeft `ValueTask<T>` (ook `Send(object)` → `ValueTask<object?>`), `IPublisher.Publish` geeft `ValueTask`. Gecontroleerd in `Mediator.xml`.

## Risks

- Ook `var t = sender.Send(...)` krijgt `.AsTask()` (kleine allocatie); dat is veilig, want `t` blijft een Task.

## Modellen

Klassendiagram, sequence en beslisboom: [mediatr-send-as-task-modellen.md](mediatr-send-as-task-modellen.md).

## Open questions

- Geen.
