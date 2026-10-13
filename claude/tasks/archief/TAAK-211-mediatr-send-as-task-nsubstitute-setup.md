# TAAK-211 — `mediatr.send-as-task` herschrijft NSubstitute-setups (`mock.Send(...).Returns(...)`) tot `.AsTask().Returns(...)`

**Status**: ✅ voltooid (2026-10-13)
**Aangemaakt**: 2026-10-13 (uit corpus-ronde 3, `claude/tasks/CORPUSRUN-mediatr.md`)
**Doel**: dotnet/eShop `59f1a6c` bouwt na `fix --fix mediatr` (nu 52 Fixed, 0 Manual, maar 12× NS1000 in `tests/Ordering.UnitTests`).

## Context voor de volgende agent

- Ronde 3 (main `9189df4`, libman omzeild met `LibraryRestore=false`): eShop migreert volledig (0 Manual, `IdentifiedCommand<T,R>` → `IdentifiedCommandOf…`),
  maar de build na de fix breekt stil: 12× `error NS1000: Member AsTask can not be intercepted` (NSubstitute.Analyzers, TWAE).
- Voor: `_mediator.Send(Arg.Any<IRequest<bool>>(), default).Returns(Task.FromResult(true));` (`_mediator = Substitute.For<IMediator>()`).
  Na: `_mediator.Send(Arg.Any<IRequest<bool>>(), default).AsTask().Returns(Task.FromResult(true));`
  → `AsTask()` is een niet-virtuele `ValueTask`-methode; de setup configureert niets meer (NS1000, en zonder analyzer runtime fout/stille default).
- Plekken: `tests/Ordering.UnitTests/Application/IdentifiedCommandHandlerTest.cs` (27, 49) en `OrdersWebApiTest.cs` (28, 43, 58, 74).
  `await _mediator.Received().Send(...)` wordt terecht niet aangeraakt.

## Nieuwe situatie (voorstel)

- `SendAsTaskPattern` slaat een `Send/Publish`-aanroep over waarvan het resultaat de ontvanger is van NSubstitute `Returns`/`ReturnsForAnyArgs`/`Throws*`
  (of Moq `Setup`-lambda), en ook aanroepen binnen `Received()`/`DidNotReceive()`.
- Het `Returns`-argument meeverhuizen: `Task.FromResult(x)` → `new ValueTask<T>(x)` (of `ValueTask.FromResult(x)`), `Task.CompletedTask` → `ValueTask.CompletedTask`;
  onbekende vorm → Manual met reden.

## Stappen

- [x] 1. Proofs `SendAsTask/nsubstitute-returns` (eShop-vorm, `ReturnsForAnyArgs` met twee waarden en typeargument, gekwalificeerde `Task`,
  `Publish(...).Returns(Task.CompletedTask)`, `Throws`, `Received().Send`) en `nsubstitute-returns-lambda` (Manual). Unittests `SendAsTaskPatternTests`.
- [x] 2. `SendAsTaskPattern`: een `Send`/`Publish` die ontvanger is van NSubstitute `Returns`/`ReturnsForAnyArgs` → de setup-aanroep wordt de finding;
  `Task.FromResult(x)` → `ValueTask.FromResult(x)`, `Task.CompletedTask` → `ValueTask.CompletedTask` (alleen bij niet-generieke return, dus `Publish`);
  andere waarden → Manual. `Throws*` en een losse `Received()/DidNotReceive().Send(...)`-statement: ongemoeid.
- [x] 3. Docs: send-as-task plan/verify REQ-5/6.
- [x] 4. Bewijs (`_results/r4/eshop`): 52 Fixed, 0 Manual, **build na fix ok** (was 12× NS1000); unit 32/32 → 32/32, 0 regressies
  (6 functionele tests zonder Docker liepen na de fix verder tot de time-out en zijn rood, vóór de fix niet gehaald).

## Keuzes

- `ValueTask.FromResult(x)` i.p.v. `new ValueTask<T>(x)`: behoudt een expliciet typeargument en de qualifier, geen type-naam nodig.
- `Task.CompletedTask` op een void-`Send` → Manual: Mediator geeft daar `ValueTask<Unit>`.
- Moq (`Setup(m => m.Send(...))`) valt buiten scope: geen corpusvoorkomen.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/SendAsTaskPattern.cs`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Herhaling ronde 3"
- `claude/tasks/mediatr-queue.md`
