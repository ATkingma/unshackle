# TAAK-213 — MediatR: generator-host verwijst naar een andere host (kgrzybek CS0436/CS0121)

**Status**: ✅ voltooid (build kgrzybek groen); 4 archtest-regressies → TAAK-214
**Afgerond**: 2026-10-13
**Aangemaakt**: 2026-10-13 (uit TAAK-212, corpus-ronde 5)
**Doel**: kgrzybek `91c8ef2` bouwt na `fix --fix mediatr`.

## Context voor de volgende agent

- `_results/r5/kgr/logs/build1.log`: 410× CS0436 (`RequestHandlerWrapper`, `NotificationHandlerWrapper`, `ContainerMetadata`, `Mediator`, ...)
  en 2× CS0121 (`AddMediator` dubbelzinnig), alleen in `Registrations.Infrastructure`.
- `Registrations.Infrastructure` verwijst naar `UserAccess.Infrastructure`; beide krijgen `Mediator.SourceGenerator` (Autofac `MediatorModule`).
  De publieke gegenereerde `AddMediator`-extensie van UserAccess botst (CS0121); `UserAccess.Infrastructure` geeft Registrations
  `InternalsVisibleTo` (internal-handler-access), waardoor ook de internal gegenereerde typen botsen (CS0436).
- De generator (3.0.2) controleert geen toegankelijkheid: zonder IVT wordt het CS0122 voor de internal handlers van UserAccess.Infrastructure.
- In ronde 4 verborgen: UserAccess.Infrastructure brak toen al op MSG0001, dus Registrations werd niet gecompileerd.

## Stappen

- [x] 1. Repro: twee host-projecten waarvan de ene de andere refereert (probe in scratch + `_Combined/host-references-host`).
- [x] 2. Opties onderzoeken: `Assemblies` beperken, `GenerateTypesAsInternal`, `Namespace` (zie Probe).
- [x] 3. Realisatie: `MediatorHosts` + Autofac/AddMediatR/internal-handler-access.
- [x] 4. Bewijs op kgrzybek met het harnas (`_results/r6`).

## Probe (Mediator.SourceGenerator 3.0.2, net8.0, HostB → HostA, beide met generator)

| HostA (verwezen) | HostB (verwijzend) | IVT A→B | Resultaat |
| ---------------- | ------------------ | ------- | --------- |
| standaard | standaard | ja | CS0121 (`AddMediator`) |
| standaard | standaard | nee | CS0122 (B noemt A's internal handler) + CS0436-waarschuwingen |
| `GenerateTypesAsInternal` | standaard | nee | CS0122 |
| standaard | `Assemblies` beperkt (± `GenerateTypesAsInternal`) | nee | CS0122 + CS0436 (A's publieke typen) |
| `GenerateTypesAsInternal` | `Assemblies` beperkt | ja | CS0121 + 130× CS0436 (ook met B internal of `Namespace` in B) |
| **`GenerateTypesAsInternal`** | **`Assemblies = [typeof(eigen), typeof(lib)]`** | **nee** | **bouwt**, geen waarschuwingen; B registreert eigen + lib-handlers, niet die van A |

- `MediatorOptions`, `AssemblyReference` en `MediatorDependencyInjectionExtensions` staan altijd in `Mediator` / `Microsoft.Extensions.DependencyInjection`;
  `Namespace` verplaatst alleen `Mediator`, dus helpt niet. `GenerateTypesAsInternal` maakt ze allemaal internal.
- `Assemblies` leest de generator uit de broncode: `typeof(T)` (ook `global::`) en `typeof(T).Assembly` werken.

## Keuzes

- **Verwezen host** (een andere host hangt er transitief van af): `GenerateTypesAsInternal = true`.
- **Verwijzende host**: `Assemblies = [typeof(<type rond de aanroep>), typeof(<één type per verwezen assembly met MediatR-messages/handlers, geen host>)]`,
  op naam gesorteerd; per assembly bij voorkeur een publiek message/handler-type. Zo ziet hij dezelfde assemblies als de generator standaard, minus de andere hosts.
  Dat volgt MediatR: de Autofac-module van kgrzybek registreerde alleen `Assemblies.Application` + `ThisAssembly`.
- **Geen IVT van host naar host**: `InternalHandlerAccessPattern` slaat een project over dat zelf host is.
- **Manual**: een host geeft de andere al zelf `InternalsVisibleTo` (geen optie helpt); `AddMediatR` scant expliciet een andere host;
  geen nameable type in een te noemen assembly.
- "Host" = project met `AddMediatR` of een Autofac-module die MediatR scant (`MediatorHosts.AssemblyNamesAsync`). De Fixer geeft de
  *werk*-solution door (het huidige bestand is al deels herschreven), dus een `AddMediator`-aanroep telt ook (op naam).
- Gedeelde code in `Fixes/MediatR/MediatorHosts.cs` (+ model `MediatorHostSettings`); `AutofacMediatorModulePattern` is nu `ISolutionPattern`.
- `TestProjects.CompileErrors` telt CS0436 nu mee: een botsing van gegenereerde typen is in een proof altijd fout.

## Bewijs

- Proof `Proofs/MediatR/_Combined/host-references-host` (twee Autofac-hosts, één verwijst naar de andere; after compileert per project met generator).
- `src/Unshackle.Tests/Core/MediatorHostsTests.cs` (6 tests: hostdetectie, beide settings, IVT-Manual, AddMediatR-variant + Manual).

## Resultaat

- Unshackle: `dotnet build` ok, `dotnet test` 1073/1073 groen (incl. 6 `MediatorHostsTests` en de nieuwe combined proof), SampleShop 38/38.
- kgrzybek (`_results/r6/kgr`): 218 bestanden gefixt, 0 Manual; **build na fix ok** (r5: 410× CS0436 + 2× CS0121). Registrations kreeg `Assemblies`,
  UserAccess `GenerateTypesAsInternal`, geen IVT meer van UserAccess.Infrastructure. Tests 242/287 (basis 246/287): 4 regressies in de
  ArchTests (gegenereerde `Mediator.Mediator`/`ContainerMetadata` hangen af van andere modules) → **TAAK-214**.

## Zie ook

- `claude/tasks/archief/TAAK-214-mediatr-archtests-gegenereerde-mediator.md`
- `claude/tasks/archief/TAAK-212-mediatr-kgrzybek-build-na-fix.md`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Herhaling ronde 5" en "Herhaling ronde 6"
- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/patterns/mediatr-autofac-mediator-module-plan.md` (REQ-7, REQ-8), `mediatr-internal-handler-access-plan.md` (REQ-7), `mediatr-add-mediatr-plan.md` (REQ-7)
