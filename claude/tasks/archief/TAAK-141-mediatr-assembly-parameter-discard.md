# TAAK-141 — MediatR: IDE0060 op assembly-parameter echt oplossen

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/REGRESSIE-corpus.md` (evo1, evo2: BUILD-NA-FIX-FAALT op IDE0060)

## Probleem

TAAK-117 laat de parameter die alleen naar `RegisterServicesFromAssembly(...)` ging staan en geeft een waarschuwing.
Projecten met `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` (evo1, evo2) bouwen daarna niet.

## Afweging

- Parameter verwijderen: wijzigt signatuur en aanroepers (andere bestanden, publieke API zoals evo2's `MediatorModule`).
- Hernoemen naar `_`: breekt aanroepers met een benoemd argument.
- **Gekozen**: `_ = parameter;` vóór de aanroep. Signatuur en aanroepers blijven gelijk, IDE0060 telt het als gebruik,
  en de lambda (die de Mediator-generator uit source leest) blijft schoon. Daarvoor vindt `Find` een aanroep die een
  los statement is als dat statement, zodat `Rewrite` twee statements kan teruggeven (bestaand mechanisme in de Fixer).
  Staat de aanroep in een expressie (`return services.AddMediatR(...)`), dan blijft de TAAK-117-waarschuwing.

## Stappen

- [x] Tests: `src/Unshackle.Tests/Core/AddMediatRPatternTests.cs` (statement → `_ = handlers;`, expressie → waarschuwing)
- [x] `AddMediatRPattern.Find`/`Rewrite`
- [x] Proof `Proofs/MediatR/AddMediatR/edge-assemblies.after.cs`
- [x] Bewijs: kopie van de evo1/evo2-uitvoer met `_ = assembly;` in `/mnt/dikkeschijf/unshackle-corpus/t141/`.
      evo1 bouwt, 45/45 tests groen. evo2: IDE0060 weg, maar daaronder zitten CS0104 (eigen `ICommand` vs `Mediator.ICommand`),
      CS0122/CS0436 (Mediator-generator in meerdere projecten) en CS0535; los probleem, nog geen taak.
      (Het harnas kon niet: er staat geen originele evo-bron in de corpusmap.)

## Zie ook

- `claude/tasks/archief/TAAK-117-mediatr-ongebruikte-assembly-parameter.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/REGRESSIE-corpus.md`
