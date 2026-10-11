# TAAK-147 — MediatR: restpunten TAAK-144 (generator op naam, global using, InternalsVisibleTo)

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/archief/TAAK-144-mediatr-evo2-naamconflict-generator.md` (Niet gedaan, `shortcut:`-commentaar)

## Opdracht en keuze per punt

1. **Generator op naam.** `OnlyWithCallTo = ["AddMediator", "AddMediatR"]` matchte syntactisch op de gefixte code, dus ook een eigen `AddMediator` (evo2's `MediatorModule`). Nu: `ProjectPackages.ForProjectAsync` kijkt naar de **originele** code en telt alleen aanroepen die semantisch binden aan de verwijderde package (MediatR's `AddMediatR`). `OnlyWithCallTo = ["AddMediatR"]`; `PlanAsync` krijgt de originele solution mee. Test `SwappedTypesTests.FixAsync_ProjectCallingOwnAddMediator_AddsNoSourceGenerator`.
2. **Global using.** Een geswapte `global using MediatR;` → `global using Mediator;` bereikt elk bestand van het project, maar `UsingDirectives.Clashing` liep alleen voor lokaal toegevoegde usings. Nu: `Fixer.SwapGlobalUsingsAsync` roept `UsingDirectives.QualifyClashingAsync` aan op elk document van het project (zelfde volledige-naam-kwalificatie). Proof `RequestHandler/edge-own-command-name-global`, test `SwappedTypesTests.FixAsync_GlobalUsingSwappedWithOwnTypeOfSameName_QualifiesOwnTypeInEveryFile`.
3. **InternalsVisibleTo.** Een `internal` registratiemethode met een gescande assembly-parameter in een assembly met `InternalsVisibleTo` (behalve `DynamicProxyGenAssembly2`, de mocking-proxy) → Manual. Een pattern ziet de solution niet, dus elke friend telt (shortcut in code). Proof `AddMediatR/edge-assemblies-friend`, test `AddMediatRPatternTests.Rewrite_AssemblyParameterOfInternalMethodWithFriend_ManualUnlessMockingProxy`.

## Stappen

- [x] Code (Core: `ProjectPackages`, `Fixer`, `UsingDirectives`, `AddMediatRPattern`, `MediatRFix`)
- [x] Proofs + unittests (zie boven)
- [x] Pattern-docs `mediatr-add-mediatr-plan.md` / `-verify.md`
- [x] `dotnet build`, `dotnet test` (724/724), samples 38/38
- [x] Harnas `claude/tools/corpus-regression.sh` op verse bron `/mnt/dikkeschijf/unshackle-corpus/src/evo`, uitvoer `/mnt/dikkeschijf/unshackle-corpus/t147/out/`:
      evo1 `ok` 45/45 → 45/45, 0 regressies. **Nu Manual** (punt 3): `Fitnet.IntegrationTests` roept de internal `AddInMemoryEventBus(Assembly.GetExecutingAssembly())` echt aan via `InternalsVisibleTo`; solution ongewijzigd.
      evo2 `ok` 39/39 → 39/39, 0 regressies, 2 Manual zoals in TAAK-144, solution ongewijzigd.

## Niet gedaan

- Punt 1 en 2 worden door evo1/evo2 niet geraakt (beide solutions blijven door `AllOrNothing` onaangeroerd); alleen door unittests/proofs gedekt.
- Punt 3 controleert niet of de friend de methode echt aanroept (vals-positief geeft Manual, nooit kapotte code).

## Zie ook

- `claude/tasks/archief/TAAK-144-mediatr-evo2-naamconflict-generator.md`
- `claude/tasks/archief/TAAK-141-mediatr-assembly-parameter-discard.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/REGRESSIE-corpus.md`
