# Documentatie-index

Snelle opzoekgids voor agents. Lees dit i.p.v. zelf zoeken.

## Project

| Document                                                   | Inhoud                                                                                                                                                                                                                                                                                                               |
| ---------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CLAUDE.md`                                                | Commando's, architectuur in het kort, pakketregels                                                                                                                                                                                                                                                                   |
| `docs/projectinfo/README.md`                               | Hoofddocument: C4 (context, container, component), links naar alles, patroontabel                                                                                                                                                                                                                                    |
| `docs/projectinfo/project.md`                              | Idee, probleem, bestaande tools, CLI, voorbeelden van herschrijvingen, roadmap                                                                                                                                                                                                                                       |
| `docs/projectinfo/architectuur/projecten.md`               | Projecten, afhankelijkheden, build-instellingen                                                                                                                                                                                                                                                                      |
| `docs/projectinfo/architectuur/core.md`                    | Fixer → Fix → Pattern (ontwerp + zoals gebouwd), SOLID                                                                                                                                                                                                                                                               |
| `docs/projectinfo/architectuur/testen.md`                  | Proofs, types, mocking, samples                                                                                                                                                                                                                                                                                      |
| `docs/projectinfo/architectuur/corpus.md`                  | Testcorpus: FA-API-frequentie in 14 projecten, dekking, fix-runs (FluentResults, Ardalis.Result)                                                                                                                                                                                                                     |
| `docs/projectinfo/architectuur/edge-cases.md`              | Edge cases uit open-source projecten → `edge-*` proofs, pending bugs                                                                                                                                                                                                                                                 |
| `docs/projectinfo/architectuur/rapportage.md`              | Rapportformaten                                                                                                                                                                                                                                                                                                      |
| `docs/projectinfo/beslissingen.md`                         | Codestijl-verwijzing, nog te beslissen                                                                                                                                                                                                                                                                               |
| `docs/projectinfo/modellen/klassen.md`                     | UML-klassendiagram: contracten en modellen van Core                                                                                                                                                                                                                                                                  |
| `docs/projectinfo/modellen/fix-flow.md`                    | UML-sequence van `Fixer.FixAsync`                                                                                                                                                                                                                                                                                    |
| `docs/projectinfo/modellen/pattern-beslissing.md`          | UML-activity: Fixed of Manual                                                                                                                                                                                                                                                                                        |
| `docs/projectinfo/modellen/patronen.md`                    | Overzicht alle patterns: FA-vorm, Shouldly-vorm, Manual                                                                                                                                                                                                                                                              |
| `docs/projectinfo/patterns/fa-be-greater-less-modellen.md` | Klassen-, sequence- en beslismodel van `fa.be-greater-less`                                                                                                                                                                                                                                                          |
| `docs/projectinfo/patterns/`                               | KingmaDoc plan + verify per patroon (`<id>-plan.md`, `<id>-verify.md`)                                                                                                                                                                                                                                               |
| `docs/projectinfo/features/`                               | KingmaDoc plan + verify per feature (geen patroon), bv. `end-to-end-keten-plan.md`, `projectbestanden-migreren-plan.md`/`-verify.md`, `mediatr-fix-plan.md`/`-verify.md`, `gelijknamige-typen-swap-plan.md`/`-verify.md`; config: `output_dir` op deze map zetten (`kingmadoc check -c <kopie van .featuredoc.yml>`) |
| `docs/issues.md`                                           | Publieke uitleg: issues aanmaken (missend patroon, nieuwe library, foute rewrite)                                                                                                                                                                                                                                    |
| `.github/ISSUE_TEMPLATE/`                                  | Issue-formulieren: missing-pattern, new-library, wrong-rewrite                                                                                                                                                                                                                                                       |
| `README.md`                                                | Publieke korte uitleg + projecttabel                                                                                                                                                                                                                                                                                 |

## Werkwijze voor agents

| Document                          | Inhoud                                                                |
| --------------------------------- | --------------------------------------------------------------------- |
| `claude/docs/overdracht.md`       | Stand bij accountwissel: lopend werk, openstaande opdracht, afspraken |
| `claude/docs/agent-werkwijze.md`  | Taakbeheer, cross-referenties, documentsplitsing, gedragsregels       |
| `claude/docs/code-standaarden.md` | Codestijl (16 keuzes)                                                 |
| `claude/docs/patroon-loop.md`     | Loop die elke 15 min een agent per patroon start (tick + agent-brief) |
| `claude/docs/design-patterns.md`  | GoF / refactoring.guru: welk patroon waar in Unshackle                |

## Taken

| Locatie                                                            | Inhoud                                                                                                                                                       |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `claude/tasks/`                                                    | Actieve taken: `TAAK-NNN-<slug>.md`                                                                                                                          |
| `claude/tasks/TAAK-019-lancering.md`                               | Volgende stap: lancering (echte casus, NuGet, publiek, README-GIF)                                                                                           |
| `claude/tasks/archief/TAAK-031-fa-assertion-scope.md`              | ✅ Assertions in `AssertionScope` → Manual; patroon `fa.assertion-scope`                                                                                     |
| `claude/tasks/archief/TAAK-040-fa-expression-bodied-void.md`       | ✅ `void M() => x.Should()...` wordt herschreven                                                                                                             |
| `claude/tasks/archief/TAAK-041-fa-null-conditional-subject.md`     | ✅ `a?.B.Should()...` → `a?.B.ShouldX()` (zelfde semantiek)                                                                                                  |
| `claude/tasks/archief/TAAK-042-fa-throwasync-configure-await.md`   | ✅ `ThrowAsync<T>().ConfigureAwait(x)` blijft behouden                                                                                                       |
| `claude/tasks/archief/TAAK-032-fa-not-be.md`                       | ✅ `x.Should().NotBe(y)` → `x.ShouldNotBe(y)`; patroon `fa.not-be`                                                                                           |
| `claude/tasks/archief/TAAK-033-fa-and-chain.md`                    | ✅ `x.Should().A().And.B()` → losse Shouldly-statements via de schakel-patronen; patroon `fa.and-chain`                                                      |
| `claude/tasks/archief/TAAK-034-fa-be-of-type.md`                   | ✅ `BeOfType<T>()`/`BeAssignableTo<T>()` (+ `Not`, `.Which`) → `ShouldBeOfType<T>()` e.d.; patroon `fa.be-of-type`                                           |
| `claude/tasks/archief/TAAK-035-fa-string-start-end.md`             | ✅ `StartWith`/`EndWith` (+ `Not`, `EquivalentOf`) en `(Not)BeNullOr(Empty                                                                                   | WhiteSpace)`→ Shouldly met`Case.Sensitive`; patroon `fa.string-start-end` |
| `claude/tasks/archief/TAAK-036-fa-contain-single-which.md`         | ✅ `ContainSingle()`/`ContainSingle(pred)` (+ `var y = ….Which`) → `ShouldHaveSingleItem()`/`ShouldContain(pred, 1)`; patroon `fa.contain-single-which`      |
| `claude/tasks/archief/TAAK-023-mediatr-request-handler.md`         | ✅ `Task<T> Handle` → `ValueTask<T> Handle`; patroon `mediatr.request-handler`                                                                               |
| `claude/tasks/archief/TAAK-043-fa-inactive-preprocessor-branch.md` | ✅ Inactieve `#if`-code: oude using blijft + Manual (`usings`)                                                                                               |
| `claude/tasks/archief/TAAK-044-fa-global-using.md`                 | ✅ `global using` projectbreed geswapt, of blijft naast de nieuwe + Manual                                                                                   |
| `claude/tasks/archief/TAAK-025-mediatr-void-request-handler.md`    | ✅ `Task Handle` → `ValueTask<Unit> Handle` + `Unit.Value`; patroon `mediatr.void-request-handler`, helper `MediatRSyntax`                                   |
| `claude/tasks/archief/TAAK-026-mediatr-notification-handler.md`    | ✅ notification-handler `Task Handle` → `ValueTask Handle`; patroon `mediatr.notification-handler`                                                           |
| `claude/tasks/archief/TAAK-027-mediatr-pipeline-behavior.md`       | ✅ `IPipelineBehavior` → Mediator (`ValueTask`, `MessageHandlerDelegate`, `next(request, ct)`, `IMessage`); proof-runner draait de Mediator-source-generator |
| `claude/tasks/archief/TAAK-028-mediatr-add-mediatr.md`             | ✅ `AddMediatR(cfg => ...)` → `AddMediator(cfg => ...)`; patroon `mediatr.add-mediatr`                                                                       |
| `claude/tasks/patroon-queue.md`                                    | Wachtrij FA → Shouldly-patronen: welk patroon, welke taak, status (ronde 2: rijen 15-34)                                                                     |
| `claude/tasks/mediatr-queue.md`                                    | Wachtrij MediatR → Mediator-patronen; infrastructuur in TAAK-020                                                                                             |
| `claude/tasks/archief/`                                            | Voltooide taken                                                                                                                                              |
| Locatie                                                            | Inhoud                                                                                                                                                       |
| ----------------------------------------------------------         | ----------------------------------------------------------------------------------------                                                                     |
| `claude/tasks/`                                                    | Actieve taken: `TAAK-NNN-<slug>.md`                                                                                                                          |
| `claude/tasks/TAAK-019-lancering.md`                               | Volgende stap: lancering (echte casus, NuGet, publiek, README-GIF)                                                                                           |
| `claude/tasks/archief/TAAK-031-fa-assertion-scope.md`              | ✅ Assertions in `AssertionScope` → Manual; patroon `fa.assertion-scope`                                                                                     |
| `claude/tasks/TAAK-040-fa-expression-bodied-void.md`               | Bug: `void M() => x.Should()...` wordt Manual                                                                                                                |
| `claude/tasks/TAAK-041-fa-null-conditional-subject.md`             | Bug: `a?.B.Should()...` wordt Manual "result is used"                                                                                                        |
| `claude/tasks/TAAK-042-fa-throwasync-configure-await.md`           | Bug: `ThrowAsync<T>().ConfigureAwait(false)` wordt Manual                                                                                                    |
| `claude/tasks/TAAK-043-fa-inactive-preprocessor-branch.md`         | Bug: inactieve `#if`-tak blijft FA, using verdwijnt                                                                                                          |
| `claude/tasks/TAAK-044-fa-global-using.md`                         | Bug: `global using FluentAssertions;` wordt niet vervangen                                                                                                   |
| `claude/tasks/TAAK-070-fa-assertion-in-throw-lambda.md`            | Bug: assertion in lambda met `Throw<XunitException>` → regressie na fix                                                                                      |
| `claude/tasks/TAAK-071-fa-equivalent-collection-params.md`         | Bug: `BeEquivalentTo(params)` op collectie krijgt foute Manual-reden                                                                                         |
| `claude/tasks/TAAK-072-fa-contain-single-subject.md`               | Bug: `ContainSingle().Subject` wordt Manual                                                                                                                  |
| `claude/tasks/TAAK-073-unused-fa-using-after-package-removal.md`   | Bug: ongebruikte `using FluentAssertions;` blijft na pakketverwijdering (CS0246)                                                                             |
| `claude/tasks/archief/TAAK-045-flaky-integratietest.md`            | ✅ Integratietest: kind-`dotnet` geïsoleerd (geen node reuse, geen gelekte MSBuild-env)                                                                      |
| `claude/tasks/patroon-queue.md`                                    | Wachtrij FA → Shouldly-patronen: welk patroon, welke taak, status (ronde 2: rijen 15-34)                                                                     |
| `claude/tasks/mediatr-queue.md`                                    | Wachtrij MediatR → Mediator-patronen; infrastructuur in TAAK-020                                                                                             |
| `claude/tasks/archief/`                                            | Voltooide taken                                                                                                                                              |

