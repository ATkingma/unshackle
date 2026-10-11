# TAAK-115 — Toegevoegde usings buiten de namespace of overbodig

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11 (corpus-run MediatR evo1, `claude/tasks/CORPUSRUN-mediatr.md`)

## Context

evo1 (evolutionary-architecture hfst. 1) zet usings binnen een file-scoped namespace en bouwt met TreatWarningsAsErrors. Na de
MediatR-fix: IDE0065 (`using Mediator;` bovenaan, buiten de namespace) en IDE0005 (overbodig) in o.a.
`PassExpiredEventHandler.cs` (handler via eigen `IIntegrationEventHandler<T>`, noemt MediatR niet), `RegisterEndpoint.cs` en
`InMemoryEventBusModule.cs` (`AddMediator` staat in `Microsoft.Extensions.DependencyInjection`).

## Stappen

- [x] `Fixer`: `AddedNamespaces` alleen voor een bestand dat de oude bibliotheek bij naam noemt (originele semantiek,
  `UsingDirectives.NamesRemovedNamespaceAsync`; inactieve `#if`-code telt mee). Een bestand dat alleen via eigen basistypen
  MediatR gebruikt of een ongebruikte oude using had, krijgt geen nieuwe.
- [x] `UsingDirectives.UpdateAsync`: geen top-level usings maar wel usings in de namespace → nieuwe usings daar, met dezelfde
  inspringing. Niet als een naam binnen de namespace de nieuwe overschaduwt (eigen `*.Mediator`, zie TAAK-131): dan bovenaan.
- [x] Tests: `FixerTests.FixAsync_FileNamesNothingOfRemovedNamespace_AddsNoNamespace`, `…_UsingsInsideNamespace_AddsNewOneThere`,
  `…_UsingsInsideNamespaceThatShadowsAddedOne_AddsNewOneOnTop`. AddMediatR-proofs zonder overbodige `using Mediator;`.
- [x] Corpus: verse kopie evo1 (`scratchpad/v110/work/evo1`): geen IDE0005/IDE0065 meer. Build nog rood op IDE0060
  (`assembly` ongebruikt na `AddMediatR(... RegisterServicesFromAssembly(assembly))` → `AddMediator(...)`) en 2x MSG0005 (TAAK-136).

## Open

- IDE0060 na `mediatr.add-mediatr`: parameter die alleen naar `RegisterServicesFromAssembly` ging wordt ongebruikt. Nieuwe taak
  waard (Manual-melding of `_ = assembly;`?), niet in deze taak.

## Zie ook

- `src/Unshackle.Core/UsingDirectives.cs`, `src/Unshackle.Core/Fixer.cs`
- `claude/tasks/TAAK-131-mediatr-namespace-schaduw.md`
