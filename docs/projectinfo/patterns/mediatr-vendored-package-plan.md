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

# Feature: mediatr.vendored-package: a package that builds on MediatR (Ardalis.SharedKernel)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (by hand, kingmadoc missing) |

> Drawn up from TAAK-194 (corpus run 2026-10-12, ardalis v9.1.0: 11× "Ardalis.SharedKernel builds on MediatR").

## One-sentence summary

If a solution uses Ardalis.SharedKernel 1.4.0–3.0.1 (builds on MediatR), the fix puts the source code of that version, with Mediator instead of MediatR, in the project that all users reference, and replaces the package with its own dependencies; namespace, types and behaviour stay the same.

## Scope (in / out)

**In scope**:
- Ardalis.SharedKernel 1.4.0, 1.5.0, 1.6.0, 2.1.0, 2.1.1 and 3.0.1 (all types: `DomainEventBase`, `EntityBase`(`<TId>`, `<T, TId>`), `HasDomainEventsBase`, `IHasDomainEvents`, `IAggregateRoot`, `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`, `IRepository`, `IReadRepository`, `IDomainEventDispatcher`, `MediatRDomainEventDispatcher`, `LoggingBehavior`, `ValueObject`).
- Project files: the package removed from every project, the root project gets Ardalis.GuardClauses, Ardalis.Specification and Microsoft.Extensions.Logging.Abstractions (CPM: an existing `PackageVersion` stays).

**Out of scope**: Ardalis.SharedKernel 1.0.0–1.3.0 (different API, not in the corpus) and other packages on MediatR: Manual as before (TAAK-133). An upgrade to 4.x is deliberately not an option (see Assumptions).

## Requirements

- **REQ-1**: WHEN a project references a supported version of Ardalis.SharedKernel THE SYSTEM SHALL no longer report the Manual "builds on MediatR" and migrate the user code like other MediatR code (proof `_Combined/Vendored/shared-kernel`).
- **REQ-2**: WHEN all users of the package (transitively) reference one of them THE SYSTEM SHALL put the source code of that version in `<that project>/Ardalis.SharedKernel/`, with `LICENSE.txt`, as new files (proof `_Combined/Vendored/shared-kernel`, test `VendoredPackagesTests.FixAsync_UsersShareOneProject_*`).
- **REQ-3**: THE SYSTEM SHALL keep the same namespace, type names, signatures and the runtime behaviour of the version in that source code, on `global::Mediator` (probe: identical output; the proof compiles without warnings).
- **REQ-4**: WHEN the package is vendored THE SYSTEM SHALL remove it from all project files and give the root project its dependencies, in the same operation as the MediatR swap (tests `EditAsync_VendoredPackage_*`, `FixAsync_UsersShareOneProject_*`).
- **REQ-5**: IF the users have no common project, the version is not supported, or the folder already has source files THEN THE SYSTEM SHALL report Manual with the reason (test `FixAsync_UsersShareNoProject_ReportsManual`).
- **REQ-6**: WHEN a new file already exists on disk THE SYSTEM SHALL write nothing and report it (test `SaveAsync_NewFileAlreadyThere_*`).

## Assumptions

- Ardalis.SharedKernel 4.0.0+ builds on Mediator.Abstractions but removed `ICommand`/`IQuery`/handlers, made `ClearDomainEvents` public and requires net9.0: not a drop-in. The source code is MIT (Copyright (c) 2022 Steve Smith); the licence comes along.
- `LoggingBehavior` logged under `ILogger<MediatR.Mediator>` (category `MediatR.Mediator`); the vendored version takes `ILoggerFactory` and creates the same category.
- Same-named `ICommand<T>`, `IQuery<T>` and handlers next to `using Mediator;` get the full name via `AddedTypes` (TAAK-144).
- The fix is all-or-nothing: a Manual here leaves the whole solution untouched.

## Models

[mediatr-vendored-package-modellen.md](mediatr-vendored-package-modellen.md).

## See also

- [mediatr-vendored-package-verify.md](mediatr-vendored-package-verify.md)
- [mediatr-unsupported-plan.md](mediatr-unsupported-plan.md)
- `claude/tasks/archief/TAAK-194-mediatr-vendored-shared-kernel.md`
