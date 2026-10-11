# TAAK-063 — mediatr: source generator alleen in het project dat AddMediator aanroept

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Doel**: geen CS0436-warnings (dubbele `Mediator`, `MediatorOptions`, `AssemblyReference`) in testprojecten na de fix.

## Context

- Gevonden in TAAK-037: na `fix` op een kopie van SampleShop krijgen `SampleShop.Tests` en `SampleShop.Advanced.Tests` ook `Mediator.SourceGenerator`. Die genereert daar een eigen `Mediator`-implementatie die botst met die uit `SampleShop` → tientallen CS0436-warnings (met `TreatWarningsAsErrors` een build-fout). De tests slagen wel (38/38).
- `MediatRFix.Package.AddedPackages` zet beide pakketten in elk project dat MediatR had.

## Stappen

- [x] 1. Generator alleen toevoegen aan projecten die `AddMediatR` aanroepen (of geen project-reference naar zo'n project hebben); andere projecten alleen `Mediator.Abstractions`.
- [x] 2. Unittest in `ProjectPackages`/project-file-tests; end-to-end op SampleShop zonder CS0436.

## Zie ook

- `claude/tasks/archief/TAAK-037-mediatr-unsupported.md`
- `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs`
- `src/Unshackle.Core/ProjectPackages.cs`
