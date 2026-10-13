---
kingmadoc: 1
feature: "mediatr-vendored-package"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/VendoredPackages.cs
    - src/Unshackle.Core/VendoredPlan.cs
    - src/Unshackle.Core/Fixes/MediatR/ArdalisSharedKernel.cs
    - src/Unshackle.Core/Fixes/MediatR/Vendored/ArdalisSharedKernel/
    - src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs
    - src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/Vendored/
---

# Feature: mediatr.vendored-package: pakket dat op MediatR bouwt (Ardalis.SharedKernel)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (met de hand, kingmadoc ontbreekt) |

> Opgesteld uit TAAK-194 (corpusrun 2026-10-12, ardalis v9.1.0: 11× "Ardalis.SharedKernel builds on MediatR").

## One-sentence summary

Gebruikt een solution Ardalis.SharedKernel 1.4.0–3.0.1 (bouwt op MediatR), dan zet de fix de broncode van die versie, met Mediator in plaats van MediatR, in het project dat alle gebruikers refereren, en vervangt het pakket door zijn eigen afhankelijkheden; namespace, typen en gedrag blijven gelijk.

## Scope (in / out)

**In scope**:
- Ardalis.SharedKernel 1.4.0, 1.5.0, 1.6.0, 2.1.0, 2.1.1 en 3.0.1 (alle typen: `DomainEventBase`, `EntityBase`(`<TId>`, `<T, TId>`), `HasDomainEventsBase`, `IHasDomainEvents`, `IAggregateRoot`, `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`, `IRepository`, `IReadRepository`, `IDomainEventDispatcher`, `MediatRDomainEventDispatcher`, `LoggingBehavior`, `ValueObject`).
- Projectbestanden: pakket weg uit elk project, het root-project krijgt Ardalis.GuardClauses, Ardalis.Specification en Microsoft.Extensions.Logging.Abstractions (CPM: bestaande `PackageVersion` blijft).

**Out of scope**: Ardalis.SharedKernel 1.0.0–1.3.0 (andere API, niet in het corpus) en andere pakketten op MediatR: Manual zoals voorheen (TAAK-133). Een upgrade naar 4.x is bewust geen optie (zie Assumptions).

## Requirements

- **REQ-1**: WHEN een project een ondersteunde versie van Ardalis.SharedKernel refereert THE SYSTEM SHALL geen Manual "builds on MediatR" meer melden en de gebruikerscode migreren zoals andere MediatR-code (proof `_Combined/Vendored/shared-kernel`).
- **REQ-2**: WHEN alle gebruikers van het pakket (transitief) één ervan refereren THE SYSTEM SHALL de broncode van die versie in `<dat project>/Ardalis.SharedKernel/` zetten, met `LICENSE.txt`, als nieuwe bestanden (proof `_Combined/Vendored/shared-kernel`, test `VendoredPackagesTests.FixAsync_UsersShareOneProject_*`).
- **REQ-3**: THE SYSTEM SHALL in die broncode dezelfde namespace, typenamen, signaturen en het runtimegedrag van de versie houden, op `global::Mediator` (probe: identieke uitvoer; proof compileert zonder warnings).
- **REQ-4**: WHEN het pakket vendored wordt THE SYSTEM SHALL het uit alle projectbestanden halen en het root-project zijn afhankelijkheden geven, in dezelfde bewerking als de MediatR-swap (tests `EditAsync_VendoredPackage_*`, `FixAsync_UsersShareOneProject_*`).
- **REQ-5**: IF de gebruikers geen gemeenschappelijk project hebben, de versie niet ondersteund is, of de map al bronbestanden heeft THEN THE SYSTEM SHALL Manual melden met de reden (test `FixAsync_UsersShareNoProject_ReportsManual`).
- **REQ-6**: WHEN een nieuw bestand al op schijf staat THE SYSTEM SHALL niets schrijven en melden (test `SaveAsync_NewFileAlreadyThere_*`).

## Assumptions

- Ardalis.SharedKernel 4.0.0+ bouwt op Mediator.Abstractions maar heeft `ICommand`/`IQuery`/handlers verwijderd, `ClearDomainEvents` public gemaakt en vraagt net9.0: geen drop-in. Broncode is MIT (Copyright (c) 2022 Steve Smith); de licentie gaat mee.
- `LoggingBehavior` logde onder `ILogger<MediatR.Mediator>` (categorie `MediatR.Mediator`); de vendored versie neemt `ILoggerFactory` en maakt dezelfde categorie.
- Gelijknamige `ICommand<T>`, `IQuery<T>` en handlers naast `using Mediator;` krijgen de volle naam via `AddedTypes` (TAAK-144).
- De fix is alles-of-niets: een Manual hier laat de hele solution ongemoeid.

## Modellen

[mediatr-vendored-package-modellen.md](mediatr-vendored-package-modellen.md).

## Zie ook

- [mediatr-vendored-package-verify.md](mediatr-vendored-package-verify.md)
- [mediatr-unsupported-plan.md](mediatr-unsupported-plan.md)
- `claude/tasks/archief/TAAK-194-mediatr-vendored-shared-kernel.md`
