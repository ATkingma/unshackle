# TAAK-117 — MediatR: ongebruikte assembly-parameter na AddMediatR → AddMediator (IDE0060)

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/archief/TAAK-115-usings-stijl-en-overbodig.md` (evo1)

## Probleem

`mediatr.add-mediatr` laat `cfg.RegisterServicesFromAssembly(assembly)` weg. Ging de parameter `assembly` alleen
daarheen, dan meldt IDE0060 "unused parameter"; met `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` breekt de build.

## Afweging

- `_ = assembly;` toevoegen: het pattern herschrijft een expressie, geen statement; een extra statement in de
  methode is ingrijpender en lelijk. In de lambda zetten kan de Mediator-generator (leest de lambda uit source) storen.
- Parameter verwijderen: wijzigt de signatuur en alle aanroepers (andere bestanden, mogelijk publieke API).
- Manual: gooit een verder correcte migratie weg.
- **Gekozen**: `Fixed` met een waarschuwing in het rapport die de parameter noemt en adviseert hem (en het argument
  bij de aanroepers) te verwijderen. De code compileert gewoon; alleen projecten die IDE0060 als fout afdwingen
  moeten die ene parameter met de hand weghalen, en het rapport zegt welke.

## Stappen

- [x] Test eerst: `src/Unshackle.Tests/Core/AddMediatRPatternTests.cs` (warning bij alleen-doorgegeven parameter, geen bij ander gebruik)
- [x] `AddMediatRPattern.UnusedParameters`
- [x] Proof: `Proofs/MediatR/AddMediatR/edge-assemblies` (parameter blijft, after compileert)

## Zie ook

- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/archief/TAAK-115-usings-stijl-en-overbodig.md`
