# TAAK-213 — MediatR: a generator host refers to another host (kgrzybek CS0436/CS0121)

**Status**: ✅ completed (kgrzybek build green); 4 archtest regressions → TAAK-214
**Finished**: 2026-10-13
**Created**: 2026-10-13 (from TAAK-212, corpus round 5)
**Goal**: kgrzybek `91c8ef2` builds after `fix --fix mediatr`.

## Context for the next agent

- `_results/r5/kgr/logs/build1.log`: 410× CS0436 (`RequestHandlerWrapper`, `NotificationHandlerWrapper`, `ContainerMetadata`, `Mediator`, ...)
  and 2× CS0121 (`AddMediator` ambiguous), only in `Registrations.Infrastructure`.
- `Registrations.Infrastructure` refers to `UserAccess.Infrastructure`; both get `Mediator.SourceGenerator` (Autofac `MediatorModule`).
  The public generated `AddMediator` extension of UserAccess clashes (CS0121); `UserAccess.Infrastructure` gives Registrations
  `InternalsVisibleTo` (internal-handler-access), so the internal generated types clash too (CS0436).
- The generator (3.0.2) does not check accessibility: without IVT it becomes CS0122 for the internal handlers of UserAccess.Infrastructure.
- Hidden in round 4: UserAccess.Infrastructure already broke on MSG0001 then, so Registrations was not compiled.

## Steps

- [x] 1. Repro: two host projects of which one references the other (probe in scratch + `_Combined/host-references-host`).
- [x] 2. Investigate options: limit `Assemblies`, `GenerateTypesAsInternal`, `Namespace` (see Probe).
- [x] 3. Realisation: `MediatorHosts` + Autofac/AddMediatR/internal-handler-access.
- [x] 4. Proof on kgrzybek with the harness (`_results/r6`).

## Probe (Mediator.SourceGenerator 3.0.2, net8.0, HostB → HostA, both with the generator)

| HostA (referenced) | HostB (referring) | IVT A→B | Result |
| ------------------ | ----------------- | ------- | ------ |
| default | default | yes | CS0121 (`AddMediator`) |
| default | default | no | CS0122 (B names A's internal handler) + CS0436 warnings |
| `GenerateTypesAsInternal` | default | no | CS0122 |
| default | `Assemblies` limited (± `GenerateTypesAsInternal`) | no | CS0122 + CS0436 (A's public types) |
| `GenerateTypesAsInternal` | `Assemblies` limited | yes | CS0121 + 130× CS0436 (also with B internal or `Namespace` in B) |
| **`GenerateTypesAsInternal`** | **`Assemblies = [typeof(own), typeof(lib)]`** | **no** | **builds**, no warnings; B registers its own + lib handlers, not those of A |

- `MediatorOptions`, `AssemblyReference` and `MediatorDependencyInjectionExtensions` are always in `Mediator` / `Microsoft.Extensions.DependencyInjection`;
  `Namespace` only moves `Mediator`, so it does not help. `GenerateTypesAsInternal` makes them all internal.
- The generator reads `Assemblies` from the source code: `typeof(T)` (also `global::`) and `typeof(T).Assembly` work.

## Choices

- **Referenced host** (another host depends on it transitively): `GenerateTypesAsInternal = true`.
- **Referring host**: `Assemblies = [typeof(<type around the call>), typeof(<one type per referenced assembly with MediatR messages/handlers, not a host>)]`,
  sorted by name; per assembly preferably a public message/handler type. That way it sees the same assemblies as the generator by default, minus the other hosts.
  That follows MediatR: kgrzybek's Autofac module only registered `Assemblies.Application` + `ThisAssembly`.
- **No IVT from host to host**: `InternalHandlerAccessPattern` skips a project that is a host itself.
- **Manual**: one host already gives the other `InternalsVisibleTo` itself (no option helps); `AddMediatR` explicitly scans another host;
  no nameable type in an assembly that must be named.
- "Host" = a project with `AddMediatR` or an Autofac module that scans MediatR (`MediatorHosts.AssemblyNamesAsync`). The Fixer passes the
  *working* solution (the current file is already partly rewritten), so an `AddMediator` call counts too (by name).
- Shared code in `Fixes/MediatR/MediatorHosts.cs` (+ model `MediatorHostSettings`); `AutofacMediatorModulePattern` is now an `ISolutionPattern`.
- `TestProjects.CompileErrors` now counts CS0436: a clash of generated types is always wrong in a proof.

## Proof

- Proof `Proofs/MediatR/_Combined/host-references-host` (two Autofac hosts, one refers to the other; after compiles per project with the generator).
- `src/Unshackle.Tests/Core/MediatorHostsTests.cs` (6 tests: host detection, both settings, IVT Manual, AddMediatR variant + Manual).

## Result

- Unshackle: `dotnet build` ok, `dotnet test` 1073/1073 green (incl. 6 `MediatorHostsTests` and the new combined proof), SampleShop 38/38.
- kgrzybek (`_results/r6/kgr`): 218 files fixed, 0 Manual; **build after fix ok** (r5: 410× CS0436 + 2× CS0121). Registrations got `Assemblies`,
  UserAccess `GenerateTypesAsInternal`, no more IVT from UserAccess.Infrastructure. Tests 242/287 (baseline 246/287): 4 regressions in the
  ArchTests (generated `Mediator.Mediator`/`ContainerMetadata` depend on other modules) → **TAAK-214**.

## See also

- `claude/tasks/archief/TAAK-214-mediatr-archtests-gegenereerde-mediator.md`
- `claude/tasks/archief/TAAK-212-mediatr-kgrzybek-build-na-fix.md`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Repeat round 5" and "Repeat round 6"
- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/patterns/mediatr-autofac-mediator-module-plan.md` (REQ-7, REQ-8), `mediatr-internal-handler-access-plan.md` (REQ-7), `mediatr-add-mediatr-plan.md` (REQ-7)
