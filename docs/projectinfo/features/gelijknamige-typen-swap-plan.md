---
kingmadoc: 1
feature: "gelijknamige-typen-swap"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Abstractions/IFix.cs
    - src/Unshackle.Core/UsingDirectives.cs
    - src/Unshackle.Core/ProjectPackages.cs
    - src/Unshackle.Core/Fixer.cs
    - src/Unshackle.Core/Models/PackageMigration.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs
    - src/Unshackle.Tests/Core/SwappedTypesTests.cs
    - src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs
---

# Feature: same-named types in the namespace swap

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-024 (blocker of TAAK-023).

## One-sentence summary

`IFix.SwappedTypes` lists types that have a same-named twin in the new namespace; references to them no longer hold on to the
old `using` and the old package, so `using MediatR;` → `using Mediator;` swaps and the package goes away.

## Scope (in / out)

**In scope**

- `IFix.SwappedTypes` (empty by default, `Namespace.MetadataName`), filled by `MediatRFix`.
- `UsingDirectives.UsedNamespaces` and `ProjectPackages.UsesAssemblyAsync` skip symbols of those types (and their members).
- `Fixer` also swaps the using in a file without a pattern change if only swapped types use the old namespace.
- `PackageMigration.AlsoRemovedPackages` (`MediatR.Contracts`): also removed from `<PackageReference>`/`<PackageVersion>`.

**Out of scope**

- Patterns (`Handle` → `ValueTask`, TAAK-023). `IPipelineBehavior` (different delegate and `IMessage` constraint: its own pattern).

## Requirements

- **REQ-1**: WHEN a file only uses swapped types of the old namespace THE SYSTEM SHALL swap the using in place, even without a pattern change.
- **REQ-2**: WHEN a project only uses swapped types of the old package THE SYSTEM SHALL remove the package (`KeepRemovedPackage = false`).
- **REQ-3**: WHEN another type of the old namespace is still used THE SYSTEM SHALL leave the using and package in place.
- **REQ-4**: WHEN `AlsoRemovedPackages` is referenced directly THE SYSTEM SHALL remove that reference (and `PackageVersion`).
- **REQ-5**: WHEN the FluentAssertions fix runs THE SYSTEM SHALL behave as before (empty list).

## Assumptions

- List verified in the real assemblies (MediatR 12.5.0 + MediatR.Contracts, Mediator.Abstractions 3.0.2): `IBaseRequest`, `IRequest`, `IRequest\`1`, `IRequestHandler\`1/\`2`, `INotification`, `INotificationHandler\`1`, `ISender`, `IPublisher`, `IMediator`, `Unit` exist in both.
- Method signatures differ (`Task` vs `ValueTask`); patterns solve that, not the swap.

## Risks

- A file without changes wrongly gets `using Mediator;` → only swap if there is an unused old using (`HasUnusedRemovedUsingAsync`).
- Contracts project without `MediatR`: is now also migrated (match on every package to remove) and gets the added packages.

## Open questions

- None.

## See also

- [gelijknamige-typen-swap-verify.md](gelijknamige-typen-swap-verify.md)
- [mediatr-fix-plan.md](mediatr-fix-plan.md)
- `claude/tasks/archief/TAAK-024-gelijknamige-typen-swap.md`
