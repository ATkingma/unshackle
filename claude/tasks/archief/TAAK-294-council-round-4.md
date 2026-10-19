# TAAK-294 — Council round 4: qualified builder-type match, Scrutor symbol check

**Status**: ✅ done
**Created**: 2026-10-19
**Goal**: implement the round-4 council decisions Q3 and Q4 (`claude/docs/council.md`) before 0.2.0.

## Context for the next agent

- Builds on TAAK-293 (`claude/tasks/archief/TAAK-293-council-round-3.md`): the registration/builder type list in `MapperRegistration.cs` and the Scrutor reason in the MediatR generic-decorator pattern.
- Q3: match builder types on the fully qualified metadata name (namespace included); add `HostApplicationBuilder`, `FunctionsApplicationBuilder`, `IFunctionsWorkerApplicationBuilder`. A lookalike type in user code must not count, so the lookup is kept.
- Q4: `Decorate` must resolve to a method in the `Scrutor` namespace; the syntax match is used only when the symbol is unresolved.
- Q1 (local function deferred) is already in place: add a test that pins it if none exists.

## Steps

- [x] 1. KingmaDoc plan updates (unused-imapper, generic-decorator)
- [x] 2. Implementation + unit tests (lookalike type, user-defined `Decorate`, unresolved fallback) + proofs
- [x] 3. Corpus `after` on the six AutoMapper repos + `scan --fix mediatr` on fullcart: 0 regressions, numbers in CORPUSRUN-blind.md
- [x] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## Outcome

- Q3: MapperRegistration.s_registrationTargets holds nine qualified metadata names; test `FixAsync_AddAutoMapperInALambdaPassedToALookalikeBuilder_KeepsTheLookup`. Q1 was already pinned by the theory row `local function`.
- Q4: `GenericDecoratorPattern.IsScrutor`; accepts the `Scrutor` namespace **or the `Scrutor` assembly**: real Scrutor declares `Decorate` in `Microsoft.Extensions.DependencyInjection` (council question). Proof `scrutor-decorate` moved its stand-in to `namespace Scrutor`.
- Corpus: six AutoMapper repos identical to TAAK-293, 0 regressions; fullcart scan unchanged (27/20).

## See also

- `claude/docs/council.md`, `claude/tasks/TAAK-295-kept-reason-codes.md`
