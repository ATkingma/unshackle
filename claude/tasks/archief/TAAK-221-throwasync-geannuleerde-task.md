# TAAK-221 — `ThrowAsync`/`ThrowExactlyAsync` op een geannuleerde task → Shouldly geeft een eigen `TaskCanceledException`

**Status**: ✅ voltooid (2026-10-13)
**Aangemaakt**: 2026-10-13 (FA-corpusrun ronde 4)
**Doel**: geen testregressies bij async throw-asserts op `OperationCanceledException`/`TaskCanceledException`.

## Context voor de volgende agent

- Steeltoe `f13e514`: 18 regressies (6 tests × 3 TFM's), `<corpus>/corpus-fa/_results/r4/steeltoe/compare.json`.
- `await action.Should().ThrowExactlyAsync<OperationCanceledException>()` wordt `(await Should.ThrowAsync<OperationCanceledException>(action)).ShouldBeOfType<OperationCanceledException>()`. Dat faalt met "should be of type OperationCanceledException but was TaskCanceledException" in MongoDb/RabbitMQ/Redis/RelationalDatabase `HealthContributorTest.Canceled_Throws` en `DiscoveryHttpDelegatingHandlerTest.DoesNotTrackStatistics_WhenRequestIsCanceled`.
- `ConfigServerConfigurationProviderTest.RemoteLoadAsync_HostTimesOut`: `ThrowAsync<TaskCanceledException>().WithInnerException<TimeoutException>()` → de inner exception is `null`.
- Vermoedelijke oorzaak (stap 1 bevestigt dit): eindigt de task in de status *Canceled* (en niet *Faulted*), dan geeft Shouldly 4.3 `Should.ThrowAsync` een zelf aangemaakte `TaskCanceledException` terug in plaats van de echte exception. Type en inner exception gaan dan verloren. FA awaitet de task en vangt de echte exception.

## Stappen

- [x] 1. Repro in een unittest: een `Func<Task>` die `token.ThrowIfCancellationRequested()` doet, met FA en met Shouldly.
- [x] 2. Rewrite kiezen: Manual als `T` `OperationCanceledException` is of daarvan afgeleid (of als er `WithInnerException` volgt), óf een vorm die de echte exception bewaart (bijv. `Should.Throw<T>(() => action().GetAwaiter().GetResult())`). Afstemmen met de gebruiker.
- [x] 3. Unittests + proofs.
- [x] 4. Steeltoe-herhaling: 0 regressies.

## Resultaat

Corpus ronde 5 (`CORPUSRUN-fa.md`): Akka 1 regressie (alleen TAAK-200), Steeltoe 0, LiteDB 0. Build, `dotnet test` (1093) en sample-tests (38) groen.

## Keuzes

- **Probe bevestigt** (`<probe-dir>/probe220`): eindigt de task *Canceled* (`ThrowIfCancellationRequested`, of een async methode die een OCE/TCE gooit), dan geeft `Should.ThrowAsync<T>` een nieuwe `TaskCanceledException` zonder inner exception terug; FA geeft de echte (`OperationCanceledException`, resp. TCE met inner `TimeoutException`). `Should.Throw<T>(() => act().GetAwaiter().GetResult())` bewaart de echte exception.
- **Gekozen: Manual** als `T` (afgeleid van) `OperationCanceledException` is én er een stap volgt (`WithMessage`/`WithInnerException*`/`Where`) of het `ThrowExactlyAsync` is. Uitzondering: `ThrowExactlyAsync<TaskCanceledException>()` zonder stap blijft Fixed (dat type maakt Shouldly juist). De `GetAwaiter().GetResult()`-vorm is verworpen: die blokkeert synchroon, haalt de `await` weg (CS1998 in async-methoden zonder andere `await`, met warnings-as-errors een bouwfout) en wijkt af van wat de gebruiker schreef.
- `.And.CancellationToken`/`var ex = …` zonder stap blijft Fixed: de nieuwe TCE krijgt het token van de task (proof `ThrowAsync/result-local-and` blijft groen).
- Proofs: `ThrowAsync/cancelled-inner`, `ThrowExactly/async-cancelled` (Manual), `ThrowExactly/async-task-canceled` (Fixed).

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md` (ronde 4)
- `claude/tasks/patroon-queue.md`
