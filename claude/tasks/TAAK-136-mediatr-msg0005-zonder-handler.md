# TAAK-136 — MediatR: Mediator-generator MSG0005 breekt build met TreatWarningsAsErrors

**Status**: open
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/CORPUSRUN-mediatr.md` (evolutionary-architecture hfst. 1, gesimuleerd na TAAK-134)

## Probleem

MediatR staat een notification zonder handler toe (publish doet dan niets). De Mediator 3-generator meldt
`MSG0005 found message without any registered handler` (`OfferPrepareEvent`, `PassRegisteredEvent`: alleen in
integratietests afgehandeld). Met `TreatWarningsAsErrors` (evo1, evo2, amantin, nadirbad, jason hebben dat)
breekt de build na een verder geslaagde migratie.

## Wat, waar, hoe

- **Waar**: project-edit van `MediatRFix` (waar `Mediator.SourceGenerator` wordt toegevoegd).
- **Hoe**: `MSG0005` aan `<NoWarn>` (of `WarningsNotAsErrors`) toevoegen in het host-project, met reden in het
  rapport; eerst nagaan of MSG0005 ook bij requests zonder handler komt (dan is het een echte runtimefout: Manual).

## Stappen

- [ ] Gedrag MSG0005 voor notification vs request vastleggen
- [ ] NoWarn of Manual
- [ ] `dotnet build` + `dotnet test` groen

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
