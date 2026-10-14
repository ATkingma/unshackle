# Verification: mediatr-internal-handler-access

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-internal-handler-access-plan.md`](mediatr-internal-handler-access-plan.md) |
| **Status**    | Verified (proof, unittests, ardalis-clone) |
| **Generated** | 2026-10-12 (by hand, kingmadoc missing) |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/MediatR/_Combined/add-mediatr-internal-handlers-other-project` (three projects Core/UseCases/Web like ardalis; new multi-project format `// @project Name -> References`, `Helpers/TestProjects.cs`) |
| REQ-2 | Met | same: all findings Fixed; `after` compiles per project, generator in Web |
| REQ-3 | Met | `InternalHandlerAccessPatternTests.FixAsync_TwoHostsScanInternalHandlers_OneAttributePerHostInOneFile` |
| REQ-4 | Met | `..._LibraryAlreadyGrantsHost_AddsNoAttribute`, `..._NoOtherProjectScansLibrary_AddsNothing` |
| REQ-5 | Met | `CompileErrors_GeneratedMediatorWithoutInternalsVisibleTo_IsCS0122` |
| REQ-6 | Met | `_Combined/internal-message-access` (after compiles with the generator; TAAK-212) |
| REQ-7 | Met | `_Combined/host-references-host`: UserAccess.Infrastructure (host) gets no `InternalsVisibleTo`, Registrations.Application does (TAAK-213) |

## Validation results

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all MediatR tests and proofs of this task green; the other red tests are the known CRLF tests and half-finished work of parallel agents.
- ardalis v9.1.0 (fresh clone, `unshackle fix --fix mediatr --force`, together with the vendored SharedKernel of a parallel task): 31 Fixed, 0 Manual; the attribute goes into `Clean.Architecture.Core/ContributorAggregate/Handlers/ContributorDeletedHandler.cs`. Core, UseCases, Infrastructure and Web build; only `tests/Clean.Architecture.UnitTests/NoOpMediator.cs` (own `IMediator` implementation with `Task`) fails: another pattern, outside this task.

## Deviations

- Proofs under `_Combined/` can now contain several projects; per-pattern proofs (`ProofTests`) stay one file.
- `HostsAsync` reads all projects per chosen file (first a text filter on `AddMediatR`); no cache.

## Follow-up TAAK-212 (2026-10-13)

REQ-6: an internal domain event without a handler also gets `InternalsVisibleTo` (CS0122 in kgrzybek). Build `dotnet build` ok; `dotnet test` 1062/1062 green. Corpus kgrzybek: see `claude/tasks/CORPUSRUN-mediatr.md` "Repeat round 5".

## See also

- [mediatr-internal-handler-access-plan.md](mediatr-internal-handler-access-plan.md)
- [mediatr-add-mediatr-verify.md](mediatr-add-mediatr-verify.md)
- `claude/tasks/TAAK-195-mediatr-addmediatr-projectgrenzen.md`
