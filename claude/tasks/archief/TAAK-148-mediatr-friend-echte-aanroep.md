# TAAK-148 — MediatR: InternalsVisibleTo alleen Manual bij echte aanroep

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/archief/TAAK-147-mediatr-restpunten-144.md` (Niet gedaan, `shortcut:` in `AddMediatRPattern.Friend`)

## Opdracht

TAAK-147 maakte een `internal` registratiemethode met gescande assembly-parameter Manual zodra het project énig `InternalsVisibleTo` had. Nu alleen Manual als een ander project de methode echt aanroept, en de melding noemt dat project (met bestand:regel).

## Keuze

- Nieuwe interface `Core/Abstractions/ISolutionPattern : IPattern` met `RewriteAsync(node, model, solution, cancellationToken)`. De Fixer roept die aan als een pattern hem implementeert (anders `Rewrite`); `ApplyPattern`/`Rewrite` in `Fixer` zijn async geworden. Geen default interface member op `IPattern`: NSubstitute-mocks van `IPattern` zouden die overschrijven.
- `AddMediatRPattern` implementeert hem: bij een friend zoekt hij met `SymbolFinder.FindReferencesAsync` aanroepen van de omsluitende methode in projecten met een andere assembly-naam (multi-target kopieën tellen dus niet). Het synchrone `Rewrite` (zonder solution) blijft voorzichtig: elke friend "may call".
- Core blijft zonder projectverwijzingen (`SymbolFinder` zit in Workspaces, al een package van Core).

## Stappen

- [x] Code: `ISolutionPattern`, `Fixer`, `AddMediatRPattern`
- [x] Proof `AddMediatR/edge-assemblies-friend`: manual → after (geen aanroeper in een één-project-proof)
- [x] Test `AddMediatRPatternTests.RewriteAsync_InternalMethodWithFriend_ManualOnlyWhenOtherProjectCallsIt` (twee projecten, met/zonder aanroep)
- [x] Pattern-docs plan/verify, `proofs.md` geregenereerd
- [x] `dotnet build`, `dotnet test` (740/740), samples 38/38
- [x] Harnas op `/mnt/dikkeschijf/unshackle-corpus/src/evo`, uitvoer `/mnt/dikkeschijf/unshackle-corpus/t148/out/`:
      evo1 `ok` 45/45 → 45/45, 0 regressies, **blijft terecht Manual**: `Fitnet.IntegrationTests/Common/TestEngine/Configuration/ConfigurationExtensions.cs:43` roept `AddInMemoryEventBus(Assembly.GetExecutingAssembly())` aan, en `FakeEventConsumer` (handler) leeft in dat testproject. De melding noemt nu dat project en die regel.
      evo2 `ok` 39/39 → 39/39, 0 regressies, Manual zoals in TAAK-147 (public methode).

## Niet gedaan

- Alleen `AddMediatRPattern` gebruikt `ISolutionPattern`; andere patterns hebben het niet nodig.
- Aanroepen via reflectie worden niet gezien (geen referentie in de solution).

## Zie ook

- `claude/tasks/archief/TAAK-147-mediatr-restpunten-144.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/REGRESSIE-corpus.md`
