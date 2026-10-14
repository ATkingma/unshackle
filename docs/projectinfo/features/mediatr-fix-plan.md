---
kingmadoc: 1
feature: "mediatr-fix"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Models/PackageMigration.cs
    - src/Unshackle.Core/Models/AddedPackage.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs
    - src/Unshackle.Interface/Cli.cs
    - src/Unshackle.Tests/ProofTests.cs
    - src/Unshackle.Tests/Helpers/TestReferences.cs
    - src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs
    - src/Unshackle.Tests/Interface/CliTests.cs
    - Directory.Packages.props
---

# Feature: MediatR fix (infrastructure)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-020. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

A second fix `mediatr` (MediatR 12.5.0 → Mediator 3.0.2) without patterns, wired to the CLI and proof runner, with a
`PackageMigration` that supports multiple packages to add (`Mediator.Abstractions` + `Mediator.SourceGenerator`).

## Scope (in / out)

**In scope**

- `PackageMigration.AddedPackages` (list of name + version); the first package replaces the old one in place, the rest goes next to it.
- `MediatRFix` (`Id = "mediatr"`, namespaces `MediatR` → `Mediator`), registration in `Cli.cs`.
- Proof runner picks fix and references by `Proofs/<Fix>/`; `TestReferences.MediatR` / `.Mediator`.
- `Mediator.SourceGenerator` 3.0.2 in `Directory.Packages.props`.

**Out of scope**

- Patterns (see `claude/tasks/mediatr-queue.md`), `MediatRSyntax`, changes in `samples/`.

## Requirements

- **REQ-1**: WHEN a fix has multiple packages to add THE SYSTEM SHALL put the first in place where the old package was and the others directly after it, in both `<PackageReference>` and `<PackageVersion>`.
- **REQ-2**: WHEN the FluentAssertions fix runs THE SYSTEM SHALL produce the same project files as before this change.
- **REQ-3**: WHEN the user passes `--fix mediatr` THE SYSTEM SHALL accept the fix.
- **REQ-4**: WHEN a proof is under `Proofs/MediatR/` THE SYSTEM SHALL run it with `MediatRFix`, compiling `before` against MediatR 12.5.0 and `after` against Mediator.Abstractions 3.0.2.
- **REQ-5**: WHEN `MediatRFix` adds packages THE SYSTEM SHALL use `Mediator.Abstractions` and `Mediator.SourceGenerator` with the same version (3.0.2).

## Assumptions

- Mediator 3.0.2 is the latest stable version (NuGet index, 2026-10-11); the assembly is called `Mediator`, namespace `Mediator` (xml docs of the package).
- _(inferred)_ MediatR 12 puts `IRequest`/`INotification`/`Unit` in `MediatR.Contracts`; the proof references include that assembly.

## Risks

- Extra package in the wrong place → add the extras first "as if the old one stays" (next to the old element), then the first one in place.
- FA behavior changes → existing `XmlProjectFileStoreTests` and `FixerProjectFileTests` stay green unchanged.

## C4 Container (Mermaid)

Unchanged compared to [projectbestanden-migreren](projectbestanden-migreren-plan.md); Core has a second `IFix`.

## Open questions

- [x] Smoke proof `MediatR/Usings`? → only possible with a pattern: the Fixer only swaps usings in documents a pattern changed, and the proof runner runs per pattern. The first MediatR pattern delivers the first proof.
