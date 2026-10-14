# TAAK-027 — mediatr.pipeline-behavior

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `IPipelineBehavior<TReq,TRes>` (MediatR 12.5.0) → Mediator 3.0.2 `IPipelineBehavior<TMessage,TResponse>`.

## Context for the next agent

- Row 4 in `claude/tasks/mediatr-queue.md`. Reuses `MediatRSyntax` (TAAK-025).
- Examples: `samples/SampleShop/SampleShop/Behaviors/LoggingBehavior.cs` (open, async), `ValidationBehavior.cs` (closed, `return next()`).

## Analysis

Signatures checked with reflection on the real assemblies (scratch project with Mediator 3.0.2):

- Mediator: `ValueTask<TResponse> Handle(TMessage, MessageHandlerDelegate<TMessage,TResponse>, CancellationToken)`, `TMessage : IMessage`; `MessageHandlerDelegate.Invoke(TMessage, CancellationToken)`.
- MediatR 12.5: `Task<TResponse> Handle(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)`; `next()` and `next(token)` both compile (proof `next-token`).

| Case                                                    | Rewrite                                                            |
| ------------------------------------------------------- | ------------------------------------------------------------------ |
| open behavior (request = type parameter)                | + `IMessage` in the constraint (before `new()`), or a new `where`  |
| closed behavior                                         | no constraint: the request type becomes an `IMessage` via the swap |
| `next()` / `next(token)`                                | `next(request, token)` with the parameter names of `Handle`        |
| non-async, returns `next(...)`/`Task.FromResult`        | `ValueTask.FromResult`; `next(...)` already returns a `ValueTask`  |
| `next` stored/passed on, other return, explicit         | Manual                                                             |

## Choices

- **The pattern node is the type, not the method**: the `IMessage` constraint is on the class. `FindHandleMethods` now uses `DescendantNodesAndSelf`, so it also works with the type as root (same result for a compilation unit).
- **`IPipelineBehavior\`2`in`SwappedTypes`**: otherwise the base list holds on to `using MediatR`. The pattern covers the signature difference; on Manual `RequestHandlerDelegate`keeps the MediatR using and the package (same behaviour as the handler patterns). Test`SwappedTypes_PipelineBehavior_IsNotSwapped...` removed.
- **The proof runner runs the Mediator source generator** on every MediatR `after` (`ProofFix.AfterGenerators`, `TestReferences.MediatorGenerators`, `TestSolution.CompileErrors(..., generators)`). The `Mediator.SourceGenerator` package is in the test project with `IncludeAssets="none"` (no analyzer for the test project itself); the DLLs are copied to `Generators/` in the output and loaded via `AnalyzerFileReference` + `AnalyzerLoader`. Needed for row 5 (`AddMediator` only exists thanks to the generator), and it makes all MediatR proofs stricter. `Microsoft.Extensions.DependencyInjection.Abstractions` is now in the MediatR and Mediator references.

## Steps

- [x] 1. Claim row 4.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-pipeline-behavior-plan.md` (approved).
- [x] 3. `PipelineBehaviorPattern`, registration + swap in `MediatRFix`.
- [x] 4. Proofs `open-async`, `no-constraint`, `new-constraint`, `closed`, `next-token`; Manual `next-stored`, `next-passed`, `explicit`.
- [x] 5. Models, verify, README row, queue row.

## Result

- `dotnet build` 0 warnings, `dotnet test` 400/400, sample tests 38/38.
- Scan SampleShop: `mediatr.pipeline-behavior` 2 Fixed, 0 Manual.

## See also

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-028-mediatr-add-mediatr.md`
- `docs/projectinfo/patterns/mediatr-pipeline-behavior-plan.md`
