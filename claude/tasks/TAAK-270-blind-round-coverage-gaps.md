# TAAK-270 — Coverage gaps from the blind round: one Manual blocks the whole migration

**Status**: open
**Created**: 2026-10-15 (blind round, `claude/tasks/CORPUSRUN-blind.md`)

## Context

In 5 of the 6 MediatR/AutoMapper repos of the blind round the tool changed nothing: a few Manual findings block every other finding through the all-or-nothing rule (correct, a partial migration breaks at runtime). The blockers:

| Repo | Blocking findings | Verdict |
| --- | --- | --- |
| miru | `AddMediatR` expression lambda with several settings (3×); one explicit `IRequestHandler<,>.Handle` | Could be migrated: rewrite the lambda to a block body; explicit handler → implicit |
| fullcart | `MediatR.Mediator` used directly; generic decorator used outside Autofac `RegisterGenericDecorator` | Justified Manual |
| culinary | Profile with a cycle (`Account` ↔ `Preference`), null sources of `Map` | Justified Manual (safety) |
| msb | Cycle (`OrderModel` ↔ `BoxModel`), `ReverseMap` with unflattening, a `Map` without `CreateMap` | Justified Manual |
| fiapgames | One profile not rewritten (`CreateMap<CriarPromocaoCommand, PromocaoModel>`), so `IMapper` stays | To check: reason text is cut; see `_results/after/fiapgames/report.json` |

## Steps

1. [ ] miru: `mediatr.add-mediatr` for an expression lambda with several settings (`cfg => cfg.A().B()`), with proof.
2. [ ] miru: explicit `IRequestHandler<,>.Handle` → implicit `ValueTask<T> Handle`, with proof (or a sharper Manual reason).
3. [ ] fiapgames: find out why `PromocaoProfile` is not rewritten; task per cause.
4. [ ] Report: show per run "N findings blocked by M Manual", so a user sees the few spots to fix by hand.

## See also

- `claude/tasks/CORPUSRUN-blind.md`
- `claude/tasks/TAAK-268-blind-validation-round.md`
