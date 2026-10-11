# Documentatie-index

Snelle opzoekgids voor agents. Lees dit i.p.v. zelf zoeken.

## Project

| Document          | Inhoud                                                                                           |
| ----------------- | ------------------------------------------------------------------------------------------------ |
| `CLAUDE.md`       | Commando's, architectuur in het kort, pakketregels                                               |
| `docs/projectinfo/README.md` | Hoofddocument: C4 (context, container, component), links naar alles, patroontabel |
| `docs/projectinfo/project.md` | Idee, probleem, bestaande tools, CLI, voorbeelden van herschrijvingen, roadmap |
| `docs/projectinfo/architectuur/projecten.md` | Projecten, afhankelijkheden, build-instellingen |
| `docs/projectinfo/architectuur/core.md` | Fixer → Fix → Pattern (ontwerp + zoals gebouwd), SOLID |
| `docs/projectinfo/architectuur/testen.md` | Proofs, types, mocking, samples |
| `docs/projectinfo/architectuur/edge-cases.md` | Edge cases uit open-source projecten → `edge-*` proofs, pending bugs |
| `docs/projectinfo/architectuur/rapportage.md` | Rapportformaten |
| `docs/projectinfo/beslissingen.md` | Codestijl-verwijzing, nog te beslissen |
| `docs/projectinfo/modellen/klassen.md` | UML-klassendiagram: contracten en modellen van Core |
| `docs/projectinfo/modellen/fix-flow.md` | UML-sequence van `Fixer.FixAsync` |
| `docs/projectinfo/modellen/pattern-beslissing.md` | UML-activity: Fixed of Manual |
| `docs/projectinfo/modellen/patronen.md` | Overzicht alle patterns: FA-vorm, Shouldly-vorm, Manual |
| `docs/projectinfo/patterns/fa-be-greater-less-modellen.md` | Klassen-, sequence- en beslismodel van `fa.be-greater-less` |
| `docs/projectinfo/patterns/` | KingmaDoc plan + verify per patroon (`<id>-plan.md`, `<id>-verify.md`) |
| `docs/projectinfo/features/` | KingmaDoc plan + verify per feature (geen patroon), bv. `end-to-end-keten-plan.md`, `projectbestanden-migreren-plan.md`/`-verify.md`, `mediatr-fix-plan.md`/`-verify.md`, `gelijknamige-typen-swap-plan.md`/`-verify.md`; config: `output_dir` op deze map zetten (`kingmadoc check -c <kopie van .featuredoc.yml>`) |
| `docs/issues.md` | Publieke uitleg: issues aanmaken (missend patroon, nieuwe library, foute rewrite) |
| `.github/ISSUE_TEMPLATE/` | Issue-formulieren: missing-pattern, new-library, wrong-rewrite |
| `README.md`       | Publieke korte uitleg + projecttabel                                                             |

## Werkwijze voor agents

| Document                         | Inhoud                                                          |
| -------------------------------- | --------------------------------------------------------------- |
| `claude/docs/overdracht.md` | Stand bij accountwissel: lopend werk, openstaande opdracht, afspraken |
| `claude/docs/agent-werkwijze.md` | Taakbeheer, cross-referenties, documentsplitsing, gedragsregels |
| `claude/docs/code-standaarden.md` | Codestijl (16 keuzes)                                           |
| `claude/docs/patroon-loop.md`     | Loop die elke 15 min een agent per patroon start (tick + agent-brief) |
| `claude/docs/design-patterns.md`  | GoF / refactoring.guru: welk patroon waar in Unshackle          |

## Taken

| Locatie                 | Inhoud                              |
| ----------------------- | ----------------------------------- |
| `claude/tasks/`         | Actieve taken: `TAAK-NNN-<slug>.md` |
| `claude/tasks/TAAK-019-lancering.md` | Volgende stap: lancering (echte casus, NuGet, publiek, README-GIF) |
| `claude/tasks/archief/TAAK-031-fa-assertion-scope.md` | ✅ Assertions in `AssertionScope` → Manual; patroon `fa.assertion-scope` |
| `claude/tasks/TAAK-040-fa-expression-bodied-void.md` | Bug: `void M() => x.Should()...` wordt Manual |
| `claude/tasks/TAAK-041-fa-null-conditional-subject.md` | Bug: `a?.B.Should()...` wordt Manual "result is used" |
| `claude/tasks/TAAK-042-fa-throwasync-configure-await.md` | Bug: `ThrowAsync<T>().ConfigureAwait(false)` wordt Manual |
| `claude/tasks/TAAK-043-fa-inactive-preprocessor-branch.md` | Bug: inactieve `#if`-tak blijft FA, using verdwijnt |
| `claude/tasks/TAAK-044-fa-global-using.md` | Bug: `global using FluentAssertions;` wordt niet vervangen |
| `claude/tasks/archief/TAAK-032-fa-not-be.md` | ✅ `x.Should().NotBe(y)` → `x.ShouldNotBe(y)`; patroon `fa.not-be` |
| `claude/tasks/patroon-queue.md` | Wachtrij FA → Shouldly-patronen: welk patroon, welke taak, status (ronde 2: rijen 15-34) |
| `claude/tasks/mediatr-queue.md` | Wachtrij MediatR → Mediator-patronen; infrastructuur in TAAK-020 |
| `claude/tasks/archief/` | Voltooide taken                     |

## Skills

| Skill                                    | Gebruik                                                    |
| ---------------------------------------- | ---------------------------------------------------------- |
| `.claude/skills/taak-start/SKILL.md`     | Nieuwe taak aanmaken                                       |
| `.claude/skills/taak-specifiek/SKILL.md` | Taak met analyse en redenering (architectureel of riskant) |
| `.claude/skills/taak-oppakken/SKILL.md`  | Taak hervatten na sessie- of accountwissel                 |

## Hooks

| Hook                                  | Wanneer                    | Wat                                                                  |
| ------------------------------------- | -------------------------- | -------------------------------------------------------------------- |
| `.claude/hooks/check-pattern-docs.sh` | Na Write/Edit van `*Pattern.cs` | Herinnert aan KingmaDoc plan/verify + README-rij                  |
|                                       | SubagentStop               | Blokkeert (één keer) als een patroon plan, verify of README-rij mist |
|                                       | Stop (hoofdsessie)         | Waarschuwt alleen                                                    |

Config: `.claude/settings.json`; KingmaDoc-config: `.featuredoc.yml` (output `docs/projectinfo/patterns/`).

## Code (waar staat wat)

| Onderwerp                | Pad                                                       |
| ------------------------ | --------------------------------------------------------- |
| CLI, composition root    | `src/Unshackle.Interface/Cli.cs` (`Program.cs` roept alleen `Cli.RunAsync` aan) |
| Commands, console, diff  | `src/Unshackle.Interface/Commands/`, `Output/`            |
| Fixer, fixes, patterns   | `src/Unshackle.Core/`                                     |
| Solutions laden, rapport, git-status | `src/Unshackle.DataAccess/{Solutions,Reports,SourceControl}/` |
| Proofs per patroon       | `src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/`             |
| Integratietests          | `src/Unshackle.Tests/Integration/` (draait op `samples/`) |
| Sample-codebase (doelcode) | `samples/SampleShop/` (FA + MediatR + AutoMapper, los van onze build) |
| Pakketversies            | `Directory.Packages.props`                                |
| Build-instellingen       | `Directory.Build.props`, `global.json`, `.editorconfig`   |

## Zie ook

- `claude/docs/agent-werkwijze.md` — regels voor documentatie en taakbeheer
