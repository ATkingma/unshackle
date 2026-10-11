# TAAK-032 — Patroon fa.not-be

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `x.Should().NotBe(y)` wordt `x.ShouldNotBe(y)`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 15 in `claude/tasks/patroon-queue.md` (ronde 2). Sample: `SampleShop.Advanced.Tests/AdvancedAssertionTests.cs:75`. Voorbeeld: `ShouldBePattern` (TAAK-001).

## Analyse

FA 7.2.2: `NotBe(unexpected, because, becauseArgs)` op object-, primitive-, enum- en nullable-assertions, plus een comparer-overload op `ObjectAssertions`; gelijkheid via `Equals`. Shouldly 4.3.0: `ShouldNotBe<T>(this T actual, T expected, string? customMessage)`, ook via `Equals`. Zelfde type-inferentieprobleem als `ShouldBe` (bv. `byte b; b.Should().NotBe(0)`).

## Keuzes

| #   | Keuze                                                            | Reden                                                                                                                                                            |
| --- | ---------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Zelfde controles als `ShouldBePattern`                           | Zelfde API-vorm; consistente Manual-redenen                                                                                                                      |
| 2   | `ShouldBePattern.ConvertExpected` → `internal` en hergebruiken   | Geen duplicatie van de conversielogica; gedrag ongewijzigd                                                                                                       |
| 3   | Geen `FluentAssertionsSyntax.NotNull` op het subject             | Null-subject slaagt in FA `NotBe(y)`; `ShouldNotBeNull()` zou dat laten falen. `ShouldNotBe<T>` accepteert een nullable T zonder waarschuwing (proof `nullable`) |
| 4   | Records: gewoon `ShouldNotBe`                                    | Beide kanten vergelijken met `Equals` (value-equality)                                                                                                           |
| 5   | AssertionScope, becauseArgs, comparer, keten, Xml/Types → Manual | Zelfde redenen als `fa.should-be`                                                                                                                                |
| 6   | `.And.NotBe(...)` blijft Manual                                  | Hoort bij `fa.and-chain`                                                                                                                                         |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan (8 REQ)
- [x] 4. `NotBePattern` + registratie
- [x] 5. Proofs (10 cases in `NotBe/`)
- [x] 6. Build/test groen (320 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotBePattern.cs` (nieuw), `ShouldBePattern.cs` (internal), `FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/NotBe/`
- `docs/projectinfo/patterns/fa-not-be-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-001-fundament.md`
- `docs/projectinfo/patterns/fa-not-be-plan.md`
