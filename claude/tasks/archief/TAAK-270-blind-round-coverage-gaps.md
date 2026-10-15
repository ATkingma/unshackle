# TAAK-270 — Coverage gaps from the blind round: one Manual blocks the whole migration

**Status**: done (step 4: console summary lists the Manual findings that hold back the rest, `SpectreConsoleViewTests`)
**Created**: 2026-10-15 (blind round, `claude/tasks/CORPUSRUN-blind.md`)

## Context

In 5 of the 6 MediatR/AutoMapper repos of the blind round the tool changed nothing: a few Manual findings block every other finding through the all-or-nothing rule (correct, a partial migration breaks at runtime). The blockers:

| Repo      | Blocking findings                                                                                   | Verdict                                                                            |
| --------- | --------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| miru      | `AddMediatR` expression lambda with several settings (3×); one explicit `IRequestHandler<,>.Handle` | Could be migrated: rewrite the lambda to a block body; explicit handler → implicit |
| fullcart  | `MediatR.Mediator` used directly; generic decorator used outside Autofac `RegisterGenericDecorator` | Justified Manual                                                                   |
| culinary  | Profile with a cycle (`Account` ↔ `Preference`), null sources of `Map`                              | Justified Manual (safety)                                                          |
| msb       | Cycle (`OrderModel` ↔ `BoxModel`), `ReverseMap` with unflattening, a `Map` without `CreateMap`      | Justified Manual                                                                   |
| fiapgames | One profile not rewritten (`CreateMap<CriarPromocaoCommand, PromocaoModel>`), so `IMapper` stays    | To check: reason text is cut; see `_results/after/fiapgames/report.json`           |

## Steps

1. [x] miru: `mediatr.add-mediatr`: an expression lambda that ends up with several settings is now rewritten to a block body (proof `AddMediatR/expression-several`). Miru's real shape is a single `cfg => cfg.RegisterServicesFromAssemblyContaining<T>()`; the dropped scan plus lifetime/host settings give several assignments (no chain).
2. [x] miru: explicit `IRequestHandler<,>.Handle` is now rewritten to `ValueTask<T>`, keeping the explicit specifier (same Handle shape in Mediator; proofs `RequestHandler/explicit`, `explicit-async`). The other patterns still call `RejectExplicit` and stay Manual.
3. [x] fiapgames: `PromocaoProfile` was blocked by `ForCtorParam("x", o => o.MapFrom(s => s.X))` (automapper.unsupported). Now migrated as `new(source.X, ...)` (REQ-12 of automapper-constructor: null-safe like a MapFrom, measured on AutoMapper 14.0.0; Manual for an unknown parameter, ReverseMap, ConstructUsing, or when several constructors make the choice undecidable). Proofs `CreateMap/for-ctor-param{,-several,-nested-null,-unknown,-reverse-map}`. Rerun on a copy: PromocaoProfile no longer blocks, but the fix still changes nothing because of a second, independent blocker: `UsuarioProfile` maps into `UsuarioModel.Nome { get; private set; }`, which AutoMapper writes by reflection and a static `ToB` cannot (justified Manual, reason already specific; sound only if the model gets a public setter or constructor).
4. [x] Report: show per run "N findings blocked by M Manual", so a user sees the few spots to fix by hand.

## See also

- `claude/tasks/CORPUSRUN-blind.md`
- `claude/tasks/archief/TAAK-268-blind-validation-round.md`
