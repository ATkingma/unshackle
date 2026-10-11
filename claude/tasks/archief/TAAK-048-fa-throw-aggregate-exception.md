# TAAK-048 — fa.throw/fa.throw-async: FA pakt AggregateException uit, Shouldly niet

**Status**: voltooid (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: TAAK-046 (semantische audit)

## Probleem

FA 7 `Throw<T>()` en `ThrowAsync<T>()` slagen ook als de actie een `AggregateException` gooit met een `T` erin (FA pakt die uit). `Should.Throw<T>` / `Should.ThrowAsync<T>` in Shouldly 4.3.0 doen dat niet.

Gemeten (`scratchpad/semprobe`):

- `Action` gooit `AggregateException(new Derived())`: `act.Should().Throw<Derived>()` PASS, `Should.Throw<Derived>(act)` FAIL.
- `Func<Task>` idem: `ThrowAsync<Derived>()` PASS, `Should.ThrowAsync<Derived>(f)` FAIL.

Typisch bij code die `Task.Wait()`/`.Result` of `Parallel.ForEach` gebruikt. Gevolg: groene test wordt rood. Statisch niet te zien welk type daadwerkelijk wordt gegooid.

## Wat, waar, hoe (opties)

- **Waar**: `ThrowPattern.cs`, `ThrowAsyncPattern.cs` (`src/Unshackle.Core/Fixes/FluentAssertions/Patterns/`).
- **Optie A (aanbevolen)**: documenteren als bekende afwijking in de KingmaDoc plan/verify + rapportregel (Fixed blijft), omdat een rood worden zichtbaar is en het patroon zeldzaam is. Geen code-wijziging behalve docs.
- **Optie B**: Manual wanneer de actie-body syntactisch `.Wait(`, `.Result`, `WaitAll`, `Parallel.` bevat (heuristiek, mist indirecte gevallen).
- **Waarom niet altijd Manual**: dan valt het grootste patroon weg voor een randgeval.
- Beslissing ligt bij de gebruiker.

## Stappen

- [x] Keuze A/B vastleggen (B)
- [x] Bij B: proofs `aggregate-*` in `Throw/` en `ThrowAsync/` (manual + fixed)
- [x] Docs `docs/projectinfo/patterns/` fa.throw + fa.throw-async bijwerken (Deviations)
- [x] `dotnet build` + `dotnet test` groen

## Resultaat

`ThrowMessage.MayThrowAggregate` (gedeeld door beide patronen) volgt lambda, lokale delegate-variabele (ook via method group) of method group en zoekt via symbolen `Task.Wait`/`WaitAll`/`Task<T>.Result`/`Parallel.For/ForEach/Invoke`; accepteert `T` geen `AggregateException` → Manual. Probe (`scratchpad/aggprobe`, FA 7.2.2/Shouldly 4.3.0): Wait/Result/WaitAll/Parallel.* FA PASS, Shouldly FAIL; `GetAwaiter().GetResult()` beide PASS (pakt zelf uit, daarom bewust niet Manual); `WaitAny` gooit niet; `Throw<AggregateException>`/`Throw<Exception>` Shouldly PASS.

## Zie ook

- `claude/tasks/TAAK-046-semantische-audit.md`

## Besluit gebruiker (2026-10-11)

Optie B: heuristiek. Kan de actie een `AggregateException` opleveren (`Task.Wait`, `.Result`, `.GetAwaiter().GetResult()`, `Parallel.*`, `Task.WaitAll`) → Manual met uitleg; anders gewoon omzetten. Start na TAAK-047 (zelfde patronen).
