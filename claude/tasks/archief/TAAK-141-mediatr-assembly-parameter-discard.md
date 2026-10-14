# TAAK-141 — MediatR: really solve IDE0060 on the assembly parameter

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/REGRESSIE-corpus.md` (evo1, evo2: BUILD-AFTER-FIX-FAILS on IDE0060)

## Problem

TAAK-117 leaves the parameter that only went to `RegisterServicesFromAssembly(...)` in place and gives a warning.
Projects with `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` (evo1, evo2) do not build afterwards.

## Trade-off

- Removing the parameter: changes the signature and callers (other files, public API such as evo2's `MediatorModule`).
- Renaming to `_`: breaks callers with a named argument.
- **Chosen**: `_ = parameter;` before the call. Signature and callers stay the same, IDE0060 counts it as usage,
  and the lambda (which the Mediator generator reads from source) stays clean. For that, `Find` returns a call that is a
  standalone statement as that statement, so `Rewrite` can return two statements (an existing mechanism in the Fixer).
  If the call is inside an expression (`return services.AddMediatR(...)`), the TAAK-117 warning stays.

## Steps

- [x] Tests: `src/Unshackle.Tests/Core/AddMediatRPatternTests.cs` (statement → `_ = handlers;`, expression → warning)
- [x] `AddMediatRPattern.Find`/`Rewrite`
- [x] Proof `Proofs/MediatR/AddMediatR/edge-assemblies.after.cs`
- [x] Proof: a copy of the evo1/evo2 output with `_ = assembly;` in `<corpus>/t141/`.
      evo1 builds, 45/45 tests green. evo2: IDE0060 gone, but underneath there are CS0104 (own `ICommand` vs `Mediator.ICommand`),
      CS0122/CS0436 (Mediator generator in several projects) and CS0535; a separate problem, no task yet.
      (The harness could not be used: there is no original evo source in the corpus folder.)

## See also

- `claude/tasks/archief/TAAK-117-mediatr-ongebruikte-assembly-parameter.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/REGRESSIE-corpus.md`
