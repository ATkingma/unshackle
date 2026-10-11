# REVIEW-B: MediatR, I/O, CLI en tests

Read-only review op `main` 3bd36cf. Scope: `Core/Fixes/MediatR/`, `DataAccess/`, `Interface/`, `Tests/` (Proofs-runner, Helpers, Integration).
Buiten beschouwing: wat TAAK-060/063/080/081 al raakt (notification publisher config, source generator alleen in host, marker-interface handlers / `symbol.Interfaces` vs `AllInterfaces` in `FindHandleMethods`, behavior request-constraint).

## Hoog

### H1. `fix` schrijft half: broncode staat al op schijf als een projectbestand daarna faalt

- **Waar:** `src/Unshackle.Interface/Commands/FixCommand.cs:80-88`, `src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs:85-99`
- **Probleem:** eerst `_store.SaveAsync` (alle `.cs`), dan `_projectFiles.SaveAsync`. De staleness-check (`text != change.OldText`) zit per bestand ín de schrijflus. Is het tweede projectbestand intussen gewijzigd (of niet leesbaar), dan zijn de `.cs`-bestanden én het eerste projectbestand al geschreven.
- **Waarom:** de code is gemigreerd naar `Mediator`/Shouldly maar de csproj verwijst nog naar het oude pakket → build stuk, half toegepaste migratie. Met `--force` (dirty tree) is dat niet via git terug te draaien. De test `FixExecuteAsync_ProjectFileChangedOnDisk_ReturnsFailure` bewijst alleen de exitcode, niet dat er niets geschreven is.
- **Fix:** in `IProjectFileStore` een check-fase scheiden (bv. `SaveAsync` eerst alle bestanden lezen en vergelijken, pas daarna schrijven), en in `FixCommand` die check vóór `_store.SaveAsync` doen. Test: twee changes, tweede stale → eerste bestand en solution onaangeraakt.

### H2. Broncode wordt overschreven zonder staleness-check

- **Waar:** `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs:40-52`
- **Probleem:** `TryApplyChanges` schrijft de documenten weg op basis van de in-memory solution; wijzigingen die de gebruiker tussen laden (MSBuild-load duurt lang) en schrijven maakt, gaan stil verloren. Voor projectbestanden bestaat die check wel.
- **Waarom:** dataverlies in gebruikerscode, inconsistent met `XmlProjectFileStore`.
- **Fix:** vóór `TryApplyChanges` per gewijzigd document `File.ReadAllText(document.FilePath)` vergelijken met de geladen `SourceText` van `Original`; bij verschil `InvalidOperationException` (wordt al netjes vertaald in `FixCommand:85`). Combineer met H1 zodat alle checks vóór de eerste write gebeuren.

### H3. Git-fout wordt als "schone working tree" gelezen

- **Waar:** `src/Unshackle.DataAccess/SourceControl/GitSourceControl.cs:29-31, 40-41`
- **Probleem:** elke exitcode ≠ 0 en een ontbrekende `git` geven `false` = "geen wijzigingen". Ook `fatal: detected dubious ownership` (exit 128, veelvoorkomend op gedeelde schijven/WSL/containers), een kapotte index of lock geven dus groen licht om te schrijven.
- **Waarom:** de enige bescherming tegen onomkeerbaar overschrijven valt stil weg, zonder melding.
- **Fix:** onderscheid "geen repository" (`git rev-parse --is-inside-work-tree` faalt, of stderr bevat `not a git repository`) van andere fouten; bij andere fouten en bij ontbrekende git: `true` teruggeven of een expliciete fout/waarschuwing in de view ("kan working tree niet controleren, gebruik --force"). Test met een fake git-fout (bv. directory zonder leesrechten).

### H4. Geen integratietest voor `--fix mediatr`

