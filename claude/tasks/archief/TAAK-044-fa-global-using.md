# TAAK-044 — `global using FluentAssertions;` wordt niet vervangen

**Status**: ✅ voltooid 2026-10-11
**Aangemaakt**: 2026-10-11
**Doel**: de pending proof(s) hieronder slagen en staan weer in `Proofs/FluentAssertions/`.

## Context voor de volgende agent

- Gevonden bij het verzamelen van edge cases uit open-source projecten (`docs/projectinfo/architectuur/edge-cases.md`).
- Ardalis.Specification-stijl: één `GlobalUsings.cs` met `global using FluentAssertions;`, testbestanden zonder using.
- Proofs staan in `src/Unshackle.Tests/Proofs/_Pending/` (draaien niet, zie `ProofTests.PendingFolderName`). Het `.after.cs` is de gewenste uitkomst:
- `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/ShouldBe/edge-global-using.before.cs` + `.after.cs`

## Analyse

### Huidige situatie

`UsingDirectives` slaat global usings bewust over (UsingDirectives.cs:31) en voegt per bestand `using Shouldly;` toe. Na verwijderen van het FA-pakket breekt `global using FluentAssertions;`.

### Probleem

Echte testcode in deze vorm wordt niet (correct) gemigreerd.

### Nieuwe situatie

`global using FluentAssertions;` → `global using Shouldly;` zodra geen bestand in het project FA nog nodig heeft; geen losse `using Shouldly;` per bestand.

### Verbetering

Minder onnodige Manual-findings / geen kapotte build na migratie.

## Aanpak

### Hoe

Projectbrede stap na alle documenten (Fixer), niet per document: alleen vervangen als geen Manual-findings meer FA nodig hebben.

## Stappen

- [x] 1. Proof(s) uit `_Pending/` terugzetten naar `Proofs/FluentAssertions/<Patroon>/`, zien falen.
- [x] 2. Fix in de genoemde code; alle proofs (`--filter-class "*ProofTests"`) en `dotnet test` groen.
- [x] 3. Verify-doc van het geraakte patroon bijwerken; taak archiveren.

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| Ontwerpvraag: proof-runner werkt met één bestand; de proof zet de global using in hetzelfde bestand. | Alle proofs + CombinedProofTests draaien |

## Gewijzigde bestanden

`src/Unshackle.Core/Fixer.cs`, `UsingDirectives.cs`, `ProjectPackages.cs`, `src/Unshackle.Tests/ProofTests.cs` (after compileert tegen oud+nieuw bij Manual), `Core/FixerTests.cs`, proofs `ShouldBe/edge-ifdef` + `edge-global-using`, `docs/projectinfo/architectuur/core.md`. Keuze en motivatie: core.md §Inactieve `#if`-code en global usings.

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/architectuur/edge-cases.md`
