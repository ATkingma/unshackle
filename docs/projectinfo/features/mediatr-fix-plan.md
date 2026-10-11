---
kingmadoc: 1
feature: "mediatr-fix"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Models/PackageMigration.cs
    - src/Unshackle.Core/Models/AddedPackage.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs
    - src/Unshackle.Interface/Cli.cs
    - src/Unshackle.Tests/ProofTests.cs
    - src/Unshackle.Tests/Helpers/TestReferences.cs
    - src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs
    - src/Unshackle.Tests/Interface/CliTests.cs
    - Directory.Packages.props
---

# Feature: MediatR-fix (infrastructuur)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-020. Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

Een tweede fix `mediatr` (MediatR 12.5.0 → Mediator 3.0.2) zonder patronen, aan CLI en proof-runner gekoppeld, met een
`PackageMigration` die meerdere toe te voegen pakketten kent (`Mediator.Abstractions` + `Mediator.SourceGenerator`).

## Scope (in / out)

**In scope**

- `PackageMigration.AddedPackages` (lijst van naam + versie); het eerste pakket vervangt het oude in place, de rest komt ernaast.
- `MediatRFix` (`Id = "mediatr"`, namespaces `MediatR` → `Mediator`), registratie in `Cli.cs`.
- Proof-runner kiest fix en referenties op `Proofs/<Fix>/`; `TestReferences.MediatR` / `.Mediator`.
- `Mediator.SourceGenerator` 3.0.2 in `Directory.Packages.props`.

**Out of scope**

- Patronen (zie `claude/tasks/mediatr-queue.md`), `MediatRSyntax`, wijzigingen in `samples/`.

## Requirements

- **REQ-1**: WHEN een fix meerdere toe te voegen pakketten heeft THE SYSTEM SHALL het eerste in place op de plek van het oude pakket zetten en de andere direct erna, in `<PackageReference>` én `<PackageVersion>`.
- **REQ-2**: WHEN de FluentAssertions-fix draait THE SYSTEM SHALL dezelfde projectbestanden opleveren als vóór deze wijziging.
- **REQ-3**: WHEN de gebruiker `--fix mediatr` meegeeft THE SYSTEM SHALL de fix accepteren.
- **REQ-4**: WHEN een proof onder `Proofs/MediatR/` staat THE SYSTEM SHALL hem met `MediatRFix` draaien, `before` tegen MediatR 12.5.0 en `after` tegen Mediator.Abstractions 3.0.2 compileren.
- **REQ-5**: WHEN `MediatRFix` pakketten toevoegt THE SYSTEM SHALL `Mediator.Abstractions` en `Mediator.SourceGenerator` met dezelfde versie (3.0.2) gebruiken.

## Assumptions

- Mediator 3.0.2 is de laatste stabiele versie (NuGet-index, 2026-10-11); de assembly heet `Mediator`, namespace `Mediator` (xml-docs van het pakket).
- _(inferred)_ MediatR 12 zet `IRequest`/`INotification`/`Unit` in `MediatR.Contracts`; de proof-referenties nemen die assembly mee.

## Risks

- Extra pakket op de verkeerde plek → extra's eerst toevoegen "alsof het oude blijft" (naast het oude element), daarna het eerste in place.
- FA-gedrag wijzigt → bestaande `XmlProjectFileStoreTests` en `FixerProjectFileTests` ongewijzigd groen.

## C4 Container (Mermaid)

Ongewijzigd ten opzichte van [projectbestanden-migreren](projectbestanden-migreren-plan.md); Core heeft een tweede `IFix`.

## Open questions

- [x] Smoke-proof `MediatR/Usings`? → kan pas met een patroon: de Fixer wisselt usings alleen in documenten die een patroon wijzigde, en de proof-runner draait per patroon. Het eerste MediatR-patroon levert de eerste proof.
