# TAAK-136 — MediatR: Mediator-generator MSG0005 breekt build met TreatWarningsAsErrors

**Status**: klaar (2026-10-11)
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

- [x] Gedrag MSG0005 voor notification vs request vastleggen
- [x] NoWarn of Manual
- [x] `dotnet build` + `dotnet test` groen

## Resultaat

MSG0005 komt ook bij een request zonder handler (probe Mediator 3.0.2), maar MediatR faalt daar ook pas bij `Send`:
gedrag gelijk, dus geen Manual. `AddedPackage.WarningsNotAsErrors`: bij het toevoegen van `Mediator.SourceGenerator`
schrijft `XmlProjectFileStore` `<WarningsNotAsErrors>$(WarningsNotAsErrors);MSG0005</WarningsNotAsErrors>` in hetzelfde
bestand (laatste onvoorwaardelijke `PropertyGroup`, anders een nieuwe). Gekozen boven `NoWarn`: de waarschuwing blijft
zichtbaar. evo1: MSG0005 nu warning.

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
