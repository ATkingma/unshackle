# TAAK-117 — MediatR: unused assembly parameter after AddMediatR → AddMediator (IDE0060)

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/archief/TAAK-115-usings-stijl-en-overbodig.md` (evo1)

## Problem

`mediatr.add-mediatr` drops `cfg.RegisterServicesFromAssembly(assembly)`. If the parameter `assembly` only went
there, IDE0060 reports "unused parameter"; with `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` the build breaks.

## Trade-off

- Adding `_ = assembly;`: the pattern rewrites an expression, not a statement; an extra statement in the
  method is more invasive and ugly. Putting it in the lambda may disturb the Mediator generator (which reads the lambda from source).
- Removing the parameter: changes the signature and all callers (other files, possibly public API).
- Manual: throws away an otherwise correct migration.
- **Chosen**: `Fixed` with a warning in the report that names the parameter and advises removing it (and the argument
  at the callers). The code compiles fine; only projects that enforce IDE0060 as an error
  have to remove that one parameter by hand, and the report says which.

## Steps

- [x] Test first: `src/Unshackle.Tests/Core/AddMediatRPatternTests.cs` (warning for a parameter that is only passed on, none for other use)
- [x] `AddMediatRPattern.UnusedParameters`
- [x] Proof: `Proofs/MediatR/AddMediatR/edge-assemblies` (parameter stays, after compiles)

## See also

- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/archief/TAAK-115-usings-stijl-en-overbodig.md`
