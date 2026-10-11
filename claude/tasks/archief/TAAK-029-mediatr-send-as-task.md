# TAAK-029 — mediatr.send-as-task

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `Send`/`Publish` van MediatR waarvan het resultaat als `Task` gebruikt wordt → `.AsTask()` (Mediator 3.0.2 geeft `ValueTask`).

## Context voor de volgende agent

- Rij 6 in `claude/tasks/mediatr-queue.md`. Voorbeelden: `samples/SampleShop/SampleShop/Features/Orders/OrderFacade.cs`, `Func<Task> act = () => Sender.Send(...)` in de testprojecten.

## Analyse

- Mediator 3.0.2 (`Mediator.xml`): `ISender.Send<T>(IRequest<T>, ct)` → `ValueTask<T>`, `Send(object, ct)` → `ValueTask<object?>`, `IPublisher.Publish` → `ValueTask`, `CreateStream` → `IAsyncEnumerable<T>` (zoals MediatR).
- Na de namespace-swap compileert `await sender.Send(...)` ongewijzigd; elk ander gebruik (return als `Task`, `Func<Task>`, `Task.WhenAll`, `.ContinueWith`, `.Wait()`) niet of anders.

## Keuzes

- **Alles behalve een directe `await` krijgt `.AsTask()`** (via `GetSymbolInfo`: methode `Send`/`Publish` uit assembly `MediatR`). Eenvoudiger en veiliger dan per gebruik het geconverteerde type beoordelen: `.AsTask()` geeft exact het oude Task-gedrag, ook voor `var t = ...` die vaker ge-await wordt.
- **`await x.ConfigureAwait(...)` en `await (x)` blijven staan**: `ValueTask` heeft `ConfigureAwait`.
- **`CreateStream` niet**: returntype gelijk.
- **Nooit Manual**.

## Stappen

- [x] 1. Claim rij 6.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-send-as-task-plan.md` (approved).
- [x] 3. `SendAsTaskPattern`, registratie in `MediatRFix`.
- [x] 4. Proofs `task-uses`, `send-object`.
- [x] 5. Modellen, verify, README-rij, queue-rij.

## Resultaat

- `dotnet build` 0 warnings, `dotnet test` 458/458, sample-tests 38/38.
- Scan SampleShop: send-as-task 5/0.

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-037-mediatr-unsupported.md`
- `docs/projectinfo/patterns/mediatr-send-as-task-plan.md`
