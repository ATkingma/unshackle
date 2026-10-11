# TAAK-041 — Subject met `?.` wordt Manual "result is used"

**Status**: ✅ voltooid (2026-10-11)
**Aangemaakt**: 2026-10-11
**Doel**: de pending proof(s) hieronder slagen en staan weer in `Proofs/FluentAssertions/`.

## Context voor de volgende agent

- Gevonden bij het verzamelen van edge cases uit open-source projecten (`docs/projectinfo/architectuur/edge-cases.md`).
- `uri?.Host.Should().BeNull();` geeft Manual "The result of BeNull() is used" terwijl het resultaat niet gebruikt wordt.
- Proofs staan in `src/Unshackle.Tests/Proofs/_Pending/` (draaien niet, zie `ProofTests.PendingFolderName`). Het `.after.cs` is de gewenste uitkomst:
- `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/BeNull/edge-null-conditional.before.cs` + `.after.cs`

## Analyse

### Huidige situatie

Bij `a?.B.Should().BeNull()` is de assertion-invocation onderdeel van de `WhenNotNull`-tak van een `ConditionalAccessExpressionSyntax`; de parent is dus geen `ExpressionStatementSyntax` en `IsResultDiscarded` geeft false.

### Probleem

Echte testcode in deze vorm wordt niet (correct) gemigreerd.

### Nieuwe situatie

`uri?.Host.ShouldBeNull();` (de `?.` blijft; semantiek gelijk: bij null-receiver wordt niets gecontroleerd, net als in FA). Of bewust Manual met een eerlijke reden.

### Verbetering

Minder onnodige Manual-findings / geen kapotte build na migratie.

## Aanpak

### Hoe

Loop in `IsResultDiscarded` omhoog door `ConditionalAccessExpressionSyntax` (zolang de invocation in `WhenNotNull` zit) voordat de parent getest wordt. HaveCount heeft al een eigen `?.`-Manual (HaveCountPattern.cs:32); die laten staan.

## Stappen

- [x] 1. Proof(s) uit `_Pending/` terugzetten naar `Proofs/FluentAssertions/<Patroon>/`, zien falen.
- [x] 2. Fix in de genoemde code; alle proofs (`--filter-class "*ProofTests"`) en `dotnet test` groen.
- [x] 3. Verify-doc van het geraakte patroon bijwerken; taak archiveren.

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| Twijfel: FA en Shouldly slaan bij `?.` beide de check over; dat is al gebruikerscode-gedrag, geen nieuwe zwakte. | Alle proofs + CombinedProofTests draaien |

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` (`IsResultDiscarded`: `?.`-keten en void expression body)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs` (ConfigureAwait)
- proofs uit `_Pending/` verplaatst; verify-docs fa-should-be, fa-be-null, fa-not-throw, fa-throw-async

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/architectuur/edge-cases.md`
