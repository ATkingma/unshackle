---
kingmadoc: 1
feature: "mediatr-internal-handler-access"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/InternalHandlerAccessPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Core/Abstractions/ISolutionFinder.cs
    - src/Unshackle.Core/Fixer.cs
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/
---

# Feature: mediatr.internal-handler-access: InternalsVisibleTo voor internal handlers die een ander project scant

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (met de hand, kingmadoc ontbreekt) |

> Opgesteld uit TAAK-195 (corpusrun 2026-10-12: ardalis `Program.cs:109` scant `Clean.Architecture.Core` met internal `ContributorDeletedHandler`; evo2 `MediationModule` scant `Contracts.Application` met internal handlers).

## One-sentence summary

Scant `AddMediatR` in project A een ander project B van de solution met internal handlers, dan krijgt één bestand van B `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("A")]`, zodat de code die Mediator's generator in A schrijft die handlers mag noemen; `mediatr.add-mediatr` herschrijft de aanroep dan in plaats van Manual.

## Scope (in / out)

**In scope**:
- Gescande assembly via `typeof(T)`, `<T>` of een lokale variabele daarmee (dezelfde herkenning als `mediatr.add-mediatr`, gedeelde helper `AddMediatRPattern.ScannedAssemblies`).
- Meerdere hosts die hetzelfde project scannen: één attribuut per host, gesorteerd, in hetzelfde bestand.

**Out of scope**: handlers in een pakket of een assembly buiten de solution (blijft Manual in `mediatr.add-mediatr`); strong-named assemblies (een friend heeft dan een `PublicKey` nodig: Manual); een `Assembly`-parameter van een publieke methode en een internal methode die een testproject aanroept (evo1/evo2): geen exacte vertaling, zie TAAK-195.

## Requirements

- **REQ-1**: WHEN `AddMediatR` in project A een project B van de solution scant met een niet-publieke handler THE SYSTEM SHALL in het eerste bestand van B (op pad) dat zo'n handler declareert `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("A")]` na de usings zetten (proof `_Combined/add-mediatr-internal-handlers-other-project`).
- **REQ-2**: WHEN dat zo is THE SYSTEM SHALL de `AddMediatR`-aanroep in A herschrijven, niet Manual melden (zelfde proof; `after` compileert per project tegen Mediator 3.0.2 met generator in A).
- **REQ-3**: WHEN twee projecten B scannen THE SYSTEM SHALL twee attributen in één bestand zetten, één bevinding (`InternalHandlerAccessPatternTests.FixAsync_TwoHostsScanInternalHandlers_OneAttributePerHostInOneFile`).
- **REQ-4**: IF B de host al `InternalsVisibleTo` geeft, of niemand B scant THEN THE SYSTEM SHALL niets toevoegen (`..._LibraryAlreadyGrantsHost_AddsNoAttribute`, `..._NoOtherProjectScansLibrary_AddsNothing`).
- **REQ-5**: Zonder het attribuut bouwt het resultaat niet (CS0122 in de gegenereerde code; `CompileErrors_GeneratedMediatorWithoutInternalsVisibleTo_IsCS0122`).

## Assumptions

- Mediator 3.0.2 genereert in het host-project code die de handlers uit gerefereerde assemblies bij naam noemt; met `InternalsVisibleTo` voor de host bouwt en draait dat (probe TAAK-183 en TAAK-195).
- Gedrag gelijk aan MediatR: MediatR registreert internal handlers via reflectie; het attribuut verandert alleen zichtbaarheid, geen runtimegedrag.
- `ISolutionFinder` (nieuw contract in `Abstractions/`): een pattern dat alleen met de solution weet of een bestand een bevinding heeft. Zonder dat zou elk bestand met een internal handler een lege Fixed-rij krijgen.

## Modellen

[mediatr-internal-handler-access-modellen.md](mediatr-internal-handler-access-modellen.md).

## Zie ook

- [mediatr-internal-handler-access-verify.md](mediatr-internal-handler-access-verify.md)
- [mediatr-add-mediatr-plan.md](mediatr-add-mediatr-plan.md)
- `claude/tasks/TAAK-195-mediatr-addmediatr-projectgrenzen.md`
