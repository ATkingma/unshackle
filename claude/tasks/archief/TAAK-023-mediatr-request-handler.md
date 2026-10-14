# TAAK-023 — mediatr.request-handler

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `IRequestHandler<TReq,TRes>` with `Task<TRes> Handle(...)` (MediatR 12.5.0) → `ValueTask<TRes> Handle(...)` (Mediator 3.0.2).

## Context for the next agent

- Row 1 in `claude/tasks/mediatr-queue.md`. Infrastructure: `claude/tasks/archief/TAAK-020-mediatr-fix-infrastructuur.md`.
- Examples: `samples/SampleShop/SampleShop/Features/Orders/{ListOrders,PlaceOrder,GetOrder}Handler.cs`.
- **No** pattern class or proof has been built yet; research only. Reason: see **Blocker**.

## Analysis

### API (checked in the real assembly `~/.nuget/packages/mediator.abstractions/3.0.2/lib/net8.0/Mediator.dll`)

- Mediator 3.0.2 contains in namespace `Mediator`: `IRequestHandler\`1`, `IRequestHandler\`2`, `IRequest`, `IRequest\`1`, `IBaseRequest`, `Unit`; `Handle`returns`ValueTask<TResponse>`.
- MediatR 12.5.0: `IRequestHandler<,>` in `MediatR.dll`, `IRequest<T>`/`Unit`/`INotification` in `MediatR.Contracts.dll`, both namespace `MediatR` (`TestReferences.MediatR` loads both).

### Intended rewrite

| Case                                                                         | Rewrite                                                     |
| ---------------------------------------------------------------------------- | ----------------------------------------------------------- |
| `async Task<T> Handle(...)`                                                  | return type only → `ValueTask<T>`; `async` and body stay    |
| non-async, every `return Task.FromResult(x)`                                 | `ValueTask<T>` + `return ValueTask.FromResult(x)` (.NET 5+) |
| expression-bodied `=> Task.FromResult(x)`                                    | same                                                        |
| non-async with another `return` (e.g. `return _inner.Handle(...)`, `Task.Run`) | Manual: cannot guarantee Task → ValueTask                 |
| explicit interface implementation / `Handle` not found                      | Manual                                                      |

Find: methods `Handle` in classes that implement `MediatR.IRequestHandler<,>` (semantically via `SemanticModel`, not by name).

## Blocker (resolved by TAAK-024)

`UsingDirectives.UpdateAsync` (`src/Unshackle.Core/UsingDirectives.cs`) swaps `using MediatR;` → `using Mediator;` only if
**nothing** in the document still binds to `MediatR` (`UsedNamespaces`, determined with the old compilation). After this rewrite
`IRequestHandler<A,B>` (and `IRequest<T>` in the same file) still binds to `MediatR`, so the using stays
and `after` never compiles against Mediator. The same applies at project level (`Fixer.cs:104`: "MediatR is still used in
this project" → package stays). The assumption from `mediatr-queue.md` ("the namespace swap covers same-named interfaces") therefore
does not hold with the current code. This is the TAAK-020 risk "`using MediatR` only disappears when nothing binds anymore".

A pattern cannot solve this without a contract or Fixer change (forbidden for pattern agents, `patroon-loop.md` step 5).

### Infrastructure proposal (separate task, row-0-like)

`IFix` gets a list of same-named types covered by the swap, e.g. `IReadOnlyList<string> SwappedTypes`
(`IRequest`, `IRequest\`1`, `IRequestHandler\`1`, `IRequestHandler\`2`, `INotification`, `INotificationHandler\`1`,
`ISender`, `IPublisher`, `IMediator`, `Unit`; FA fix: empty). `UsingDirectives.UsedNamespaces`and the package check in`Fixer`do not count symbols with that metadata name as "usage". After that this pattern is small: only rewrite`Handle`.

### MediatR.Contracts / removing the package

- Types from `MediatR.Contracts` have namespace `MediatR`; the swap treats them as `MediatR` usage (same blocker).
- `PackageMigration.RemovedPackage = "MediatR"` only removes that package. A project that references `MediatR.Contracts` separately
  (e.g. a contracts project) keeps that reference: then `IRequest` exists twice (MediatR and Mediator) → ambiguous after the swap.
  To be noted as Manual or as a second package to remove in the infrastructure task.

## Choices

- Do not build without a working swap: a proof whose `after` does not compile fails in `ProofTests`; a pattern that
  only does `Task`→`ValueTask` and leaves `using MediatR` gives users non-compiling code.
- `ValueTask.FromResult(x)` over `new(x)`: readable and equal to `Task.FromResult`, exists since .NET 5.

## Steps

- [x] 1. Claim row 1.
- [x] 2. Research API in real assemblies, examples in samples.
- [x] 3. Blocker resolved by TAAK-024 (`MediatRFix.SwappedTypes`).
- [x] 4. Pattern `RequestHandlerPattern`, proofs (`async`, `from-result-expression`, `from-result-block`, `generic`, `token-name`; Manual: `other-return`, `explicit`), plan/models/verify, README row.

## Result

- `ProofTests` now passes `SwappedTypes` to the fix substitute (otherwise `using MediatR` stays); empty for `*.manual.txt`, so that a manual proof only checks that the pattern changes nothing (the Fixer would swap the using anyway otherwise).
- Explicit interface implementation → Manual (choice: rare, and the interface name in the declaration must change too).
- Scan `dotnet run --project src/Unshackle.Interface -- scan samples/SampleShop/SampleShop.slnx --fix mediatr` (after `dotnet restore` of the sample; without restore: 0 findings): `mediatr.request-handler` 5 Fixed / 0 Manual (`CountQuery`, `RegisterCustomerCommand`, `GetOrderHandler`, `ListOrdersHandler`, `PlaceOrderHandler`); `project-file` 4 Fixed, 1 Manual (`SampleShop.csproj`: MediatR still used by notification handler/`AddMediatR`, rows 3 and 5).
- Checks: `dotnet build` 0 warnings, `dotnet test` 318/318, sample tests 38/38, hook green.

## See also

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-020-mediatr-fix-infrastructuur.md`
- `src/Unshackle.Core/UsingDirectives.cs`
- `claude/docs/patroon-loop.md`
