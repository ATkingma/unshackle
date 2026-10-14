# TAAK-240 — MediatR: a warning in the report with architecture tests (exclude namespace `Mediator`)

**Status**: ✅ completed
**Finished**: 2026-10-13
**Created**: 2026-10-13 (follow-up of TAAK-214)
**Goal**: whoever migrates MediatR in a solution with NetArchTest/ArchUnitNET is told in the report that archtests must exclude the namespace `Mediator`.

## Context for the next agent

- TAAK-214: kgrzybek's `*Module_DoesNotHave_Dependency_On_Other_Modules` fail after the fix on the generated `Mediator.Mediator` and
  `Mediator.Internals.ContainerMetadata` (they name messages of other modules). A legitimate difference; the solution in the test:
  `.And().DoNotResideInNamespace("Mediator")`.
- TAAK-214 found an automatic report "cannot be derived reliably". Chosen compromise: no analysis of the tests, only a
  warning as soon as a project of the solution references an architecture-test library.

## Steps

- [x] 1. `MediatorHosts.WithArchitectureTestWarning(result, solution)`: adds a `Warning` to a `FixedResult` (after an existing one)
  if a project has a `PortableExecutableReference` whose file name starts with `NetArchTest` (NetArchTest.Rules,
  NetArchTest.eNhancedEdition) or `ArchUnitNET` (TngTech.ArchUnitNET + xUnit/NUnit/MSTest). No I/O: the references come from the Roslyn `Solution`.
- [x] 2. Called in `AddMediatRPattern.RewriteAsync` and `AutofacMediatorModulePattern.RewriteAsync` (only with a solution; Manual stays Manual).
- [x] 3. Unit test `AddMediatRPatternTests.RewriteAsync_SolutionWithArchitectureTests_WarnsAboutMediatorNamespace` (NetArchTest.Rules, ArchUnitNET.xUnit, another dll).
- [x] 4. KingmaDoc plan/verify of `mediatr.add-mediatr` and `mediatr.autofac-mediator-module`, `mediatr-queue.md`, `INDEX.md`.
- [x] 5. Scan (report only) on a copy of kgrzybek `91c8ef2`: the 4 `mediatr.autofac-mediator-module` rows are Fixed with the warning.

## Result

- Tests: `*Mediat*` + `*ProofTests` 820/820 green; new test 3/3.
- kgrzybek scan: warning on `Modules/{Meetings,Administration,UserAccess,Payments}/Infrastructure/Configuration/Mediation/MediatorModule.cs`
  (the first project with the library is named, here `...UserAccess.Domain.UnitTests`).
- Trade-off: the warning also appears when the archtests do not test module dependencies (a false positive, but harmless: the advice is always right).

## See also

- `claude/tasks/archief/TAAK-214-mediatr-archtests-gegenereerde-mediator.md`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Repeat round 6"
- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`, `docs/projectinfo/patterns/mediatr-autofac-mediator-module-plan.md`
