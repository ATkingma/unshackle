# TAAK-024 — Gelijknamige typen in de namespace-swap

**Status**: voltooid
**Aangemaakt**: 2026-10-11
**Gevonden in**: TAAK-023, sectie Blokkade

## Probleem

`UsingDirectives.UpdateAsync` wisselt `using MediatR;` alleen als niets meer aan `MediatR` bindt, en `ProjectPackages`
houdt het pakket zolang een symbool uit de assembly gebruikt wordt. `IRequest<T>`, `Unit`, `IRequestHandler<,>` enz. bestaan
in Mediator met dezelfde naam, maar houden using en pakket vast: `after` compileert nooit. `MediatR.Contracts` los
gerefereerd blijft staan → `IRequest` ambigu.

## Wat, waar, hoe, waarom

| Wat                     | Waar                                                                       | Hoe                                                                                                  | Waarom / verbetering                                                                                                                    |
| ----------------------- | -------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| `SwappedTypes`          | `src/Unshackle.Core/Abstractions/IFix.cs`                                  | default-member `=> []`, namen als `MediatR.IRequest\`1`                                              | FA hoeft niets te doen; metadata-naam is eenduidig (ariteit)                                                                            |
| Swap negeert tweelingen | `src/Unshackle.Core/UsingDirectives.cs`                                    | `IsSwapped(symbol)`: type of `ContainingType`, `OriginalDefinition`                                  | Ook leden (`Unit.Value`, `sender.Send`) tellen niet als gebruik                                                                         |
| Bestand zonder patroon  | `src/Unshackle.Core/Fixer.cs`                                              | `HasUnusedRemovedUsingAsync` → ook dan `UpdateAsync`                                                 | Een bestand met alleen `IRequest<T>` heeft geen patroon nodig, wel de swap; zonder deze check zou elk bestand `using Mediator;` krijgen |
| Pakketcheck             | `src/Unshackle.Core/ProjectPackages.cs`                                    | `IsSwapped` overslaan; match op `RemovedPackage` ∪ `AlsoRemovedPackages`                             | Pakket gaat weg als alleen tweelingen overblijven                                                                                       |
| Extra oud pakket        | `Models/PackageMigration.cs`, `DataAccess/Projects/XmlProjectFileStore.cs` | `AlsoRemovedPackages` (default `[]`), verwijderd uit referenties en `PackageVersion` tenzij gehouden | `MediatR.Contracts` naast Mediator geeft ambigue typen                                                                                  |
| Lijst                   | `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs`                           | 11 typen, geen `IPipelineBehavior`                                                                   | Signatuur/constraint verschilt (`MessageHandlerDelegate`, `IMessage`) → eigen patroon (rij 4)                                           |

## Stappen

- [x] 1. Typen verifiëren in de echte assemblies (theory-test per type tegen `TestReferences.MediatR`/`.Mediator`).
- [x] 2. `IFix.SwappedTypes`, `UsingDirectives`, `ProjectPackages`, `Fixer`.
- [x] 3. `PackageMigration.AlsoRemovedPackages` + `XmlProjectFileStore`.
- [x] 4. Tests: `SwappedTypesTests`, `XmlProjectFileStoreTests.EditAsync_AlsoRemovedPackageReferenced_RemovesIt`.
- [x] 5. Plan/verify, `core.md`, INDEX, README; `mediatr-queue.md` rij 1 terug op open.

## Resultaat

Build 0 warnings, 295 tests groen, sample 38 groen, hook groen. Shortcut: een contracts-project zonder `MediatR` krijgt ook
`Mediator.SourceGenerator` erbij (eerste toegevoegde pakket komt "transitief" binnen).

## Zie ook

- `claude/tasks/TAAK-023-mediatr-request-handler.md`
- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/features/gelijknamige-typen-swap-plan.md`
