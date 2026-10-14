# TAAK-147 — MediatR: remaining points of TAAK-144 (generator by name, global using, InternalsVisibleTo)

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/archief/TAAK-144-mediatr-evo2-naamconflict-generator.md` (Not done, `shortcut:` comments)

## Assignment and choice per point

1. **Generator by name.** `OnlyWithCallTo = ["AddMediator", "AddMediatR"]` matched syntactically on the fixed code, so also an own `AddMediator` (evo2's `MediatorModule`). Now: `ProjectPackages.ForProjectAsync` looks at the **original** code and only counts calls that bind semantically to the removed package (MediatR's `AddMediatR`). `OnlyWithCallTo = ["AddMediatR"]`; `PlanAsync` gets the original solution. Test `SwappedTypesTests.FixAsync_ProjectCallingOwnAddMediator_AddsNoSourceGenerator`.
2. **Global using.** A swapped `global using MediatR;` → `global using Mediator;` reaches every file of the project, but `UsingDirectives.Clashing` only ran for locally added usings. Now: `Fixer.SwapGlobalUsingsAsync` calls `UsingDirectives.QualifyClashingAsync` on every document of the project (the same full-name qualification). Proof `RequestHandler/edge-own-command-name-global`, test `SwappedTypesTests.FixAsync_GlobalUsingSwappedWithOwnTypeOfSameName_QualifiesOwnTypeInEveryFile`.
3. **InternalsVisibleTo.** An `internal` registration method with a scanned assembly parameter in an assembly with `InternalsVisibleTo` (except `DynamicProxyGenAssembly2`, the mocking proxy) → Manual. A pattern does not see the solution, so every friend counts (shortcut in code). Proof `AddMediatR/edge-assemblies-friend`, test `AddMediatRPatternTests.Rewrite_AssemblyParameterOfInternalMethodWithFriend_ManualUnlessMockingProxy`.

## Steps

- [x] Code (Core: `ProjectPackages`, `Fixer`, `UsingDirectives`, `AddMediatRPattern`, `MediatRFix`)
- [x] Proofs + unit tests (see above)
- [x] Pattern docs `mediatr-add-mediatr-plan.md` / `-verify.md`
- [x] `dotnet build`, `dotnet test` (724/724), samples 38/38
- [x] Harness `claude/tools/corpus-regression.sh` on the fresh source `<corpus>/src/evo`, output `<corpus>/t147/out/`:
      evo1 `ok` 45/45 → 45/45, 0 regressions. **Now Manual** (point 3): `Fitnet.IntegrationTests` really calls the internal `AddInMemoryEventBus(Assembly.GetExecutingAssembly())` via `InternalsVisibleTo`; solution unchanged.
      evo2 `ok` 39/39 → 39/39, 0 regressions, 2 Manual as in TAAK-144, solution unchanged.

## Not done

- Points 1 and 2 are not hit by evo1/evo2 (both solutions stay untouched through `AllOrNothing`); only covered by unit tests/proofs.
- Point 3 does not check whether the friend really calls the method (a false positive gives Manual, never broken code).

## See also

- `claude/tasks/archief/TAAK-144-mediatr-evo2-naamconflict-generator.md`
- `claude/tasks/archief/TAAK-141-mediatr-assembly-parameter-discard.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/REGRESSIE-corpus.md`
