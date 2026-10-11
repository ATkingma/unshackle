# TAAK-016 — Patroon fa.be-equivalent-to-object

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `a.Should().BeEquivalentTo(b)` op objecten wordt `a.ShouldBeEquivalentTo(b)` alleen waar FluentAssertions en Shouldly aantoonbaar gelijk beslissen; de rest is Manual.

## Context voor de volgende agent

- Rij 14 in `claude/tasks/patroon-queue.md`. Strings zijn rij 9 (TAAK-009); Find sluit `StringAssertions` uit. Sample: `OrderHandlerTests.cs:30` (`OrderDto`, alleen primitieven) wordt Fixed.

## Analyse (proefprogramma FA 7.2.2 tegen Shouldly 4.3.0, ~70 gevallen)

- Shouldly is strenger: collecties op volgorde (FA niet), runtime-typen/leden (FA gebruikt de leden van de verwachting; anonieme typen, basis- vs afgeleide klasse, subset-verwachting falen in Shouldly), `int` vs `long`, interface-typen, `Equals`-override waar FA `Equals` gebruikt maar leden verschillen. Dat zijn hoorbare extra fouten.
- Gevaarlijk (FA faalt, Shouldly slaagt): type met `Equals`-override dat false geeft (ook als lid), cyclische graaf, type zonder publieke leden (FA gooit `InvalidOperationException`).
- Gelijk: zelfde type, primitieven, string (hoofdlettergevoelig, null vs leeg faalt), decimal, enum, DateTime/Guid, records, structs, privé genegeerd, publieke velden meegenomen, null/null.
- `ShouldBeEquivalentTo(expected, customMessage)` bestaat; `ShouldNotBeEquivalentTo` niet.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Fixed alleen bij gelijk compile-time type op beide zijden (zonder nullability) | Anders vergelijkt FA alleen de leden van de verwachting |
| 2 | Type en bereikbare leden: alleen gewone class/struct/record + leaf-leden (primitieven, string, decimal, enum, DateTime(Offset), Guid, TimeSpan, DateOnly, TimeOnly, Nullable daarvan) | Alleen die zijn geprobeerd en gelijk |
| 3 | Collectie (subject of lid) Manual | FA negeert volgorde |
| 4 | `Equals`-override, cyclus, geen publieke leden Manual | Gevaarlijke richting: Shouldly slaagt waar FA faalt |
| 5 | Niet-afgeschermde klasse toegestaan | Afgeleid runtime-type geeft hoorbaar een extra fout, nooit een stil verschil |
| 6 | `options =>`, becauseArgs, NotBeEquivalentTo, ketens, gebruikt resultaat Manual | Geen equivalent |

## Stappen

- [x] 1. Claimen, onderzoek, keuzes
- [x] 2. KingmaDoc-plan (13 REQ, approved)
- [x] 3. `BeEquivalentToObjectPattern` + registratie
- [x] 4. Proofs (6 Fixed, 14 Manual) in `BeEquivalentToObject/`
- [x] 5. Modellen (3 figuren), verify, README-rij
- [x] 6. Build/test groen (236 + 15 sample), archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/`
- `docs/projectinfo/patterns/fa-be-equivalent-to-object-{plan,modellen,verify}.md` (+ `img/`), `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-009-fa-be-equivalent-to-string.md`
