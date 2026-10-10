# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**Weet je niet waar iets staat?** → zie `claude/docs/INDEX.md`

## Documentatie-architectuur

| Locatie                          | Inhoud                                                                              |
| -------------------------------- | ----------------------------------------------------------------------------------- |
| `CLAUDE.md` (dit bestand)        | Commando's, architectuur in het kort, pakketregels                                  |
| `docs/projectinfo/README.md`     | Hoofddocument: C4-diagrammen, links naar architectuur, project en elk patroon (KingmaDoc) |
| `docs/projectinfo/project.md`    | Wat Unshackle is, waarom, wat er al bestaat, roadmap |
| `docs/projectinfo/architectuur/` | Projecten, Core (Fixer → Fix → Pattern), testen, rapportage |
| `claude/docs/INDEX.md`           | Volledige index: zoek hier als je niet weet waar iets staat                         |
| `claude/docs/code-standaarden.md` | Codestijl en design patterns |
| `claude/docs/agent-werkwijze.md` | Regels voor taakbeheer, cross-referenties, documentsplitsing                        |
| `claude/tasks/`                  | Actieve taken `TAAK-NNN-<slug>.md`; voltooid in `archief/`                          |
| `.claude/skills/`                | `/taak-start`, `/taak-specifiek`, `/taak-oppakken`                                  |

## Werkafspraken

- Communiceer in het Nederlands; code, namen en commitberichten in het Engels.
- **Nooit pushen of mergen naar `main` als er een test faalt (oud of nieuw).** Zie `claude/docs/agent-werkwijze.md` §6.
- Elk patroon krijgt KingmaDoc plan + verify in `docs/projectinfo/patterns/` en een rij in `docs/projectinfo/README.md` (hook `.claude/hooks/check-pattern-docs.sh` bewaakt dit).
- Documentatie en taken volgens `claude/docs/agent-werkwijze.md` (nieuw document → `INDEX.md`, `## Zie ook` onderaan).
- Code volgt `claude/docs/code-standaarden.md`: o.a. class met `required init` voor modellen, klassieke constructors, **nooit `sealed`**, `CancellationToken` altijd verplicht, design patterns volgens GoF/refactoring.guru.

## Commando's

```bash
dotnet build                                                        # warnings = errors, code style afgedwongen
dotnet test                                                         # Microsoft.Testing.Platform (global.json), xUnit v3
dotnet test --project src/Unshackle.Tests -- --filter-method "*ShouldBe*"   # één test
dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"
dotnet run --project src/Unshackle.Interface -- scan MijnApp.sln --fix fluentassertions
dotnet test --solution samples/SampleShop/SampleShop.slnx         # sample-doelcode (oude FA/MediatR/AutoMapper)
```

Exitcode 8 bij `dotnet test` betekent "geen tests gevonden", geen fout.

CI: `.github/workflows/ci.yml` draait build, tests en de sample-tests op elke PR naar `main` en elke push naar `main`.

## Architectuur in het kort

- `Interface → Core ← DataAccess`, `Interface → DataAccess`. **Core verwijst naar geen ander project.** Nieuwe I/O hoort in DataAccess, achter een interface uit Core; `Interface/Program.cs` is de composition root.
- Core: `Fixer` → `IFix` (één per bibliotheek) → `IPattern` (`Find` + `Rewrite`). Een pattern gooit nooit; mislukt een rewrite, dan `Manual(reden)` voor het rapport.
- Eén testproject `src/Unshackle.Tests`: unit (`Core/`, `DataAccess/`, `Interface/`), `Proofs/`, en `Integration/` (draait op `samples/` via MSBuild, traag). `samples/` is "gebruikerscode": eigen props en `.editorconfig`, niet in `Unshackle.slnx`, valt niet onder onze code-standaarden.
- Elk nieuw patroon krijgt proofs in `src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/` (`*.before.cs` + `*.after.cs` of `*.manual.txt`). `Proofs/` wordt niet gecompileerd; `before` moet compileren tegen de oude bibliotheek, `after` tegen de nieuwe.
- Types in unittests komen uit echte assemblies (FluentAssertions 7.2.2, MediatR 12.5.0, AutoMapper 14.0.0, Shouldly, Mediator.Abstractions, `Basic.Reference.Assemblies.Net100`), niet uit een build.
- Mock alleen eigen interfaces met NSubstitute, nooit Roslyn-typen. Geen Moq.

## Pakketten

- Versies alleen in `Directory.Packages.props` (central package management).
- Geen pakketten met licentie- of sponsorchecks (daarom is Verify eruit). AutoMapper 14.0.0 staat in de tests met `NoWarn="NU1903"`: bewust de laatste vrije versie, alleen als types voor proofs.
- Geen `Microsoft.Build.Locator`: Roslyn 5.x `MSBuildWorkspace` gebruikt een eigen build host.
