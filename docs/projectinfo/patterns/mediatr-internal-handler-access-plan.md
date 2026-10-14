---
kingmadoc: 1
feature: "mediatr-internal-handler-access"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/InternalHandlerAccessPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Core/Abstractions/ISolutionFinder.cs
    - src/Unshackle.Core/Fixer.cs
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/
---

# Feature: mediatr.internal-handler-access: InternalsVisibleTo for internal handlers that another project scans

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (by hand, kingmadoc missing) |

> Drawn up from TAAK-195 (corpus run 2026-10-12: ardalis `Program.cs:109` scans `Clean.Architecture.Core` with the internal `ContributorDeletedHandler`; evo2 `MediationModule` scans `Contracts.Application` with internal handlers).

## One-sentence summary

If `AddMediatR` in project A scans another project B of the solution with internal handlers, one file of B gets `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("A")]`, so the code that Mediator's generator writes in A may name those handlers; `mediatr.add-mediatr` then rewrites the call instead of Manual.

## Scope (in / out)

**In scope**:
- Scanned assembly via `typeof(T)`, `<T>` or a local variable holding it (the same recognition as `mediatr.add-mediatr`, shared helper `AddMediatRPattern.ScannedAssemblies`).
- Several hosts scanning the same project: one attribute per host, sorted, in the same file.

**Out of scope**: handlers in a package or an assembly outside the solution (stays Manual in `mediatr.add-mediatr`); strong-named assemblies (a friend then needs a `PublicKey`: Manual); an `Assembly` parameter of a public method and an internal method that a test project calls (evo1/evo2): no exact translation, see TAAK-195.

## Requirements

- **REQ-1**: WHEN `AddMediatR` in project A scans a project B of the solution with a non-public handler THE SYSTEM SHALL put `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("A")]` after the usings in the first file of B (by path) that declares such a handler (proof `_Combined/add-mediatr-internal-handlers-other-project`).
- **REQ-2**: WHEN that is the case THE SYSTEM SHALL rewrite the `AddMediatR` call in A, not report Manual (same proof; `after` compiles per project against Mediator 3.0.2 with the generator in A).
- **REQ-3**: WHEN two projects scan B THE SYSTEM SHALL put two attributes in one file, one finding (`InternalHandlerAccessPatternTests.FixAsync_TwoHostsScanInternalHandlers_OneAttributePerHostInOneFile`).
- **REQ-4**: IF B already gives the host `InternalsVisibleTo`, or nobody scans B THEN THE SYSTEM SHALL add nothing (`..._LibraryAlreadyGrantsHost_AddsNoAttribute`, `..._NoOtherProjectScansLibrary_AddsNothing`).
- **REQ-5**: Without the attribute the result does not build (CS0122 in the generated code; `CompileErrors_GeneratedMediatorWithoutInternalsVisibleTo_IsCS0122`).
- **REQ-6**: WHEN B only has a non-public, non-generic message (request or notification, e.g. a domain event) THE SYSTEM SHALL also give the host `InternalsVisibleTo` from B: the generator names every message it sees, also without a handler (CS0122 in kgrzybek; proof `_Combined/internal-message-access`, TAAK-212).
- **REQ-7**: IF B is a host itself (AddMediatR or Autofac module) THEN THE SYSTEM SHALL add nothing: `InternalsVisibleTo` between two hosts makes their generated types clash (CS0436/CS0121); the referring host limits its `Assemblies` (proof `_Combined/host-references-host`, TAAK-213).

## Assumptions

- Mediator 3.0.2 generates code in the host project that names the handlers from referenced assemblies; with `InternalsVisibleTo` for the host that builds and runs (probe TAAK-183 and TAAK-195).
- Behaviour equal to MediatR: MediatR registers internal handlers via reflection; the attribute only changes visibility, not runtime behaviour.
- `ISolutionFinder` (new contract in `Abstractions/`): a pattern that only knows with the solution whether a file has a finding. Without it every file with an internal handler would get an empty Fixed row.

## Models

[mediatr-internal-handler-access-modellen.md](mediatr-internal-handler-access-modellen.md).

## See also

- [mediatr-internal-handler-access-verify.md](mediatr-internal-handler-access-verify.md)
- [mediatr-add-mediatr-plan.md](mediatr-add-mediatr-plan.md)
- `claude/tasks/archief/TAAK-195-mediatr-addmediatr-projectgrenzen.md`
