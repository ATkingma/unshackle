# Verification: mediatr-internal-handler-access

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-internal-handler-access-plan.md`](mediatr-internal-handler-access-plan.md) |
| **Status**    | Verified (proof, unittests, ardalis-clone) |
| **Generated** | 2026-10-12 (met de hand, kingmadoc ontbreekt) |

## Requirements

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `Proofs/MediatR/_Combined/add-mediatr-internal-handlers-other-project` (drie projecten Core/UseCases/Web zoals ardalis; nieuw meerprojectformaat `// @project Naam -> Referenties`, `Helpers/TestProjects.cs`) |
| REQ-2 | Gehaald | idem: alle bevindingen Fixed; `after` compileert per project, generator in Web |
| REQ-3 | Gehaald | `InternalHandlerAccessPatternTests.FixAsync_TwoHostsScanInternalHandlers_OneAttributePerHostInOneFile` |
| REQ-4 | Gehaald | `..._LibraryAlreadyGrantsHost_AddsNoAttribute`, `..._NoOtherProjectScansLibrary_AddsNothing` |
| REQ-5 | Gehaald | `CompileErrors_GeneratedMediatorWithoutInternalsVisibleTo_IsCS0122` |
| REQ-6 | Gehaald | `_Combined/internal-message-access` (after compileert met generator; TAAK-212) |
| REQ-7 | Gehaald | `_Combined/host-references-host`: UserAccess.Infrastructure (host) krijgt geen `InternalsVisibleTo`, Registrations.Application wel (TAAK-213) |

## Validation results

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: alle MediatR-tests en -proofs van deze taak groen; de overige rode tests zijn de bekende CRLF-tests en half werk van parallelle agents.
- ardalis v9.1.0 (verse clone, `unshackle fix --fix mediatr --force`, samen met de vendored SharedKernel van een parallelle taak): 31 Fixed, 0 Manual; het attribuut komt in `Clean.Architecture.Core/ContributorAggregate/Handlers/ContributorDeletedHandler.cs`. Core, UseCases, Infrastructure en Web bouwen; alleen `tests/Clean.Architecture.UnitTests/NoOpMediator.cs` (eigen `IMediator`-implementatie met `Task`) faalt: ander patroon, buiten deze taak.

## Deviations

- Proofs onder `_Combined/` kunnen nu meerdere projecten bevatten; per-patroonproofs (`ProofTests`) blijven één bestand.
- `HostsAsync` leest per gekozen bestand alle projecten (eerst een tekstfilter op `AddMediatR`); geen cache.

## Vervolg TAAK-212 (2026-10-13)

REQ-6: internal domain event zonder handler krijgt ook `InternalsVisibleTo` (CS0122 in kgrzybek). Build `dotnet build` ok; `dotnet test` 1062/1062 groen. Corpus kgrzybek: zie `claude/tasks/CORPUSRUN-mediatr.md` "Herhaling ronde 5".

## Zie ook

- [mediatr-internal-handler-access-plan.md](mediatr-internal-handler-access-plan.md)
- [mediatr-add-mediatr-verify.md](mediatr-add-mediatr-verify.md)
- `claude/tasks/TAAK-195-mediatr-addmediatr-projectgrenzen.md`
