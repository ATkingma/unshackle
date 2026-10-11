---
kingmadoc: 1
feature: "gelijknamige-typen-swap"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Abstractions/IFix.cs
    - src/Unshackle.Core/UsingDirectives.cs
    - src/Unshackle.Core/ProjectPackages.cs
    - src/Unshackle.Core/Fixer.cs
    - src/Unshackle.Core/Models/PackageMigration.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs
    - src/Unshackle.Tests/Core/SwappedTypesTests.cs
    - src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs
---

# Feature: gelijknamige typen in de namespace-swap

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-024 (blokkade van TAAK-023).

## One-sentence summary

`IFix.SwappedTypes` noemt typen die in de nieuwe namespace een gelijknamige tweeling hebben; verwijzingen ernaar houden de
oude `using` en het oude pakket niet meer vast, zodat `using MediatR;` → `using Mediator;` wisselt en het pakket weggaat.

## Scope (in / out)

**In scope**

- `IFix.SwappedTypes` (default leeg, `Namespace.MetadataName`), gevuld door `MediatRFix`.
- `UsingDirectives.UsedNamespaces` en `ProjectPackages.UsesAssemblyAsync` slaan symbolen van die typen (en hun leden) over.
- `Fixer` wisselt de using ook in een bestand zonder patroon-wijziging als alleen swapped typen de oude namespace gebruiken.
- `PackageMigration.AlsoRemovedPackages` (`MediatR.Contracts`): mee verwijderd uit `<PackageReference>`/`<PackageVersion>`.

**Out of scope**

- Patronen (`Handle` → `ValueTask`, TAAK-023). `IPipelineBehavior` (andere delegate en `IMessage`-constraint: eigen patroon).

## Requirements

- **REQ-1**: WHEN een bestand alleen swapped typen van de oude namespace gebruikt THE SYSTEM SHALL de using in place wisselen, ook zonder patroon-wijziging.
- **REQ-2**: WHEN een project alleen nog swapped typen van het oude pakket gebruikt THE SYSTEM SHALL het pakket verwijderen (`KeepRemovedPackage = false`).
- **REQ-3**: WHEN een ander type van de oude namespace nog gebruikt wordt THE SYSTEM SHALL using en pakket laten staan.
- **REQ-4**: WHEN `AlsoRemovedPackages` direct gerefereerd wordt THE SYSTEM SHALL die referentie (en `PackageVersion`) verwijderen.
- **REQ-5**: WHEN de FluentAssertions-fix draait THE SYSTEM SHALL zich gedragen als voorheen (lege lijst).

## Assumptions

- Lijst geverifieerd in de echte assemblies (MediatR 12.5.0 + MediatR.Contracts, Mediator.Abstractions 3.0.2): `IBaseRequest`, `IRequest`, `IRequest\`1`, `IRequestHandler\`1/\`2`, `INotification`, `INotificationHandler\`1`, `ISender`, `IPublisher`, `IMediator`, `Unit` bestaan in beide.
- Methodesignaturen verschillen (`Task` vs `ValueTask`); dat lossen patronen op, niet de swap.

## Risks

- Bestand zonder wijziging krijgt ten onrechte `using Mediator;` → alleen wisselen als er een ongebruikte oude using is (`HasUnusedRemovedUsingAsync`).
- Contracts-project zonder `MediatR`: wordt nu ook gemigreerd (match op elk te verwijderen pakket) en krijgt de toegevoegde pakketten.

## Open questions

- Geen.

## Zie ook

- [gelijknamige-typen-swap-verify.md](gelijknamige-typen-swap-verify.md)
- [mediatr-fix-plan.md](mediatr-fix-plan.md)
- `claude/tasks/archief/TAAK-024-gelijknamige-typen-swap.md`
