# TAAK-042 — `await act.Should().ThrowAsync<T>().ConfigureAwait(false)` wordt Manual

**Status**: ✅ voltooid (2026-10-11)
**Aangemaakt**: 2026-10-11
**Doel**: de pending proof(s) hieronder slagen en staan weer in `Proofs/FluentAssertions/`.

## Context voor de volgende agent

- Gevonden bij het verzamelen van edge cases uit open-source projecten (`docs/projectinfo/architectuur/edge-cases.md`).
- Library-tests (bv. EventFlow-stijl) zetten `.ConfigureAwait(false)` achter elke await, ook achter ThrowAsync.
- Proofs staan in `src/Unshackle.Tests/Proofs/_Pending/` (draaien niet, zie `ProofTests.PendingFolderName`). Het `.after.cs` is de gewenste uitkomst:
- `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/ThrowAsync/edge-configure-await.before.cs` + `.after.cs`

## Analyse

### Huidige situatie

`ThrowAsyncPattern` (regel ~24/57) verwacht `await` direct om `ThrowAsync<T>()`; met `.ConfigureAwait(false)` ertussen valt het in "not directly awaited".

### Probleem

Echte testcode in deze vorm wordt niet (correct) gemigreerd.

### Nieuwe situatie

`await Should.ThrowAsync<T>(act).ConfigureAwait(false);` (Shouldly geeft `Task<T>`, ConfigureAwait werkt).

### Verbetering

Minder onnodige Manual-findings / geen kapotte build na migratie.

## Aanpak

### Hoe

Herken `await <x>.Should().ThrowAsync<T>().ConfigureAwait(<arg>)` als directe await; behoud de ConfigureAwait-aanroep in de rewrite. Zelfde voor `WithMessage`-ketens via `ThrowMessage`.

## Stappen

- [x] 1. Proof(s) uit `_Pending/` terugzetten naar `Proofs/FluentAssertions/<Patroon>/`, zien falen.
- [x] 2. Fix in de genoemde code; alle proofs (`--filter-class "*ProofTests"`) en `dotnet test` groen.
- [x] 3. Verify-doc van het geraakte patroon bijwerken; taak archiveren.

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| Niet verwarren met `.Where`/`.WithInnerException`, die blijven Manual. | Alle proofs + CombinedProofTests draaien |

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` (`IsResultDiscarded`: `?.`-keten en void expression body)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs` (ConfigureAwait)
- proofs uit `_Pending/` verplaatst; verify-docs fa-should-be, fa-be-null, fa-not-throw, fa-throw-async

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/architectuur/edge-cases.md`
