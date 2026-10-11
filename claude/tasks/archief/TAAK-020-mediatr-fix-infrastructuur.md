# TAAK-020 — MediatRFix-infrastructuur (MediatR 12.5.0 → Mediator 3.x)

**Status**: voltooid
**Aangemaakt**: 2026-10-11
**Doel**: een tweede `IFix` (`mediatr`) staat, is aan de CLI en de proof-runner gekoppeld, zonder patronen; de patronen komen uit `claude/tasks/mediatr-queue.md`.

## Context voor de volgende agent

- Roadmap stap 3 in `docs/projectinfo/project.md`. Ontwerp-voorbeeld: `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`.
- Lege mappen staan al: `src/Unshackle.Core/Fixes/MediatR/Patterns/`.
- Pakketten in `Directory.Packages.props`: `MediatR` 12.5.0, `Mediator.Abstractions` 3.0.2 (types voor proofs).
- Doelcode: `samples/SampleShop/SampleShop/` (handlers, `LoggingBehavior`, `ServiceCollectionExtensions.AddMediatR`) en `SampleShop.Tests/OrderHandlerTests.cs` (`ISender.Send`).
- Na deze taak draait de patroon-loop (`claude/docs/patroon-loop.md`) op `claude/tasks/mediatr-queue.md`.

## Analyse

### Huidige situatie

`IFix` heeft één implementatie; `Cli.cs:24`, `ProofTests.cs:23` en `CombinedProofTests.cs:37/53` noemen `FluentAssertionsFix` hard. `PackageMigration` kent precies één toegevoegd pakket.

### Probleem

- Mediator heeft **twee** pakketten nodig: `Mediator.Abstractions` (types) én `Mediator.SourceGenerator` (genereert `AddMediator` en de dispatcher). `PackageMigration` kan dat niet uitdrukken.
- De proof-runner kiest de fix niet per proof-map; proofs onder `Proofs/MediatR/` zouden met de FA-fix draaien.
- Patroon-ID's gebruiken prefix `fa.`; voor deze fix wordt dat `mediatr.`.

### Nieuwe situatie

`MediatRFix` (`Id = "mediatr"`, `RemovedNamespaces = ["MediatR"]`, `AddedNamespaces = ["Mediator"]`), de CLI accepteert `--fix mediatr`, proofs onder `Proofs/MediatR/` draaien tegen MediatR (before) en Mediator.Abstractions (after).

### Verbetering

Roadmap stap 3 kan in de bestaande patroon-loop, parallel per patroon, zonder dat patroon-agents contracten hoeven te wijzigen (wat de agent-brief verbiedt).

## Aanpak

### Wat

1. `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` met lege `Patterns`-lijst.
2. Contractwijziging `PackageMigration`: meerdere toegevoegde pakketten (bv. `AddedPackages` als lijst van naam+versie). Mediator 3.x-versie voor beide pakketten vastleggen (zelfde versie; nu 3.0.2 in props, `Mediator.SourceGenerator` toevoegen).
3. Registratie in `Cli.cs` en een fix-per-proofmap in `ProofTests.cs`/`CombinedProofTests.cs`.
4. Hulpklasse `MediatRSyntax` (zoals `FluentAssertionsSyntax`) alleen als het eerste patroon hem nodig heeft — niet vooraf.
5. Docs: rij in `docs/projectinfo/README.md`, `architectuur/core.md` noemt de tweede fix, `INDEX.md` → `mediatr-queue.md`.

### Waar

`src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs`, `src/Unshackle.Core/Models/PackageMigration.cs` (+ DataAccess-gebruik ervan), `src/Unshackle.Interface/Cli.cs`, `src/Unshackle.Tests/ProofTests.cs`, `src/Unshackle.Tests/CombinedProofTests.cs`, `Directory.Packages.props`.

### Hoe

Kopieer de vorm van `FluentAssertionsFix`. Breid `PackageMigration` minimaal uit en pas de FA-fix mee aan; bestaande tests moeten groen blijven. Eén smoke-proof (`using MediatR;` → `using Mediator;` op een bestand zonder andere MediatR-code) bewijst de koppeling.

### Waarom deze aanpak

Contracten nu wijzigen, vóór de patroon-loop, omdat patroon-agents dat niet mogen. Geen `MediatRSyntax` vooraf: YAGNI.

## Stappen

- [x] 1. `PackageMigration` meerdere pakketten + FA-fix en DataAccess aanpassen, tests groen.
- [x] 2. `MediatRFix` aanmaken, in `Cli.cs` registreren.
- [x] 3. Proof-runner: fix kiezen op proof-map (`Proofs/<Fix>/`). Smoke-proof niet mogelijk zonder patroon (Fixer wisselt usings alleen na een patroon-wijziging); het eerste patroon levert de eerste proof.
- [x] 4. `Mediator.SourceGenerator` in `Directory.Packages.props`.
- [x] 5. Docs (README, core.md, INDEX.md).
- [x] 6. `dotnet build`, `dotnet test`, sample-tests groen; rij 0 in `mediatr-queue.md` op `klaar`.

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| Mediator heeft source-generator nodig; zonder die compileert `AddMediator` niet | beide pakketten via `PackageMigration` |
| Gemengde code (FA-fix en MediatR-fix tegelijk in SampleShop) | `CombinedProofTests` per fix laten draaien |
| `using MediatR` verdwijnt pas als niets meer bindt; Manual-gevallen houden hem | bestaand gedrag van de Fixer, zo laten |

## Gewijzigde bestanden

- `src/Unshackle.Core/Models/PackageMigration.cs`, `AddedPackage.cs` (nieuw): `AddedPackages` lijst
- `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs`: extra pakketten naast het oude, eerste in place
- `src/Unshackle.Interface/Cli.cs`
- `src/Unshackle.Tests/ProofTests.cs`, `Helpers/TestReferences.cs`, `Interface/CliTests.cs`, `DataAccess/Projects/XmlProjectFileStoreTests.cs`, `Core/FixerProjectFileTests.cs`
- `Directory.Packages.props`: `Mediator.SourceGenerator` 3.0.2 (3.0.2 = laatste stabiele, NuGet-index)
- `docs/projectinfo/features/mediatr-fix-plan.md` / `-verify.md`, `README.md`, `architectuur/core.md`, `claude/docs/INDEX.md`, `claude/tasks/mediatr-queue.md`

Noot: `CombinedProofTests` blijft FA-only; een MediatR-`_Combined` hoort bij de patroon-loop.

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/features/mediatr-fix-plan.md`
- `claude/docs/patroon-loop.md`
- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
