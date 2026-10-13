# TAAK-224 — FA-tijdextensies `5.Seconds()`, `200.Milliseconds()` → `TimeSpan.FromSeconds(5)`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-13 (FA-corpusrun ronde 4)
**Doel**: `FluentAssertions.Extensions` (`TimeSpanConversionExtensions`) migreren, zodat FA niet alleen hierom in het project blijft.

## Context voor de volgende agent

- Ronde 4: 380 `unmatched-use`-Manuals: `int.Seconds()` 278 (Akka 259, Steeltoe 19), `int.Milliseconds()` 48 (Akka 35, Steeltoe 13) en `double.Seconds()` 54 (Akka). Meestal staan ze als timeout-argument buiten een assert, bijv. `ExpectMsg(.., 3.Seconds())` uit Akka-TestKit.
- Mapping: `n.Seconds()` → `TimeSpan.FromSeconds(n)`, `Milliseconds` → `FromMilliseconds`, en `Minutes`/`Hours`/`Days`/`Ticks` op dezelfde manier. Varianten als `n.Seconds(m)` en `.And(..)` → Manual. Let op de `int`- en `double`-overloads van `TimeSpan.From*` in .NET 9+ (geen gedragsverschil).
- Ook de `DateTime`-extensies (`1.January(2020)`) bekijken; alleen meenemen als ze in het corpus voorkomen.

## Stappen

- [x] 1. Nieuw patroon `fa.timespan-extensions` (KingmaDoc plan + verify, rij in `docs/projectinfo/README.md`).
- [x] 2. Unittests + proofs.
- [x] 3. Akka/Steeltoe-herhaling: 0 unmatched-use voor `Seconds`/`Milliseconds`.

## Keuzes

- Eigen `IPattern` (`TimeSpanExtensionsPattern`, `fa.timespan-extensions`), geen `FluentAssertionPattern`: er is geen `x.Should()`. Staat als laatste in `FluentAssertionsFix.Patterns`.
- `Ticks/Milliseconds/Seconds/Minutes/Hours/Days` met één getal → `TimeSpan.From*(n)`: precies wat FA 7.2.2 doet (gereflecteerd). Haakjes om de ontvanger vallen weg.
- Maanden (EventFlow: 23× `1.October(2020)`) → `new DateTime(y, m, d)`, alleen als dag of jaar constant is (evaluatievolgorde draait om).
- `Seconds(n, offset)`, `And`, `At`, `AsUtc`, Nano/Micro, … → Manual (patroon vindt ze, rapport legt uit).
- `FluentAssertions.Extensions` toegevoegd aan `RemovedNamespaces`, anders bleef de using (en dus het pakket) staan.
- `TimeSpan` niet in scope → `using System` erbij; eigen type `TimeSpan` in scope → `global::System.TimeSpan`.

## Resultaat

Zie `docs/projectinfo/patterns/fa-timespan-extensions-*.md` en CORPUSRUN-fa ronde 4 → "Herhaling TAAK-224/225".

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md` (ronde 4)
- `claude/tasks/patroon-queue.md`
