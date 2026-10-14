# TAAK-024 — Same-named types in the namespace swap

**Status**: done
**Created**: 2026-10-11
**Found in**: TAAK-023, section Blocker

## Problem

`UsingDirectives.UpdateAsync` only swaps `using MediatR;` if nothing binds to `MediatR` anymore, and `ProjectPackages`
keeps the package as long as a symbol from the assembly is used. `IRequest<T>`, `Unit`, `IRequestHandler<,>` etc. exist
in Mediator with the same name, but hold on to the using and the package: `after` never compiles. A separately referenced
`MediatR.Contracts` stays → `IRequest` ambiguous.

## What, where, how, why

| What                     | Where                                                                      | How                                                                                                   | Why / improvement                                                                                                                      |
| ------------------------ | -------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `SwappedTypes`           | `src/Unshackle.Core/Abstractions/IFix.cs`                                  | default member `=> []`, names like `MediatR.IRequest\`1`                                              | FA needs to do nothing; metadata name is unambiguous (arity)                                                                           |
| Swap ignores twins       | `src/Unshackle.Core/UsingDirectives.cs`                                    | `IsSwapped(symbol)`: type or `ContainingType`, `OriginalDefinition`                                   | Members too (`Unit.Value`, `sender.Send`) do not count as usage                                                                        |
| File without a pattern   | `src/Unshackle.Core/Fixer.cs`                                              | `HasUnusedRemovedUsingAsync` → `UpdateAsync` in that case too                                         | A file with only `IRequest<T>` needs no pattern, but does need the swap; without this check every file would get `using Mediator;`     |
| Package check            | `src/Unshackle.Core/ProjectPackages.cs`                                    | skip `IsSwapped`; match on `RemovedPackage` ∪ `AlsoRemovedPackages`                                   | Package is removed if only twins remain                                                                                                |
| Extra old package        | `Models/PackageMigration.cs`, `DataAccess/Projects/XmlProjectFileStore.cs` | `AlsoRemovedPackages` (default `[]`), removed from references and `PackageVersion` unless kept        | `MediatR.Contracts` next to Mediator gives ambiguous types                                                                             |
| List                     | `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs`                           | 11 types, no `IPipelineBehavior`                                                                      | Signature/constraint differs (`MessageHandlerDelegate`, `IMessage`) → own pattern (row 4)                                              |

## Steps

- [x] 1. Verify types in the real assemblies (theory test per type against `TestReferences.MediatR`/`.Mediator`).
- [x] 2. `IFix.SwappedTypes`, `UsingDirectives`, `ProjectPackages`, `Fixer`.
- [x] 3. `PackageMigration.AlsoRemovedPackages` + `XmlProjectFileStore`.
- [x] 4. Tests: `SwappedTypesTests`, `XmlProjectFileStoreTests.EditAsync_AlsoRemovedPackageReferenced_RemovesIt`.
- [x] 5. Plan/verify, `core.md`, INDEX, README; `mediatr-queue.md` row 1 back to open.

## Result

Build 0 warnings, 295 tests green, sample 38 green, hook green. Shortcut: a contracts project without `MediatR` also gets
`Mediator.SourceGenerator` (the first added package comes in "transitively").

## See also

- `claude/tasks/TAAK-023-mediatr-request-handler.md`
- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/features/gelijknamige-typen-swap-plan.md`
