# TAAK-144 — MediatR: evo2 does not build after the fix (CS0104, CS0122/CS0436, CS0535)

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/archief/TAAK-141-mediatr-assembly-parameter-discard.md` (evo2 after `_ = assembly;`)

## Analysis per error class (evo2 = evolutionary-architecture ch. 2, fresh clone)

| Error       | Cause                                                                                                                                                                                                                                                                              | Safe automatically?                                                                         |
| ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| CS0104 (4)  | `ContractsModule.cs` has `using Application;` with its own `ICommand`/`ICommand<T>`; the new `using Mediator;` brings in `Mediator.ICommand`/`ICommand<T>`                                                                                                                          | **Yes**: references to the own type get their full name (`global::Ns.ICommand<T>`)          |
| CS0535 (4)  | Follow-up error of CS0104 (signature with an ambiguous type)                                                                                                                                                                                                                       | solved with CS0104                                                                          |
| CS0436 (70) | `Mediator.SourceGenerator` in 5 projects (Common.Infrastructure, Contracts.Infrastructure, Offers.Api, Passes.Api, Common.IntegrationTests); each writes a public `Mediator.Mediator`/`MediatorOptions`/`AssemblyReference`, which clash with those from a referenced project | **No**                                                                                      |
| CS0122 (12) | The generator in Passes.Api sees the `internal` handlers of Contracts.Application (referenced) and writes code that calls them                                                                                                                                                    | **No**                                                                                      |

Why the generator cannot go "only into the right project": evo2 registers MediatR per module. `Common.Infrastructure.MediatorModule.AddMediator(services, assembly)` is a **public** method that Offers and Passes call with their own assembly; Contracts.Infrastructure scans `typeof(IContractsModule).Assembly` (Contracts.Application) with **internal** handlers. Mediator has one generated mediator per host, which can only call public handlers from referenced projects. `MediatorOptions.Assemblies` / `GenerateTypesAsInternal` do not help either: several `AddMediator` calls in one container give one `ISender` (the first `TryAdd`), which does not know the handlers of the other modules → error at run time. That is a design choice for the user (make handlers public and one `AddMediator` in the host), so Manual.

## Chosen

1. `AddMediatRPattern` → Manual (and through `AllOrNothing` the solution stays untouched) when:
    - a dropped `RegisterServicesFrom...` gets a parameter of a method that is visible outside the assembly (the handlers are with the caller; proof `edge-assemblies-public`). An `internal` method as in evo1 keeps `_ = assembly;` (TAAK-141; proof `edge-assemblies` now `internal`).
    - the scanned assembly (via `typeof(T)`, `<T>` or a local variable holding it) is another project with a non-public handler (CS0122; `AddMediatRPatternTests`). Public handlers in another project (eShopOnWeb, ardalis) stay Fixed.
2. `UsingDirectives.UpdateAsync`: when an added namespace comes in, every reference to an own type with the same name and arity as a type in that namespace gets the full name. The types are in `IFix.AddedTypes` (Mediator 3.0.2 + the three generated ones; `SwappedTypesTests` compares with the real assembly). Proof `RequestHandler/edge-own-command-name`.

## Steps

- [x] Unit tests `AddMediatRPatternTests` (public method, internal/public handler in another project), `SwappedTypesTests.AddedTypes_...`
- [x] Proofs `AddMediatR/edge-assemblies-public` (manual), `AddMediatR/edge-assemblies` (internal), `RequestHandler/edge-own-command-name`
- [x] Pattern docs `mediatr-add-mediatr-plan.md` / `-verify.md`
- [x] Proof with `claude/tools/corpus-regression.sh` on a fresh clone (`<corpus>/src/evo`, output `<corpus>/t144/out/`):
      evo2 status `ok`: 2 Manual reasons (public `MediatorModule.AddMediator`, internal `PrepareContractCommandHandler`), solution unchanged, build green, 39/39 → 39/39.
      evo1 status `ok`: 8 Fixed, build green, 45/45 → 45/45, 0 regressions (flaky: time-dependent validator test names, also before the fix).

## Not done

- A `global using Mediator` (via `SwapGlobalUsings`) is not checked for name conflicts (shortcut in code).
- `OnlyWithCallTo = ["AddMediator", ...]` also matches an own method with that name (evo2's `MediatorModule.AddMediator`), so Offers/Passes got the generator. With the Manual above harmless for evo2; to be picked up separately if it shows up elsewhere.

## See also

- `claude/tasks/archief/TAAK-141-mediatr-assembly-parameter-discard.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/REGRESSIE-corpus.md`