- **Waar:** `src/Unshackle.Tests/Integration/SampleShopTests.cs` (alle drie tests gebruiken `fluentassertions`); `CliTests.cs:42` test alleen "missing solution".
- **Probleem:** `samples/SampleShop` bevat MediatR-code (handlers, behavior, `ServiceCollectionExtensions`), maar de echte keten MSBuild-load → patterns → `Directory.Packages.props`-edit (twee added packages + `MediatR.Contracts` weg) → build + test met de source generator draait nergens.
- **Waarom:** juist het riskantste deel (pakketwissel met twee nieuwe pakketten, generator die `AddMediator` schrijft) is alleen met unit-proofs in-memory gedekt.
- **Fix:** `FixTwice_SampleShopCopy_...` parametriseren over fixId, of een aparte `FixMediatR_SampleShopCopy_BuildsAndTests` met `dotnet build`/`dotnet test` na de fix.

## Midden

### M1. Report kan elk bestand met `.md/.csv/.json` overschrijven, ook in de doelrepo

- **Waar:** `src/Unshackle.DataAccess/Reports/*ReportWriter.cs` (`File.Create`/`WriteAllLinesAsync`), `ScanCommand.cs:47`
- **Probleem:** `--report README.md` of `--report Directory.Packages.json`… overschrijft zonder vraag; bij `fix` gebeurt dat ná de git-check, dus het report zelf maakt de tree ook vuil voor een volgende run. Geen path traversal-risico in de klassieke zin (CLI-argument van de gebruiker zelf), wel stil dataverlies.
- **Fix:** weigeren als het doel al bestaat en niet eerder door Unshackle geschreven is, of minimaal: weigeren als het pad binnen de solution-map ligt en bestaat zonder `--force`. Goedkoopste variant: `FileMode.CreateNew` tenzij `--force`.

### M2. Markdown-report escapet alleen `|`

- **Waar:** `src/Unshackle.DataAccess/Reports/MarkdownReportWriter.cs:34`
- **Probleem:** `Reason` en `File` komen uit gebruikerscode (o.a. `'{setting}'` in `AddMediatRPattern.cs:200` zet de broncode letterlijk in de reden). `<script>`, `<img onerror>`, backticks en een eindigende `\` (die de `\|` van de volgende cel opheft) gaan ongeëscaped de tabel in. Gerenderd in een PR-comment of wiki is dat HTML-injectie / kapotte tabel.
- **Fix:** in `Cell` eerst `\` → `\\`, dan `|` → `\|`, en `<`, `>`, `&` HTML-encoden (`System.Net.WebUtility.HtmlEncode`). Test toevoegen met `<script>` en een trailing backslash.

### M3. `AddMediatRPattern` dupliceert/verliest commentaar in het lambda-blok

- **Waar:** `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs:211-214`
- **Probleem:** elke nieuwe statement krijgt de leading en trailing trivia van `body.Statements[0]`. Staat er boven de eerste regel een comment (`// handlers`), dan verschijnt die boven `ServiceLifetime =` én boven `PipelineBehaviors =`; comments bij latere statements verdwijnen.
- **Fix:** alleen de whitespace-indentatie van `Statements[0]` kopiëren (`.Where(t => t.IsKind(WhitespaceTrivia)).TakeLast(1)`, zoals `VoidRequestHandlerPattern.cs:521` al doet) en de comments van alle originele statements vóór de eerste nieuwe zetten. Proof toevoegen.

### M4. Alleen `Statements` met expressies; `if (env.IsDev) cfg.AddBehavior...` → Manual, maar `settings.Count == 0` dekt ook een leeg blok

- **Waar:** `AddMediatRPattern.cs:158-168`
- **Probleem:** `services.AddMediatR(cfg => { })` (leeg) wordt Manual met een misleidende reden ("not a list of cfg calls"), terwijl `AddMediator(cfg => { cfg.ServiceLifetime = ServiceLifetime.Transient; })` gewoon kan.
- **Fix:** leeg blok behandelen als `settings = []` met geldige rewrite; alleen niet-expressie-statements Manual.

