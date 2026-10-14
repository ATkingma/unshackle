# TAAK-020 — MediatRFix infrastructure (MediatR 12.5.0 → Mediator 3.x)

**Status**: done
**Created**: 2026-10-11
**Goal**: a second `IFix` (`mediatr`) exists and is wired to the CLI and the proof runner, without patterns; the patterns come from `claude/tasks/mediatr-queue.md`.

## Context for the next agent

- Roadmap step 3 in `docs/projectinfo/project.md`. Design example: `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`.
- Empty folders already exist: `src/Unshackle.Core/Fixes/MediatR/Patterns/`.
- Packages in `Directory.Packages.props`: `MediatR` 12.5.0, `Mediator.Abstractions` 3.0.2 (types for proofs).
- Target code: `samples/SampleShop/SampleShop/` (handlers, `LoggingBehavior`, `ServiceCollectionExtensions.AddMediatR`) and `SampleShop.Tests/OrderHandlerTests.cs` (`ISender.Send`).
- After this task the pattern loop (`claude/docs/patroon-loop.md`) runs on `claude/tasks/mediatr-queue.md`.

## Analysis

### Current situation

`IFix` has one implementation; `Cli.cs:24`, `ProofTests.cs:23` and `CombinedProofTests.cs:37/53` hard-code `FluentAssertionsFix`. `PackageMigration` knows exactly one added package.

### Problem

- Mediator needs **two** packages: `Mediator.Abstractions` (types) and `Mediator.SourceGenerator` (generates `AddMediator` and the dispatcher). `PackageMigration` cannot express that.
- The proof runner does not pick the fix per proof folder; proofs under `Proofs/MediatR/` would run with the FA fix.
- Pattern IDs use the prefix `fa.`; for this fix it becomes `mediatr.`.

### New situation

`MediatRFix` (`Id = "mediatr"`, `RemovedNamespaces = ["MediatR"]`, `AddedNamespaces = ["Mediator"]`), the CLI accepts `--fix mediatr`, proofs under `Proofs/MediatR/` run against MediatR (before) and Mediator.Abstractions (after).

### Improvement

Roadmap step 3 can go through the existing pattern loop, in parallel per pattern, without pattern agents having to change contracts (which the agent brief forbids).

## Approach

### What

1. `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` with an empty `Patterns` list.
2. Contract change `PackageMigration`: multiple added packages (e.g. `AddedPackages` as a list of name+version). Pin the Mediator 3.x version for both packages (same version; now 3.0.2 in props, add `Mediator.SourceGenerator`).
3. Registration in `Cli.cs` and a fix per proof folder in `ProofTests.cs`/`CombinedProofTests.cs`.
4. Helper class `MediatRSyntax` (like `FluentAssertionsSyntax`) only if the first pattern needs it — not upfront.
5. Docs: row in `docs/projectinfo/README.md`, `architectuur/core.md` mentions the second fix, `INDEX.md` → `mediatr-queue.md`.

### Where

`src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs`, `src/Unshackle.Core/Models/PackageMigration.cs` (+ its DataAccess usage), `src/Unshackle.Interface/Cli.cs`, `src/Unshackle.Tests/ProofTests.cs`, `src/Unshackle.Tests/CombinedProofTests.cs`, `Directory.Packages.props`.

### How

Copy the shape of `FluentAssertionsFix`. Extend `PackageMigration` minimally and adjust the FA fix along with it; existing tests must stay green. One smoke proof (`using MediatR;` → `using Mediator;` on a file without other MediatR code) proves the wiring.

### Why this approach

Change contracts now, before the pattern loop, because pattern agents are not allowed to. No `MediatRSyntax` upfront: YAGNI.

## Steps

- [x] 1. `PackageMigration` multiple packages + adjust FA fix and DataAccess, tests green.
- [x] 2. Create `MediatRFix`, register in `Cli.cs`.
- [x] 3. Proof runner: pick fix by proof folder (`Proofs/<Fix>/`). Smoke proof not possible without a pattern (the Fixer only swaps usings after a pattern change); the first pattern delivers the first proof.
- [x] 4. `Mediator.SourceGenerator` in `Directory.Packages.props`.
- [x] 5. Docs (README, core.md, INDEX.md).
- [x] 6. `dotnet build`, `dotnet test`, sample tests green; row 0 in `mediatr-queue.md` set to `klaar`.

## Risks & edge cases

| Risk | Mitigation |
| ---- | ---------- |
| Mediator needs the source generator; without it `AddMediator` does not compile | both packages via `PackageMigration` |
| Mixed code (FA fix and MediatR fix at the same time in SampleShop) | run `CombinedProofTests` per fix |
| `using MediatR` only disappears when nothing binds anymore; Manual cases keep it | existing Fixer behaviour, leave as is |

## Changed files

- `src/Unshackle.Core/Models/PackageMigration.cs`, `AddedPackage.cs` (new): `AddedPackages` list
- `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` (new), `FluentAssertionsFix.cs`
- `src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs`: extra packages next to the old one, the first in place
- `src/Unshackle.Interface/Cli.cs`
- `src/Unshackle.Tests/ProofTests.cs`, `Helpers/TestReferences.cs`, `Interface/CliTests.cs`, `DataAccess/Projects/XmlProjectFileStoreTests.cs`, `Core/FixerProjectFileTests.cs`
- `Directory.Packages.props`: `Mediator.SourceGenerator` 3.0.2 (3.0.2 = latest stable, NuGet index)
- `docs/projectinfo/features/mediatr-fix-plan.md` / `-verify.md`, `README.md`, `architectuur/core.md`, `claude/docs/INDEX.md`, `claude/tasks/mediatr-queue.md`

Note: `CombinedProofTests` stays FA-only; a MediatR `_Combined` belongs to the pattern loop.

## See also

- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/features/mediatr-fix-plan.md`
- `claude/docs/patroon-loop.md`
- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
