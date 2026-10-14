# Verification: mediatr-vendored-package

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-vendored-package-plan.md`](mediatr-vendored-package-plan.md) |
| **Status**    | Verified (proof, unittests, probe, ardalis-clone) |
| **Generated** | 2026-10-13 (by hand) |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/MediatR/_Combined/Vendored/shared-kernel` via `VendoredProofTests` |
| REQ-2 | Met | `VendoredPackagesTests.FixAsync_UsersShareOneProject_PutsSourceThereAndSwapsThePackage` |
| REQ-3 | Met | Probe TAAK-194 (cleaned up): identical output for 1.4.0, 1.5.0, 2.1.0 against the original with MediatR 12.2; the proof compiles without warnings |
| REQ-4 | Met | `XmlProjectFileStoreTests.EditAsync_VendoredPackage_RootSwapsItForItsDependenciesAndOtherProjectLosesIt` |
| REQ-5 | Met | `VendoredPackagesTests.FixAsync_UsersShareNoProject_ReportsManual` |
| REQ-6 | Met | `XmlProjectFileStoreTests.SaveAsync_NewFileAlreadyThere_ThrowsAndWritesNothing` |

## Deviations

- ardalis v9.1.0 (TAAK-195 run): `fix --fix mediatr --force` → 31 Fixed, 0 Manual; Core/UseCases/Infrastructure/Web build. The test project breaks on an own `IMediator` implementation (`NoOpMediator`): TAAK-197.

## See also

- [mediatr-vendored-package-plan.md](mediatr-vendored-package-plan.md)
- `claude/tasks/archief/TAAK-194-mediatr-vendored-shared-kernel.md`