### M5. `UnsupportedPattern` kijkt alleen naar direct geïmplementeerde interfaces

- **Waar:** `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs:439`
- **Probleem:** `class AuditPre : BasePre<Foo>` met `abstract class BasePre<T> : IRequestPreProcessor<T>` meldt alleen de basisclass; afgeleide generieke messages (`class Query<T> : BaseQuery`, `BaseQuery : IRequest`) worden helemaal niet gemeld, terwijl de generator erop breekt.
- **Waarom:** een generieke message die niet Manual wordt gerapporteerd geeft na de fix een build-fout in `Mediator.g.cs` zonder uitleg. (Als TAAK-080 dit voor `FindHandleMethods` oplost: dezelfde fix hier meenemen.)
- **Fix:** `symbol.AllInterfaces` gebruiken voor de `s_noGeneric`-check; advies-check mag op `AllInterfaces` blijven maar alleen rapporteren op het type dat de interface zelf declareert. Proof met afgeleide generieke query.

### M6. Padvergelijking case-sensitive in `XmlProjectFileStore`

- **Waar:** `XmlProjectFileStore.cs:38-39, 41`
- **Probleem:** `files` en `Keeps` gebruiken `Ordinal`. Op Windows kunnen `ProjectPath` (uit MSBuild) en `FindUp`-resultaat in casing verschillen (`src\App` vs `SRC\app`): hetzelfde bestand wordt twee keer geladen, twee changes met dezelfde `OldText`; de tweede `SaveAsync` ziet de eerste write en gooit "changed after it was read" — ná een gedeeltelijke write (zie H1). `Keeps` kan bovendien een gedeelde props-file missen en het pakket weghalen terwijl een project het nog nodig heeft.
- **Fix:** `Path.GetFullPath` normaliseren en `StringComparer.OrdinalIgnoreCase` op Windows (of `OperatingSystem.IsWindows() ? OrdinalIgnoreCase : Ordinal`) in een helper.

### M7. `<Using Include="MediatR" Alias="..."/>` en `Static` blijven staan terwijl het pakket weg gaat

- **Waar:** `XmlProjectFileStore.cs:10` (shortcut), `:160`
- **Probleem:** de shortcut is gedocumenteerd, maar het gevolg is niet "left alone" maar een build-fout: het pakket wordt wél verwijderd. Er komt geen Manual-finding.
- **Fix:** nu: als zo'n `<Using>` gevonden wordt, `EditAsync` laten signaleren (exception → wordt al Manual in `Fixer.cs:149`) of het project als `KeepRemovedPackage` behandelen. Shortcut-comment aanpassen.

### M8. Git-proces blijft draaien bij annulering

- **Waar:** `GitSourceControl.cs:33-38`; zelfde patroon in `SampleShopTests.cs:194-198`
- **Probleem:** `WaitForExitAsync(ct)` gooit bij cancel, maar het proces wordt niet gekild; `using` disposed alleen de handle. Bij `dotnet restore` in de integratietest blijven bij timeout child-processen (en MSBuild-nodes) hangen → flaky vervolg-runs.
- **Fix:** `catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }`. Het proces-starten zit nu drie keer gedupliceerd (GitSourceControl, `DotnetAsync`, `GitInitAsync`); een test-helper `ProcessRunner` in `Tests/Helpers` voor de twee testkopieën.

## Laag

### L1. `MsBuildSolutionStore` gebruikt `System.IO.File` direct

- **Waar:** `MsBuildSolutionStore.cs:16`
- **Probleem:** alle andere DataAccess-klassen krijgen `IFileSystem` geïnjecteerd; deze niet. Inconsistent, en niet te testen zonder echt bestand.
- **Fix:** `IFileSystem` via constructor (klassieke constructor, zoals de report writers).

### L2. `Cli.RunAsync` krijgt altijd `CancellationToken.None`

