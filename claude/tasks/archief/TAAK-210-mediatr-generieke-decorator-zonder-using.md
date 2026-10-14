# TAAK-210 — A generic decorator in a file without `using MediatR` still falls into `mediatr.unsupported`

**Status**: ✅ completed (2026-10-13) — 0 Manual on kgrzybek; the build after the fix still breaks on new causes → TAAK-212
**Created**: 2026-10-13 (from corpus round 3, `claude/tasks/CORPUSRUN-mediatr.md`)
**Goal**: kgrzybek `91c8ef2` migrates fully: 0 Manual, build ok, tests 246/287 → 246/287.

## Context for the next agent

- Corpus round 3 (main `9189df4`): kgrzybek 0 Fixed, 270 Manual. Only root Manuals: 15× `mediatr.unsupported`
  ("Mediator's source generator does not support generic messages…") on the module decorators
  `Modules/<Module>/Infrastructure/Configuration/Processing/{Logging,UnitOfWork,Validation}CommandHandlerDecorator<T>` (5 modules × 3).
- On the same classes `mediatr.generic-decorator` (TAAK-193) **does** find them as decorators. `mediatr.void-request-handler` also finds them.
- The BuildingBlocks decorator `UnitOfWorkCommandHandlerDecorator<T> : IRequestHandler<T>` (file **with** `using MediatR`) gets **no** unsupported.
- The module decorators only implement the own marker `ICommandHandler<T> : IRequestHandler<T>` (another project, `Application`)
  and their file has **no `using MediatR;`**.

## Reproduction (measured)

Two projects: `App` (MediatR 12.2.0; `ICommand : IRequest`, `ICommandHandler<in T> : IRequestHandler<T>`, handler) and `Infra`
(Autofac 7.1.0; `LogDecorator<T> : ICommandHandler<T>` with ctor `ICommandHandler<T>`, `RegisterGenericDecorator(typeof(LogDecorator<>), typeof(ICommandHandler<>))`).

| `Infra/D.cs`                         | Result `fix --dry-run`                                      |
| ------------------------------------ | ----------------------------------------------------------- |
| with `using MediatR;`                | everything Fixed (void-request-handler, generic-decorator)  |
| without (`typeof(MediatR.IRequestHandler<>)` fully qualified) | `mediatr.unsupported` Manual on `LogDecorator<T>` → everything Manual |

A kgrzybek excerpt (only Payments Application + Infrastructure in its own `.slnx`) reproduces it too; there, only
adding `using MediatR;` to one decorator did **not** help — so something else may play a part (first solve the small repro, then measure again).

## Presumed cause

`UnsupportedPattern` runs after `VoidRequestHandlerPattern`/`GenericDecoratorPattern` on the already rewritten file.
`GenericDecoratorPattern.Decorated` has a fallback for "already rewritten in this file" that expects the ctor parameter as
MediatR's `IRequestHandler<T, Unit>`; without `using MediatR` the rewritten text (or the marker route) does not bind,
`Decorated` returns `null` and `UnsupportedPattern` sees a generic request handler → Manual.

## Steps

- [x] 1. Repro: `Proofs/MediatR/_Combined/generic-decorator-marker-without-using` (projects `App`, `Handlers`, `Infra`; without the fix → `mediatr.unsupported`, everything Manual) and unit tests in `GenericDecoratorPatternTests`.
- [x] 2. Cause confirmed (as presumed): `GenericDecoratorPattern.RewriteVoid` writes `IRequestHandler<T, Unit>`; without `using MediatR` that only binds after `using Mediator` (end of the run), so for `UnsupportedPattern` it is an error type, `Decorated` returns `null` → Manual.
- [x] 3. Fix: `GenericDecoratorPattern.IsRewrittenVoid` also recognises the error type `IRequestHandler<T, Unit>` (name, arity 2, first argument the type parameter, second `Unit`).
- [x] 4. Proof as `_Combined` (the bug sits between two patterns; a separate proof runs only one pattern). Docs: generic-decorator plan/verify REQ-7, void-request-handler plan/verify REQ-6/7, `architectuur/core.md`.
- [~] 5. Proof (`_results/r4/kgr`): 217 Fixed, **0 Manual** (was 270 Manual). The build after the fix still fails → TAAK-212.

## Extra causes (found during the proof)

The excerpt observation ("adding `using MediatR` did not help") came from all-or-nothing: all 15 decorators must succeed at the same time. After the fix
three new causes surfaced in the corpus run, all three solved:

1. **`Directory.Packages.props` without central package management** (kgrzybek: `<PackageReference Include="MediatR" Version="12.2.0" />` in an
   `ItemGroup Condition="...EndsWith('BuildingBlocks.Domain')"`, no `<PackageVersion>`): `XmlProjectFileStore` treated it as CPM →
   versionless `<PackageReference>` in the projects + `<PackageVersion>` → `NU1015` on restore. Now: CPM only with a `<PackageVersion>` or
   `ManagePackageVersionsCentrally=true`; otherwise the file belongs to the chain (reference swapped in place) and an extra package (generator)
   goes into the project itself instead of next to a conditioned reference. Unit test `EditAsync_PackagesPropsWithoutCentralManagement_…`.
2. **`Unit`/`ValueTask<Unit>` without `using Mediator`**: `VoidRequestHandlerPattern` required no namespace; handlers in a file without `using MediatR`
   (only the own marker) got `ValueTask<Unit>` without `using Mediator` → 92× CS0246/CS0738. Now `Fixed(..., "Mediator", "System.Threading.Tasks")`.
3. **SA1513** (StyleCop, TWAE): `return Unit.Value;` directly after a `}`. Now a blank line before it (proof `VoidRequestHandler/block-end`).

Remainder (TAAK-212): `MSG0001` "multiple handlers" (44 handler types), `SA1210` using order (Fixer/`UsingDirectives`), `CS1503` in
`Validation…WithResultDecorator` (`ValueTask<TResult>` to a `Task<TResult>` parameter), `CS0122` internal domain events in `Mediator.g.cs`.

## See also

- `claude/tasks/archief/TAAK-193-mediatr-generieke-decorators-messages.md` — `mediatr.generic-decorator`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Repeat round 3"
- `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`, `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericDecoratorPattern.cs`
- `claude/tasks/mediatr-queue.md`
