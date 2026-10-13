# TAAK-240 — MediatR: waarschuwing in het rapport bij architectuurtests (namespace `Mediator` uitzonderen)

**Status**: ✅ voltooid
**Afgerond**: 2026-10-13
**Aangemaakt**: 2026-10-13 (vervolg op TAAK-214)
**Doel**: wie MediatR migreert in een solution met NetArchTest/ArchUnitNET krijgt in het rapport te horen dat archtests de namespace `Mediator` moeten uitzonderen.

## Context voor de volgende agent

- TAAK-214: kgrzybek's `*Module_DoesNotHave_Dependency_On_Other_Modules` falen na de fix op de gegenereerde `Mediator.Mediator` en
  `Mediator.Internals.ContainerMetadata` (die noemen messages van andere modules). Legitiem verschil; oplossing in de test:
  `.And().DoNotResideInNamespace("Mediator")`.
- TAAK-214 vond een automatische melding "niet betrouwbaar af te leiden". Gekozen compromis: geen analyse van de tests, alleen een
  waarschuwing zodra een project van de solution een architectuurtest-bibliotheek refereert.

## Stappen

- [x] 1. `MediatorHosts.WithArchitectureTestWarning(result, solution)`: voegt aan een `FixedResult` een `Warning` toe (achter een bestaande)
  als een project een `PortableExecutableReference` heeft waarvan de bestandsnaam met `NetArchTest` (NetArchTest.Rules,
  NetArchTest.eNhancedEdition) of `ArchUnitNET` (TngTech.ArchUnitNET + xUnit/NUnit/MSTest) begint. Geen I/O: de referenties komen uit de Roslyn-`Solution`.
- [x] 2. Aangeroepen in `AddMediatRPattern.RewriteAsync` en `AutofacMediatorModulePattern.RewriteAsync` (alleen met solution; Manual blijft Manual).
- [x] 3. Unittest `AddMediatRPatternTests.RewriteAsync_SolutionWithArchitectureTests_WarnsAboutMediatorNamespace` (NetArchTest.Rules, ArchUnitNET.xUnit, andere dll).
- [x] 4. KingmaDoc plan/verify van `mediatr.add-mediatr` en `mediatr.autofac-mediator-module`, `mediatr-queue.md`, `INDEX.md`.
- [x] 5. Scan (alleen rapport) op een kopie van kgrzybek `91c8ef2`: de 4 `mediatr.autofac-mediator-module`-rijen zijn Fixed met de waarschuwing.

## Resultaat

- Tests: `*Mediat*` + `*ProofTests` 820/820 groen; nieuwe test 3/3.
- kgrzybek-scan: waarschuwing op `Modules/{Meetings,Administration,UserAccess,Payments}/Infrastructure/Configuration/Mediation/MediatorModule.cs`
  (het eerste project met de bibliotheek wordt genoemd, hier `...UserAccess.Domain.UnitTests`).
- Afweging: de waarschuwing komt ook als de archtests geen module-afhankelijkheid testen (vals positief, maar onschuldig: het advies klopt altijd).

## Zie ook

- `claude/tasks/archief/TAAK-214-mediatr-archtests-gegenereerde-mediator.md`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Herhaling ronde 6"
- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`, `docs/projectinfo/patterns/mediatr-autofac-mediator-module-plan.md`