- **Waar:** `src/Unshackle.Interface/Program.cs:3`
- **Probleem:** werkt omdat System.CommandLine zelf Ctrl+C koppelt, maar dat is impliciet. Prima, alleen één comment waard. Geen actie nodig behalve comment.

### L3. Explicit-interface-check en `CommaList` gedupliceerd over MediatR-patterns

- **Waar:** `PipelineBehaviorPattern.cs:275, 336`, `VoidRequestHandlerPattern.cs:476`, (Request/NotificationHandler idem)
- **Fix:** `CommaList` naar `MediatRSyntax`; het `ExplicitInterfaceSpecifier`-Manual-bericht als gedeelde helper. Pas oppakken ná TAAK-060/063/080/081 om merge-conflicten te vermijden.

### L4. `SendAsTaskPattern` mist `_mediator?.Send(x)`

- **Waar:** `SendAsTaskPattern.cs:565`
- **Probleem:** bij null-conditional is `i.Expression` een `MemberBindingExpressionSyntax`; geen finding, terwijl het type `ValueTask?`-achtig wordt en `Task`-gebruik breekt zonder melding.
- **Fix:** `MemberBindingExpressionSyntax` herkennen en Manual rapporteren (`.AsTask()` op een nullable ValueTask is niet triviaal).

### L5. `ProjectFile` zoekt de sluittag tekstueel

- **Waar:** `src/Unshackle.DataAccess/Projects/ProjectFile.cs:29-30`
- **Probleem:** `LastIndexOf("</Project")` matcht ook een comment ná de root (`<!-- </Project> -->`); dan wordt `_suffix` verkeerd afgesneden en raakt de file corrupt bij schrijven. Ook alleen-`\r`-regeleinden breken de regelberekening.
- **Fix:** sluitpositie bepalen via `XmlReader` (laatste `EndElement` op depth 0) of een round-trip-check: `_prefix + root + _suffix` met ongewijzigd document moet `OldText` opleveren, anders gooien (wordt Manual).

### L6. Manual-proof met lege `.manual.txt` bewijst niets

- **Waar:** `src/Unshackle.Tests/ProofTests.cs:77-78`
- **Probleem:** `Reason.Contains("")` is altijd waar; een lege of per ongeluk geleegde file test dan alleen "is Manual".
- **Fix:** `reason.ShouldNotBeNullOrWhiteSpace("manual.txt must name the reason")` vóór de assert.

### L7. `ProofTests` faalt onduidelijk bij een tikfout in de mapnaam

- **Waar:** `ProofTests.cs:53-54`
- **Probleem:** `s_fixes[parts[0]]` → `KeyNotFoundException`, `.Single(...)` → "Sequence contains no matching element"; geen hint welke map.
- **Fix:** `ShouldContainKey` / `SingleOrDefault(...).ShouldNotBeNull($"no pattern {parts[1]}Pattern for proof {proof}")`.

### L8. `ConsoleText.Sanitize` laat bidi- en zero-width-tekens door

- **Waar:** `src/Unshackle.Interface/Output/ConsoleText.cs:12`
- **Probleem:** `char.IsControl` dekt U+202E (RLO), U+2066-2069 en U+200B niet (categorie `Format`). Een diff kan zo andere code tonen dan er geschreven wordt ("Trojan Source").
- **Fix:** ook `char.GetUnicodeCategory(c) == UnicodeCategory.Format` vervangen. Test toevoegen naast `Sanitize_EscapeSequence_...`.

## Eerst fixen

1. **H1 + H2** samen: alle staleness-checks (bron én projectbestanden) vóór de eerste write; test dat er bij een stale bestand niets geschreven is.
2. **H3**: git-fouten niet als "schoon" interpreteren.
3. **M1**: report mag geen bestaand bestand stil overschrijven.
4. **H4**: integratietest voor `--fix mediatr` op SampleShop met build + test.
5. **M2**: Markdown-cellen correct escapen (backslash + HTML).
