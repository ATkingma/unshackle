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
| `docs/projectinfo/architectuur/proofs.md`                  | Alle proof-tests per patroon met uitkomst en korte uitleg (gegenereerd: `python3 claude/tools/proofs-doc.py`)                                                                                                                                                                                                        |
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

| Locatie | Inhoud |
| ------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `claude/tasks/` | Actieve taken: `TAAK-NNN-<slug>.md` |
| `claude/tasks/TAAK-019-lancering.md` | Volgende stap: lancering (echte casus, NuGet, publiek, README-GIF) |
| `claude/tasks/archief/TAAK-031-fa-assertion-scope.md` | ✅ Assertions in `AssertionScope` → Manual; patroon `fa.assertion-scope` |
| `claude/tasks/archief/TAAK-040-fa-expression-bodied-void.md` | ✅ `void M() => x.Should()...` wordt herschreven |
| `claude/tasks/archief/TAAK-041-fa-null-conditional-subject.md` | ✅ `a?.B.Should()...` → `a?.B.ShouldX()` (zelfde semantiek) |
| `claude/tasks/archief/TAAK-042-fa-throwasync-configure-await.md` | ✅ `ThrowAsync<T>().ConfigureAwait(x)` blijft behouden |
| `claude/tasks/archief/TAAK-032-fa-not-be.md` | ✅ `x.Should().NotBe(y)` → `x.ShouldNotBe(y)`; patroon `fa.not-be` |
| `claude/tasks/archief/TAAK-033-fa-and-chain.md` | ✅ `x.Should().A().And.B()` → losse Shouldly-statements via de schakel-patronen; patroon `fa.and-chain` |
| `claude/tasks/archief/TAAK-034-fa-be-of-type.md` | ✅ `BeOfType<T>()`/`BeAssignableTo<T>()` (+ `Not`, `.Which`) → `ShouldBeOfType<T>()` e.d.; patroon `fa.be-of-type` |
| `claude/tasks/archief/TAAK-035-fa-string-start-end.md` | ✅ `StartWith`/`EndWith` (+ `Not`, `EquivalentOf`) en `(Not)BeNullOr(Empty | WhiteSpace)`→ Shouldly met`Case.Sensitive`; patroon `fa.string-start-end` |
| `claude/tasks/archief/TAAK-036-fa-contain-single-which.md` | ✅ `ContainSingle()`/`ContainSingle(pred)` (+ `var y = ….Which`) → `ShouldHaveSingleItem()`/`ShouldContain(pred, 1)`; patroon `fa.contain-single-which` |
| `claude/tasks/archief/TAAK-038-fa-equal-sequence.md` | ✅ `Equal(seq)`/`Equal(params)` → `ShouldBe(seq)` (volgorde-gevoelig), `NotEqual` Manual; patroon `fa.equal-sequence` |
| `claude/tasks/archief/TAAK-039-fa-have-length.md` | ✅ string `HaveLength(n)` → `Length.ShouldBe(n)`; patroon `fa.have-length` |
| `claude/tasks/archief/TAAK-050-fa-be-approximately.md` | ✅ `(Not)BeApproximately`/`(Not)BeCloseTo` → `ShouldBe(x, tol)`/`ShouldNotBe`/`Math.Abs(..).ShouldBeGreaterThan`; patroon `fa.be-approximately` |
| `claude/tasks/archief/TAAK-051-fa-throw-exactly.md` | ✅ `ThrowExactly(Async)<T>` → `Should.Throw(Async)<T>(..).ShouldBeOfType<T>()`; patroon `fa.throw-exactly` |
| `claude/tasks/archief/TAAK-052-fa-invoking.md` | ✅ `x.Invoking/Awaiting(s => ..)` → `Should.Throw/ThrowAsync/NotThrow(() => ..)`; patroon `fa.invoking` |
| `claude/tasks/archief/TAAK-023-mediatr-request-handler.md` | ✅ `Task<T> Handle` → `ValueTask<T> Handle`; patroon `mediatr.request-handler` |
| `claude/tasks/archief/TAAK-043-fa-inactive-preprocessor-branch.md` | ✅ Inactieve `#if`-code: oude using blijft + Manual (`usings`) |
| `claude/tasks/archief/TAAK-044-fa-global-using.md` | ✅ `global using` projectbreed geswapt, of blijft naast de nieuwe + Manual |
| `claude/tasks/archief/TAAK-025-mediatr-void-request-handler.md` | ✅ `Task Handle` → `ValueTask<Unit> Handle` + `Unit.Value`; patroon `mediatr.void-request-handler`, helper `MediatRSyntax` |
| `claude/tasks/archief/TAAK-026-mediatr-notification-handler.md` | ✅ notification-handler `Task Handle` → `ValueTask Handle`; patroon `mediatr.notification-handler` |
| `claude/tasks/archief/TAAK-027-mediatr-pipeline-behavior.md` | ✅ `IPipelineBehavior` → Mediator (`ValueTask`, `MessageHandlerDelegate`, `next(request, ct)`, `IMessage`); proof-runner draait de Mediator-source-generator |
| `claude/tasks/archief/TAAK-028-mediatr-add-mediatr.md` | ✅ `AddMediatR(cfg => ...)` → `AddMediator(cfg => ...)`; patroon `mediatr.add-mediatr` |
| `claude/tasks/archief/TAAK-045-flaky-integratietest.md` | ✅ Integratietest: kind-`dotnet` geïsoleerd (geen node reuse, geen gelekte MSBuild-env) |
| `claude/tasks/archief/TAAK-047-fa-withmessage-line-endings.md` | ✅ `WithMessage` met regeleinde: `IgnoreLineEndings` of Manual |
| `claude/tasks/archief/TAAK-048-fa-throw-aggregate-exception.md` | ✅ `Throw`/`ThrowAsync`: actie met `Wait`/`.Result`/`Parallel.*` → Manual (AggregateException) |
| `claude/tasks/archief/TAAK-049-fa-equivalent-runtime-type.md` | ✅ `BeEquivalentTo` object: waarschuwing bij niet-sealed types (runtime-type) |
| `claude/tasks/archief/TAAK-080-mediatr-marker-interface-handler.md` | ✅ Bug: handler via eigen marker-interface (`ICommandHandler<T> : IRequestHandler<T>`) niet gevonden |
| `claude/tasks/archief/TAAK-081-mediatr-behavior-request-constraint.md` | ✅ Bug: behavior met `IRequest<TResponse>`-constraint krijgt overbodig `IMessage` |
| `claude/tasks/archief/TAAK-092-fa-be-equivalent-to-collection.md` | ✅ `BeEquivalentTo` op een collectie zonder options → `ShouldBe(expected, ignoreOrder: true)` |
| `claude/tasks/archief/TAAK-093-fa-be-same-as.md` | ✅ `BeSameAs`/`NotBeSameAs` → `ShouldBeSameAs`/`ShouldNotBeSameAs` |
| `claude/tasks/archief/TAAK-094-fa-not-throw-async.md` | ✅ `await act.Should().NotThrowAsync()` → `await Should.NotThrowAsync(act)` |
| `claude/tasks/archief/TAAK-095-fa-date-count-comparisons.md` | ✅ `BeAfter`/`BeOnOrAfter`/`BeBefore`/`BeOnOrBefore` en `HaveCountGreaterThan…`/`HaveCountLessThan…` |
| `claude/tasks/archief/TAAK-096-fa-which-chain.md` | ✅ `.Which`/`.Subject` na waarde-teruggevende assertions |
| `claude/tasks/archief/TAAK-097-fa-because-args.md` | ✅ becauseArgs centraal → `string.Format` |
| `claude/tasks/archief/TAAK-098-fa-hoist-subject.md` | ✅ Subject met bijwerkingen → `var subject = …;` |
| `claude/tasks/archief/TAAK-099-fa-match.md` | ✅ `fa.match`: MatchRegex/NotMatchRegex en wildcard-Match |
| `claude/tasks/archief/TAAK-100-fa-assertion-scope-satisfy.md` | ✅ AssertionScope → `ShouldSatisfyAllConditions` |
| `claude/tasks/archief/TAAK-101-fa-direct-mappings.md` | ✅ Kleine mappings |
| `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` | Frequentietabel MediatR/AutoMapper-API's uit 9 open-source projecten, dekking door patronen |
| `claude/tasks/archief/TAAK-110-unrecognized-should-keeps-using.md` | ✅ Bug (corpus): niet-herkende `.Should()` → using toch weg (CSFE, CS1061) |
| `claude/tasks/archief/TAAK-116-one-run-lambda-overload.md` | ✅ Eén fix-run volstaat: Shouldly als referentie in het model (CSFE 21 `usings`-Manuals → 0) |
| `claude/tasks/archief/TAAK-111-project-file-not-editable-blocks-rewrite.md` | ✅ Bug (corpus): csproj niet aanpasbaar (BOM?) → code wel herschreven, Shouldly ontbreekt (Polly, Ardalis.Result) |
| `claude/tasks/archief/TAAK-112-bogus-load-builder-project.md` | ✅ Bug (corpus): Bogus laadt niet (Builder 'System') |
| `claude/tasks/archief/TAAK-113-equal-sequence-collection-expression.md` | ✅ Bug (corpus): `Equal([..])` → `new T[] { [..] }` (PowerApps, CS0029) |
| `claude/tasks/archief/TAAK-114-be-empty-guid.md` | ✅ Bug (corpus): `Guid` `NotBeEmpty()` → `ShouldNotBe(Guid.Empty)` (Polly, CS1061) |
| `claude/tasks/archief/TAAK-115-usings-stijl-en-overbodig.md` | ✅ Bug (corpus): toegevoegde usings buiten namespace of overbodig (evo1, IDE0065/IDE0005) |
| `claude/tasks/archief/TAAK-130-mediatr-cross-file-marker-volgorde.md` | ✅ TAAK-130 — MediatR: handler via marker-interface in ander bestand wordt gemist (bestandsvolgorde) |
| `claude/tasks/TAAK-131-mediatr-namespace-schaduw.md` | TAAK-131 — MediatR: `using Mediator;` binnen een namespace wordt overschaduwd door eigen `*.Mediator`-namespace |
| `claude/tasks/archief/TAAK-132-mediatr-abstracte-generieke-basis.md` | ✅ TAAK-132 — MediatR: abstracte generieke basisklasse onterecht Manual ("generic messages") |
| `claude/tasks/archief/TAAK-133-mediatr-extern-pakket-op-mediatr.md` | ✅ TAAK-133 — MediatR: pakketten die zelf op MediatR bouwen (Ardalis.SharedKernel) worden niet gezien |
| `claude/tasks/archief/TAAK-134-projectbestand-roundtrip.md` | ✅ TAAK-134 — Projectbestanden: "cannot be edited without changing unrelated text" bij gewone opmaak |
| `claude/tasks/TAAK-135-loader-razor-en-vreemde-projecten.md` | TAAK-135 — Laden: Razor-componenten, `.dcproj` en niet-ladende bijprojecten blokkeren de hele solution |
| `claude/tasks/archief/TAAK-136-mediatr-msg0005-zonder-handler.md` | ✅ TAAK-136 — MediatR: Mediator-generator MSG0005 breekt build met TreatWarningsAsErrors |
| `claude/tasks/archief/TAAK-131-mediatr-namespace-schaduw.md` | ✅ TAAK-131 — MediatR: `using Mediator;` binnen een namespace wordt overschaduwd door eigen `*.Mediator`-namespace |
| `claude/tasks/TAAK-132-mediatr-abstracte-generieke-basis.md` | TAAK-132 — MediatR: abstracte generieke basisklasse onterecht Manual ("generic messages") |
| `claude/tasks/TAAK-133-mediatr-extern-pakket-op-mediatr.md` | TAAK-133 — MediatR: pakketten die zelf op MediatR bouwen (Ardalis.SharedKernel) worden niet gezien |
| `claude/tasks/archief/TAAK-135-loader-razor-en-vreemde-projecten.md` | ✅ TAAK-135 — Laden: Razor-componenten, `.dcproj` en niet-ladende bijprojecten blokkeren de hele solution |
| `claude/tasks/TAAK-136-mediatr-msg0005-zonder-handler.md` | TAAK-136 — MediatR: Mediator-generator MSG0005 breekt build met TreatWarningsAsErrors |
| `claude/tasks/archief/TAAK-118-gedeeld-pakketbestand-niet-geladen-projecten.md` | ✅ TAAK-118 — Gedeeld `Directory.*.props` houdt het oude pakket als een niet-gemigreerd project op schijf het nog gebruikt |
| `claude/tasks/CORPUSRUN-fa.md` | Corpus-run FA: 2/9 bouwen na fix; blokkades TAAK-110..113 |
| `claude/tasks/CORPUSRUN-mediatr.md` | Corpus-run MediatR: 0/9 volledig gemigreerd, blokkades en top-10 acties |
| `claude/tasks/REGRESSIE-corpus.md` | Regressie-run per test: 8/10 identiek; Polly → TAAK-140 |
| `claude/tasks/REGRESSIE-corpus-2.md` | Regressie-run ronde 2 (Facility, MODiX): 0 regressies; foutklassen → TAAK-152..154 |
| `claude/tasks/archief/TAAK-152-using-volgorde-na-hernoemen.md` | TAAK-152 — Hernoemde using niet gesorteerd → SA1210 (Facility) |
| `claude/tasks/archief/TAAK-153-equal-sequence-opmaak-meerdere-regels.md` | TAAK-153 — `Equal(` over meerdere regels wordt één regel |
| `claude/tasks/archief/TAAK-154-mediatr-handler-expression-body-task.md` | TAAK-154 — MediatR: `Task Handle(…) => X(…)` → Manual blokkeert hele migratie (MODiX) |
| `claude/tasks/archief/TAAK-140-catch-filter-op-fa-exception.md` | Bug: eigen code vangt FA-exception → Shouldly-exception niet gevangen (Polly) |
| `claude/tasks/archief/TAAK-143-harnas-mtp.md` | Harnas ondersteunt MTP (PowerApps: 1178 tests, 0 regressies) |
| `claude/tasks/archief/TAAK-144-mediatr-evo2-naamconflict-generator.md` | evo2: eigen `ICommand` vs `Mediator.ICommand` (volledige naam), gedeelde/internal registratie → Manual |
| `claude/tasks/archief/TAAK-147-mediatr-restpunten-144.md` | Generator alleen bij echte `AddMediatR`, global using op naamconflict, `InternalsVisibleTo` → Manual |
| `claude/tasks/archief/TAAK-148-mediatr-friend-echte-aanroep.md` | `InternalsVisibleTo` alleen Manual bij echte aanroep uit ander project (`ISolutionPattern`) |
| `claude/tasks/patroon-queue.md` | Wachtrij FA → Shouldly-patronen: welk patroon, welke taak, status (ronde 2: rijen 15-34) |
| `claude/tasks/mediatr-queue.md` | Wachtrij MediatR → Mediator-patronen; infrastructuur in TAAK-020 |
| `claude/tasks/automapper-queue.md` | Wachtrij AutoMapper → handgeschreven mapping (voorstel, op frequentie) |
| `claude/tasks/archief/` | Voltooide taken |
| `claude/tasks/archief/TAAK-029-mediatr-send-as-task.md` | ✅ niet-ge-awaite `Send`/`Publish` → `.AsTask()`; patroon `mediatr.send-as-task` |
| `claude/tasks/archief/TAAK-037-mediatr-unsupported.md` | ✅ pre/post/exception/publisher/stream-behavior/generieke berichten → Manual met advies; stream-request-swap; `RegisterGenericHandlers` valt weg; end-to-end SampleShop |
| `claude/tasks/archief/TAAK-060-mediatr-notification-publisher-config.md` | ✅ `NotificationPublisher = new TaskWhenAllPublisher()` → `NotificationPublisherType`; CS0104 na swap |
| `claude/tasks/archief/TAAK-061-mediatr-processors-exception-handlers.md` | Pre/post-processors en exception-handlers herschrijven (klaar; ook alles-of-niets) |
| `claude/tasks/archief/TAAK-062-mediatr-stream-pipeline-behavior.md` | (klaar) `IStreamPipelineBehavior` herschrijven, `AddOpenStreamBehavior` → `StreamPipelineBehaviors` |
| `claude/tasks/archief/TAAK-063-mediatr-source-generator-alleen-host.md` | ✅ Source generator alleen in host-project (CS0436 in testprojecten) |
| `claude/tasks/TAAK-040-fa-expression-bodied-void.md` | Bug: `void M() => x.Should()...` wordt Manual |
| `claude/tasks/TAAK-041-fa-null-conditional-subject.md` | Bug: `a?.B.Should()...` wordt Manual "result is used" |
| `claude/tasks/TAAK-042-fa-throwasync-configure-await.md` | Bug: `ThrowAsync<T>().ConfigureAwait(false)` wordt Manual |
| `claude/tasks/TAAK-043-fa-inactive-preprocessor-branch.md` | Bug: inactieve `#if`-tak blijft FA, using verdwijnt |
| `claude/tasks/TAAK-044-fa-global-using.md` | Bug: `global using FluentAssertions;` wordt niet vervangen |
| `claude/tasks/TAAK-070-fa-assertion-in-throw-lambda.md` | Bug: assertion in lambda met `Throw<XunitException>` → regressie na fix |
| `claude/tasks/archief/TAAK-071-fa-equivalent-collection-params.md` | ✅ `BeEquivalentTo(params)` op collectie → Manual met collectie-reden |
| `claude/tasks/archief/TAAK-072-fa-contain-single-subject.md` | ✅ `ContainSingle().Subject` als `.Which` |
| `claude/tasks/archief/TAAK-073-unused-fa-using-after-package-removal.md` | ✅ Ongebruikte oude using ook verwijderd in bestanden zonder findings |
| `claude/tasks/TAAK-061-mediatr-processors-exception-handlers.md` | Pre/post-processors en exception-handlers herschrijven i.p.v. Manual |
| `claude/tasks/TAAK-062-mediatr-stream-pipeline-behavior.md` | `IStreamPipelineBehavior` herschrijven, `AddOpenStreamBehavior` → `StreamPipelineBehaviors` |
| `claude/tasks/archief/TAAK-090-loader-nuget-warnings.md` | ✅ Loader: NuGet-audit/NU1701 en ontbrekend projectbestand zijn waarschuwingen, geen laadfout |
| `claude/tasks/archief/TAAK-091-custom-assertions-gegroepeerd.md` | ✅ `fa.custom-assertion`: eigen FA-extensies Manual, gegroepeerd per extensie in rapport en console |
| `claude/tasks/archief/TAAK-117-mediatr-ongebruikte-assembly-parameter.md` | ✅ TAAK-117 — MediatR: ongebruikte assembly-parameter na AddMediator (IDE0060) → waarschuwing in rapport |
| `claude/tasks/archief/TAAK-070-fa-assertion-in-throw-lambda.md` | ✅ Assertion in lambda met `Throw<XunitException>` erop → assertion en Throw Manual |

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
