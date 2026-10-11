# TAAK-043 — Assertions in een inactieve `#if`-tak blijven FA terwijl de using verdwijnt

**Status**: ✅ voltooid 2026-10-11
**Aangemaakt**: 2026-10-11
**Doel**: de pending proof(s) hieronder slagen en staan weer in `Proofs/FluentAssertions/`.

## Context voor de volgende agent

- Gevonden bij het verzamelen van edge cases uit open-source projecten (`docs/projectinfo/architectuur/edge-cases.md`).
- Refit-achtige multi-target tests zetten assertions in `#if NET9_0_OR_GREATER`.
- Proofs staan in `src/Unshackle.Tests/Proofs/_Pending/` (draaien niet, zie `ProofTests.PendingFolderName`). Het `.after.cs` is de gewenste uitkomst:
- `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/ShouldBe/edge-ifdef.before.cs` + `.after.cs`

## Analyse

### Huidige situatie

Roslyn ziet code in een inactieve tak als `DisabledTextTrivia`; het patroon vindt alleen de actieve tak. `UsingDirectives` haalt `using FluentAssertions;` weg omdat de actieve code hem niet meer nodig heeft, waardoor de andere configuratie niet meer compileert.

### Probleem

Echte testcode in deze vorm wordt niet (correct) gemigreerd.

### Nieuwe situatie

Óf beide takken herschreven (parse per preprocessor-symbolenset), óf de FA-using blijft staan + Manual "code in inactive #if branch still uses FluentAssertions".

### Verbetering

Minder onnodige Manual-findings / geen kapotte build na migratie.

## Aanpak

### Hoe

Minimaal: in `Fixer`/`UsingDirectives` de using niet verwijderen als `DisabledTextTrivia` nog `.Should()` bevat, en een Manual-finding geven. TAAK-022 (dedupe per target) is gerelateerd: de scan per target framework ziet andere symbolen; `DEBUG`/eigen symbolen blijven een gat.

## Stappen

- [x] 1. Proof(s) uit `_Pending/` terugzetten naar `Proofs/FluentAssertions/<Patroon>/`, zien falen.
- [x] 2. Fix in de genoemde code; alle proofs (`--filter-class "*ProofTests"`) en `dotnet test` groen.
- [x] 3. Verify-doc van het geraakte patroon bijwerken; taak archiveren.

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| Raakt Fixer/UsingDirectives (buiten patroon-code): eigen ontwerpkeuze nodig. | Alle proofs + CombinedProofTests draaien |

## Gewijzigde bestanden

`src/Unshackle.Core/Fixer.cs`, `UsingDirectives.cs`, `ProjectPackages.cs`, `src/Unshackle.Tests/ProofTests.cs` (after compileert tegen oud+nieuw bij Manual), `Core/FixerTests.cs`, proofs `ShouldBe/edge-ifdef` + `edge-global-using`, `docs/projectinfo/architectuur/core.md`. Keuze en motivatie: core.md §Inactieve `#if`-code en global usings.

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/architectuur/edge-cases.md`
