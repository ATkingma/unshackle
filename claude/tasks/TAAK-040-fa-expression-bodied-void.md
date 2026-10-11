# TAAK-040 — Assertion als expression body van een void-methode wordt Manual i.p.v. Fixed

**Status**: stap 1/3 — nog te starten
**Aangemaakt**: 2026-10-11
**Doel**: de pending proof(s) hieronder slagen en staan weer in `Proofs/FluentAssertions/`.

## Context voor de volgende agent

- Gevonden bij het verzamelen van edge cases uit open-source projecten (`docs/projectinfo/architectuur/edge-cases.md`).
- `public void Test(int v) => v.Should().Be(1);` (ook BeNull, NotThrow) geeft Manual "result ... is used".
- Proofs staan in `src/Unshackle.Tests/Proofs/_Pending/` (draaien niet, zie `ProofTests.PendingFolderName`). Het `.after.cs` is de gewenste uitkomst:
- `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/ShouldBe/edge-expression-bodied.before.cs` + `.after.cs`
- `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/BeNull/edge-expression-bodied.before.cs` + `.after.cs`
- `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/NotThrow/edge-expression-bodied.before.cs` + `.after.cs`

## Analyse

### Huidige situatie

`FluentAssertionsSyntax.IsResultDiscarded` (src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs) kent alleen `ExpressionStatementSyntax` en void-lambda's, niet `ArrowExpressionClauseSyntax` van een void-methode/local function/accessor.

### Probleem

Echte testcode in deze vorm wordt niet (correct) gemigreerd.

### Nieuwe situatie

Expression-bodied void-leden worden herschreven zoals een statement: `=> v.ShouldBe(1);`.

### Verbetering

Minder onnodige Manual-findings / geen kapotte build na migratie.

## Aanpak

### Hoe

Voeg in `IsResultDiscarded` een tak toe: `ArrowExpressionClauseSyntax` waarvan de parent een methode/local function is met `void` (of `Task` bij async is géén discard; alleen echte void) als returntype.

## Stappen

- [ ] 1. Proof(s) uit `_Pending/` terugzetten naar `Proofs/FluentAssertions/<Patroon>/`, zien falen.
- [ ] 2. Fix in de genoemde code; alle proofs (`--filter-class "*ProofTests"`) en `dotnet test` groen.
- [ ] 3. Verify-doc van het geraakte patroon bijwerken; taak archiveren.

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| Raakt alle patronen die `IsResultDiscarded` gebruiken: draai alle proofs. | Alle proofs + CombinedProofTests draaien |

## Gewijzigde bestanden

_(wordt aangevuld tijdens uitvoering)_

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/architectuur/edge-cases.md`
