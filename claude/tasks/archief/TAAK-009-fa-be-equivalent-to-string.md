# TAAK-009 — Patroon fa.be-equivalent-to-string

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `s.Should().BeEquivalentTo("x")` op een string wordt `s.ShouldBe("x", StringCompareShould.IgnoreCase)`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 9 in `claude/tasks/patroon-queue.md`. Voorbeeld: TAAK-007. Sample: `OrderTests.cs:51`. `OrderHandlerTests.cs:30` (object) is rij 14.

## Analyse

FA 7.2.2 `StringAssertions.BeEquivalentTo(string, because, becauseArgs)`: alleen hoofdletters genegeerd (ordinal); leading/trailing whitespace, `\r\n` vs `\n`, İ/i en ß/SS verschillen. Geen `options =>`-overload voor strings. Shouldly `ShouldBe(expected, StringCompareShould.IgnoreCase)` gedraagt zich identiek (proefprogramma). Shouldly-overload met because: `ShouldBe(expected, customMessage, StringCompareShould)`. `ShouldNotBe` heeft geen IgnoreCase-overload.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Find filtert op `StringAssertions` | `BeEquivalentTo` op objecten/collecties is rij 14 |
| 2 | `because` staat tussen expected en `StringCompareShould.IgnoreCase` | Enige bestaande overload-volgorde |
| 3 | `NotBeEquivalentTo` Manual | Geen Shouldly-equivalent |
| 4 | Manual-redenen: "no Shouldly equivalent", "becauseArgs", "is used", "is not in the form" | `ProofTests` eist één reden per proof |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan (7 REQ)
- [x] 4. `BeEquivalentToStringPattern` + registratie
- [x] 5. Proofs (13 bestanden in `BeEquivalentToString/`)
- [x] 6. Build/test groen (98 + 15 sample), verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToStringPattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/`
- `docs/projectinfo/patterns/fa-be-equivalent-to-string-{plan,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-007-fa-contain.md`
