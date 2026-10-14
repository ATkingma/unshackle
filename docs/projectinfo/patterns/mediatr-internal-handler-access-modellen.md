# Models: mediatr.internal-handler-access

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/InternalHandlerAccessPattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `InternalHandlerAccessPattern` | `ISolutionPattern` + `ISolutionFinder`; finds and writes only with the solution |
| `ISolutionFinder` | `FindAsync(root, model, solution, ct)`; the `Fixer` calls it instead of `Find` |
| `AddMediatRPattern` | `Find` (the `AddMediatR` calls), `ScannedAssemblies`, `IsHiddenHandler`, `Types`, `CanGrantAccess` (shared) |
| `PatternResult.Fixed` | the whole `CompilationUnitSyntax` with the extra attribute lists |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `FindAsync` → `IsChosenFile` | Is this file the first (path, position) with a non-public handler of this assembly? |
| Hosts | `HostsAsync` | Per other C# project: documents with the text `AddMediatR`, `AddMediatRPattern.Find`, `ScannedAssemblies` contains this assembly, and `GivesAccessTo` not yet |
| Write | `RewriteAsync` | Per host `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Host")]` before the first member (takes over its leading trivia) or after existing attributes |
| Host side | `AddMediatRPattern.RewriteAsync` → `HiddenHandler(..., CanGrantAccess)` | The assembly is a project of the solution without a strong name: no Manual |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| No finding | strong-named, no host, the host already has access, or not the chosen file | `InternalHandlerAccessPatternTests` (`AlreadyGrants`, `NoOtherProjectScans`) |
| Fixed, one attribute | one host scans | `_Combined/add-mediatr-internal-handlers-other-project` |
| Fixed, several attributes | two hosts | `..._TwoHostsScanInternalHandlers_...` |
| Manual (`Rewrite` without a solution) | only outside the Fixer | no proof |

## See also

- [mediatr-internal-handler-access-plan.md](mediatr-internal-handler-access-plan.md)
- [mediatr-internal-handler-access-verify.md](mediatr-internal-handler-access-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/_Combined/`
