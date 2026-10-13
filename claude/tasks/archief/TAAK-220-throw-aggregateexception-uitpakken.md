# TAAK-220 — `Throw<T>()` op een delegate die via `Wait()`/`WaitAsync` een `AggregateException` gooit → testregressie

**Status**: ✅ voltooid (2026-10-13)
**Aangemaakt**: 2026-10-13 (FA-corpusrun ronde 4)
**Doel**: geen testregressies meer bij `Invoking(..).Should().Throw<T>()` waarbij de delegate een `AggregateException` gooit die FA uitpakt en Shouldly niet.

## Context voor de volgende agent

- Akka.NET `23a8566`, `Akka.Streams.Tests`: 9 regressies (ronde 4, `<corpus>/corpus-fa/_results/r4/akka/compare.json`).
- FA 7 `Throw<T>()` pakt een `AggregateException` uit en zoekt `T` in de inner exceptions; Shouldly `Should.Throw<T>` niet. Met `T = Exception` slaagt Shouldly wel, maar geeft het de `AggregateException` terug, dus `.Should().Be(Ex)` of `.Message` faalt daarna.
- De bestaande Manual-regel in `fa.invoking` ("The action blocks on a task … Wait/Result/WaitAll") ziet het niet als:
  - de receiver de lambda-parameter is: `task.Invoking(_ => _.Wait(TimeSpan.FromSeconds(3))).Should().Throw<Exception>().And.Should().Be(Ex)` (QueueSourceSpec ×5), `future.Invoking(f => f.Wait()).Should().Throw<Exception>().WithMessage(..)` (FlowWatchTerminationSpec);
  - de delegate een lokale variabele is: `fail.Should().Throw<TestException>()` (FlowScanSpec), `action.Should().Throw<Exception>()` (FlowSupervisionSpec);
  - het een eigen blokkerende methode is: `p.Awaiting(t => t.WaitAsync(..)).Should().ThrowAsync<TestException>()` (SinkForeachAsyncSpec; de `WaitAsync` van Akka gooit een `AggregateException`).

## Stappen

- [x] 1. Detectie uitbreiden: `Wait`/`Result`/`GetAwaiter().GetResult()` ook op lambda-parameters; bij een delegate in een lokale de initializer volgen (zoals TAAK-070).
- [x] 2. Niet te analyseren delegate (eigen methode, parameter, veld): Manual of Fixed met waarschuwing "FluentAssertions unwraps AggregateException". Met de gebruiker afstemmen welke van de twee.
- [x] 3. Unittests + proofs (`Proofs/FluentAssertions/Throw/aggregate-*`).
- [x] 4. Akka-herhaling: Akka.Streams.Tests 0 regressies.

## Resultaat

Corpus ronde 5 (`CORPUSRUN-fa.md`): Akka 1 regressie (alleen TAAK-200), Steeltoe 0, LiteDB 0. Build, `dotnet test` (1093) en sample-tests (38) groen.

## Keuzes

- **Probe** (FA 7.2.2 / Shouldly 4.3.0, net10.0, `<probe-dir>/probe220`): `Should.Throw<Exception>(() => t.Wait())` geeft de `AggregateException`, FA `Throw<Exception>()` de inner `TestEx`. FA pakt ook bij `Throw<SystemException>` uit; alleen `T` = `AggregateException` (of afgeleid) houdt de aggregate. Shouldly `ThrowAsync<TestEx>` op een task met een geneste `AggregateException(TestEx)` faalt, FA vlakt af en slaagt.
- **Lambda-parameter als receiver** (`_ => _.Wait(..)`) werd al gezien; de echte oorzaak in QueueSourceSpec/FlowWatchTermination was `T = Exception`: dat accepteert `AggregateException`, dus de oude regel liet het door. Nu: is `T` een basis van `AggregateException`, dan Manual zodra de keten de exception gebruikt (`.And`, `.Which`, een stap, `var ex = …`); kale `Throw<Exception>()` blijft Fixed (beide slagen).
- **Delegate in een lokale** werd al gevolgd; FlowScan/FlowSupervision blokkeren in een eigen methode (`Scan(..)`, `Run(..)`). `MayThrowAggregate` volgt nu methoden met broncode in de compilatie, twee niveaus diep.
- **`WaitAsync`**: niet statisch te bewijzen (de geneste aggregate ontstaat in Akka). Gekozen voor Manual bij een lambda `x => x.WaitAsync(..)` met `T` geen `AggregateException`: de task komt van elders. Manual i.p.v. Fixed+waarschuwing, omdat Fixed de test laat falen.
- Niet te volgen delegates (parameter, veld) blijven Fixed zoals voorheen (geen bewijs van blokkeren; anders te veel Manual).
- Proofs: `Throw/aggregate-helper-method`, `aggregate-exception-and`, `aggregate-exception-plain`; `Invoking/aggregate-parameter-and`, `aggregate-parameter-message`, `awaiting-wait-async`.

## Zie ook

- `claude/tasks/archief/TAAK-070-fa-assertion-in-throw-lambda.md`
- `claude/tasks/CORPUSRUN-fa.md` (ronde 4)
- `claude/tasks/patroon-queue.md`