## Skills

| Skill                                    | Gebruik                                                    |
| ---------------------------------------- | ---------------------------------------------------------- |
| `.claude/skills/taak-start/SKILL.md`     | Nieuwe taak aanmaken                                       |
| `.claude/skills/taak-specifiek/SKILL.md` | Taak met analyse en redenering (architectureel of riskant) |
| `.claude/skills/taak-oppakken/SKILL.md`  | Taak hervatten na sessie- of accountwissel                 |

## Hooks

| Hook                                  | Wanneer                         | Wat                                                                  |
| ------------------------------------- | ------------------------------- | -------------------------------------------------------------------- |
| `.claude/hooks/check-pattern-docs.sh` | Na Write/Edit van `*Pattern.cs` | Herinnert aan KingmaDoc plan/verify + README-rij                     |
|                                       | SubagentStop                    | Blokkeert (één keer) als een patroon plan, verify of README-rij mist |
|                                       | Stop (hoofdsessie)              | Waarschuwt alleen                                                    |

Config: `.claude/settings.json`; KingmaDoc-config: `.featuredoc.yml` (output `docs/projectinfo/patterns/`).

## Code (waar staat wat)

| Onderwerp                            | Pad                                                                             |
| ------------------------------------ | ------------------------------------------------------------------------------- |
| CLI, composition root                | `src/Unshackle.Interface/Cli.cs` (`Program.cs` roept alleen `Cli.RunAsync` aan) |
| Commands, console, diff              | `src/Unshackle.Interface/Commands/`, `Output/`                                  |
| Fixer, fixes, patterns               | `src/Unshackle.Core/`                                                           |
| Solutions laden, rapport, git-status | `src/Unshackle.DataAccess/{Solutions,Reports,SourceControl}/`                   |
| Proofs per patroon                   | `src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/`                                   |
| Integratietests                      | `src/Unshackle.Tests/Integration/` (draait op `samples/`)                       |
| Sample-codebase (doelcode)           | `samples/SampleShop/` (FA + MediatR + AutoMapper, los van onze build)           |
| Pakketversies                        | `Directory.Packages.props`                                                      |
| Build-instellingen                   | `Directory.Build.props`, `global.json`, `.editorconfig`                         |

## Zie ook

- `claude/docs/agent-werkwijze.md` — regels voor documentatie en taakbeheer
