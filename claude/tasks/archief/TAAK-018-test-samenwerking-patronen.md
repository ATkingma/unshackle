# TAAK-018 — Test samenwerking patronen (combined proofs)

**Status**: ✅ voltooid — 2026-10-11
**Aangemaakt**: 2026-10-11
**Doel**: Geautomatiseerde tests die de echte `FluentAssertionsFix` (alle 15 patterns, echte volgorde) via de `Fixer` draaien en zo bewijzen dat patterns samenwerken.

## Context voor de volgende agent

- `ProofTests` draait per proof één pattern (gemockte `IFix`). Samenwerking (AllSatisfy-body die door latere patterns wordt herschreven, `.And`-splitsing naast andere patterns, usings-eindstaat) was alleen handmatig bewezen (REQ-12 van `fa.all-satisfy`).
- Parallel werkt een andere agent aan TAAK-017 (Core-contract, DataAccess, Integration-tests): die bestanden niet aanraken. Geen pattern-code wijzigen.
- Vindt een case een echte bug in een pattern/Fixer: niet repareren, hier vastleggen met `[!]`.

## Analyse

### Huidige situatie

`src/Unshackle.Tests/ProofTests.cs` ontdekt `Proofs/<Fix>/<Pattern>/<case>.before.cs` en draait alleen dat ene pattern. `FluentAssertionsFix.Patterns` (`src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`) heeft AllSatisfy bewust eerst.

### Probleem

- Verplaatst iemand AllSatisfy naar achteren, dan blijft de foreach-body FluentAssertions-code en faalt geen enkele test.
- Interactie tussen patterns in één bestand (en de usings) is niet getest.

### Nieuwe situatie

Tweede soort proof in `Proofs/FluentAssertions/_Combined/<case>.before.cs` + `.after.cs`, gedraaid door `CombinedProofTests` met de hele fix; plus een test die de volgorde vastlegt.

### Verbetering

Regressies in volgorde, `.And`-splitsing en usings worden automatisch gevangen; REQ-12 van fa.all-satisfy is geautomatiseerd.

## Aanpak

### Wat / Waar

- `src/Unshackle.Tests/CombinedProofTests.cs` (nieuw): theorie over `_Combined/*.before.cs` + volgorde-test.
- `src/Unshackle.Tests/ProofTests.cs`: sluit de map `_Combined` uit van de pattern-ontdekking.
- Cases: `all-satisfy-be`, `all-satisfy-other`, `mixed`, `usings`, `usings-existing`, `manual-and-fixed`.
- Docs: `docs/projectinfo/patterns/fa-all-satisfy-verify.md`, `docs/projectinfo/architectuur/testen.md`.

### Hoe

1. Before moet compileren tegen FluentAssertions. De Fixer draait met één `FluentAssertionsFix`.
2. Na de fix: exacte tekst gelijk aan `.after.cs`; after compileert tegen alleen Shouldly. Bevat de after nog `using FluentAssertions;` (Manual-restant), dan tegen Shouldly + FluentAssertions.
3. Volgorde-test: `Patterns[0].Id == "fa.all-satisfy"` met uitleg in de assert-boodschap.

### Waarom deze aanpak

Alleen `.after.cs` (geen `.manual.txt`): een combined-bestand bevat Fixed én Manual naast elkaar, dus de exacte eindtekst is het bewijs. Eigen testklasse i.p.v. ProofTests uitbreiden: ProofTests zoekt één pattern per case en gebruikt een gemockte fix, dat is een ander contract.

## Stappen

- [x] 1. Taak aanmaken
- [x] 2. `_Combined`-cases (before) schrijven
- [x] 3. `CombinedProofTests` + ProofTests-uitsluiting
- [x] 4. After-bestanden controleren/vastleggen; bugs noteren
- [x] 5. Docs bijwerken (verify fa-all-satisfy, testen.md)
- [x] 6. `dotnet build` + `dotnet test` groen

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| Andere agent breekt build tijdelijk | 30 s wachten, max 5× opnieuw |
| Case onthult pattern-bug | Niet repareren; `[!]` + melding |

## Gewijzigde bestanden

- `src/Unshackle.Tests/CombinedProofTests.cs` (nieuw), `src/Unshackle.Tests/ProofTests.cs` (sluit `_Combined` uit)
- `src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/*` (6 cases)
- `docs/projectinfo/patterns/fa-all-satisfy-verify.md`, `docs/projectinfo/architectuur/testen.md`

Bevinding (geen bug): `HaveCount(n).And.OnlyContain(...)` blijft Manual (`.And` na HaveCount wordt niet ondersteund; eigen proofs `HaveCount/chain`, `OnlyContain/and-chain`). Alleen `BeGreaterThan().And.BeLessThan()` wordt gesplitst; die zit in `mixed`.

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/architectuur/testen.md`
- `docs/projectinfo/patterns/fa-all-satisfy-verify.md`
