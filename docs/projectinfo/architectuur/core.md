# Core: Fixer → Fix → Pattern

```
Fixer                       de ene ingang: FixAsync(solution, request)
 └─ IFix                    één per bibliotheek (FluentAssertions, MediatR, AutoMapper)
     └─ IPattern            één soort code vinden + herschrijven
          ├─ Find(...)      → plekken in de code
          └─ Rewrite(...)   → Fixed(nieuwe node) of Manual(reden)
```

```
src/Unshackle.Core/
├─ Fixer.cs
├─ Abstractions/   IFix, IPattern, IFixer, ISolutionStore, IReportWriter
├─ Models/         Finding, PatternResult, FixRequest, FixReport
└─ Fixes/
   ├─ FluentAssertions/   FluentAssertionsFix.cs + Patterns/
   ├─ MediatR/            MediatRFix.cs + Patterns/
   └─ AutoMapper/         AutoMapperFix.cs + Patterns/
```

Elke fix heeft dezelfde indeling. Ken je er één, dan ken je ze allemaal.

**Afwijkende patronen** (bijvoorbeeld `HaveCount`, waar het type bepaalt wat de juiste
vervanging is) regel je binnen dat pattern. Wordt het te groot, dan splits je het in meerdere
patterns, zoals `HaveCountOnListPattern` en `HaveCountOnEnumerablePattern`. De Fixer merkt
daar niks van.

**Een pattern crasht nooit.** Lukt een herschrijving niet, dan geeft het `Manual(reden)`
terug en komt dat in het rapport.

**Dry-run kost niks extra.** Roslyn past een `Solution` nooit aan. De Fixer berekent een
nieuwe `Solution` en het rapport. Bij `--dry-run` tonen we alleen de diff, anders schrijft
`ISolutionStore` de wijzigingen weg.

## Zoals gebouwd (TAAK-001)

- `IPattern`: `Id`, `Description`, `Find(SyntaxNode root, SemanticModel model)` → nodes,
  `Rewrite(SyntaxNode node, SemanticModel model)` → `PatternResult`. Het pattern kiest zelf welke node het vervangt
  (expressie, buitenste aanroep van een keten, of een heel statement).
- `PatternResult.Fixed(node(s), params requiredNamespaces)` of `PatternResult.Manual(reason)`.
- `IFix`: `Id`, `Patterns`, `RemovedNamespaces`, `AddedNamespaces`.
- `Fixer`: per document de patterns na elkaar (vers `SemanticModel`), vervangen met `TrackNodes`/`ReplaceNode`
  (buitenste eerst, overlap → Manual), daarna usings via `UsingDirectives`. Exceptions per pattern → Manual.
  Elk bestand wordt gefixt op de **originele** solution (alleen dat bestand verandert in zijn werkkopie): de semantiek
  van andere bestanden hangt dus niet af van de volgorde (TAAK-130: marker-interface in een eerder bestand was al geswapt,
  waardoor een handler in een later bestand stil gemist werd). De eindtekst gaat daarna in de verzamelde solution.
  Per bestandspad één keer: een multi-target project (één Roslyn-project per TFM) wordt in de eerste TFM gefixt
  en gerapporteerd; de kopieën in de andere TFM's krijgen dezelfde tekst. Projectmigraties tellen ook per
  projectbestand (package blijft als één TFM het nog nodig heeft); `fix` telt geschreven bestanden uniek (TAAK-022).
- Details en redenen: `claude/tasks/archief/TAAK-001-fundament.md`.

## FluentAssertions-patronen: gedeelde basis (REVIEW-A)

- `FluentAssertionPattern` (Template Method): `Rewrite` doet de controles die elke `x.Should().Method(...)` deelt
  (AssertionScope, subjectvorm, gebruikt resultaat, becauseArgs) met één redentekst, daarna `RewriteAssertion(AssertionContext)`.
  Hooks: `GetAssertion` (statement of `.Which` als node), `IsResultDiscarded`/`ResultUsedReason`, `RewriteBeforeArguments`
  (params-overloads die de argumentsplitsing anders als becauseArgs ziet). De Throw-familie (Throw, ThrowAsync, ThrowExactly,
  NotThrow, Invoking) heeft een andere node-vorm en deelt `ThrowSyntax` en `ThrowMessage` in plaats van de basis.
- Gedeelde helpers in `FluentAssertionsSyntax`: `ConvertExpected`, `IsSafeReceiver`/`NotNullReceiver`, `IsSafeToEvaluateTwice`
  (één symboolgebaseerde regel), `TryGetSplittableStatement`/`NotSplittableReason`, `Arguments`/`Invoke`, `WithWhich`/`WithStatement`.
  Patronen roepen geen statics van andere patronen meer aan.
- `.And`-ketens zijn alleen van `AndChainPattern`; het geeft warnings van schakels door en accepteert schakels van meerdere statements.
- `UsingDirectives` swapt ook usings binnen een namespace-declaratie (blok of file-scoped). Nieuwe usings komen binnen de
  namespace als het bestand ze daar heeft (IDE0065), tenzij een eigen namespace de nieuwe overschaduwt; en alleen in een bestand
  dat de oude bibliotheek bij naam noemt (IDE0005, TAAK-115).
- `IFix.RemovedExtensionMethods` (FA: `Should`): een `x.Should()` die na de fix nergens aan bindt (ontvanger onbekend, bv. na een
  herschreven lambda-argument) houdt de oude using en het pakket en geeft een Manual-finding `usings` (TAAK-110).

## Contracten naar buiten (TAAK-005)

- `ISolutionStore`: `LoadAsync(path, ct)` → `Solution` (gooit `FileNotFoundException` of `SolutionLoadException` met
  de MSBuild-fouten, of als typen niet resolven (CS0246/CS0234: niet gerestored → advies `dotnet restore`). Alleen laden:
  `FixCommand` schrijft de gewijzigde `.cs`-bestanden samen met de projectbestanden via `IProjectFileStore.SaveAsync`
  (REVIEW-B H1/H2): eerst worden álle bestanden gecontroleerd (ongewijzigd sinds laden, UTF-8), pas dan geschreven
  (temp-bestand + move), en bij een fout halverwege worden de al geschreven bestanden teruggezet. Implementatie: `MsBuildSolutionStore`.
- `IReportWriter`: `Extension` + `WriteAsync(path, findings, ct)`. Zie `rapportage.md`.
- `ISourceControl`: `HasUncommittedChangesAsync(directory, ct)`; `fix` weigert op een vuile werkmap tenzij `--force`.
  Geen git-repository, geen git of een git-fout geeft `InvalidOperationException` (nooit "schoon"); `fix` weigert dan
  ook zonder `--force`. Processen starten via `DataAccess/Processes/ProcessRunner` (killt de procesboom bij annulering).
- `--report` weigert een bestaand bestand te overschrijven zonder `--force` (`scan` en `fix`).
- De Fixer zelf is niet veranderd: Interface laadt, roept `FixAsync` aan en laat DataAccess schrijven.

## Laden ondanks NuGet-warnings en ontbrekende projecten (TAAK-090)

- `MsBuildSolutionStore` geeft MSBuild alleen voor de design-time build globale properties mee: `NuGetAudit=false` en
  `WarningsNotAsErrors=NU1701;NU1900–NU1905`. Gebruikersbestanden blijven ongewijzigd.
- Roslyn meldt elke MSBuild-melding van een project als `Failure`, ook een gewone warning (NU1903, NU1701). Zo'n failure
  is alleen een waarschuwing (`ISolutionStore.Warnings`, door `scan`/`fix` getoond als `Warning: …`) als het project
  toch geladen is, of als het genoemde projectbestand niet bestaat. Elke andere failure blijft fataal.
- Waarom een ontbrekend project veilig is: er staat niets van op schijf om te herschrijven. Hangt een geladen project
  ervan af, dan resolven zijn typen niet en faalt de bestaande CS0246/CS0234-check alsnog (met de waarschuwingen erbij in de
  foutmelding). Een project dat wél bestaat maar niet laadt blijft fataal: de fix zou zijn code overslaan en toch
  gedeelde pakketbestanden wijzigen.
- Corpus (TAAK-090): Polly, EventFlow, SmartEnum en Bogus laden nu. Specification niet: het kernproject staat buiten de
  tarball, de andere projecten verwijzen ernaar (CS0246). EventFlow's eigen `dotnet restore` faalt op NU1902/NU1903 door
  TreatWarningsAsErrors; dat is een build van de gebruiker (`-p:NuGetAudit=false`), niet iets van de loader.

## Eigen assertion-extensies (TAAK-091)

- `fa.custom-assertion` (`Patterns/CustomAssertionPattern.cs`, nieuw bestand; één regel in `FluentAssertionsFix.Patterns`)
  herkent een niet-FA-methode op een FA-assertions-type (receiver of basistype in een `FluentAssertions`-namespace,
  naam eindigt op `Assertions`) en geeft Manual met één reden per extensie. Zie `rapportage.md` voor het groeperen.

## Projectbestanden (TAAK-017)

- `IFix.Package` (default `null`): `PackageMigration` (oud pakket = assembly-naam, `AddedPackages`: naam + versie; het eerste vervangt het oude in place, de rest komt ernaast). FA → Shouldly 4.3.0; MediatR → Mediator.Abstractions + Mediator.SourceGenerator 3.0.2.
- `Fixer` maakt per project dat de oude assembly refereert een `ProjectMigration`; `KeepRemovedPackage` als een `.cs`-bestand
  het pakket na de fix nog semantisch gebruikt (`ProjectPackages`). Alle projecten gaan in één `IProjectFileStore.EditAsync`,
  want `Directory.*.props` is gedeeld.
- Rapport: Fixed per gewijzigd projectbestand, Manual per project dat het pakket houdt of bij een lees-/XML-fout (Line 0,
  pattern `project-file`). `FixReport.ProjectFileChanges` bevat de nieuwe tekst; `FixCommand` toont de diff of geeft ze met de broncode aan `SaveAsync`.
- Implementatie: `XmlProjectFileStore` (DataAccess, `XDocument` + `IFileSystem`), behoudt witruimte, CRLF en BOM en weigert te
  schrijven als het bestand intussen veranderd is. Zie `docs/projectinfo/features/projectbestanden-migreren-plan.md`.
- Tekstuele splice (TAAK-134/111): `ProjectFile` loopt bij het laden de originele tekst naast het `XDocument` af en onthoudt per
  node waar hij staat. Bij schrijven komt elke ongewijzigde node letterlijk uit de originele tekst; alleen gewijzigde
  attribuutwaarden worden in place vervangen, nieuwe elementen op één regel geschreven en verwijderde weggeknipt (met hun
  inspringing). Zo blijven `/>`, `" >`, `->` in attributen, attributen over meerdere regels, comments, BOM en (gemengde)
  regeleinden byte-gelijk; de reader normaliseert geen regeleinden, dus gekopieerde inspringing houdt het regeleinde van de buur.
  Een bestand zonder wijziging (vaak `Directory.Build.props`) levert geen change op.
- Veiligheidsregel (TAAK-111): faalt `EditAsync` toch, dan zet de Fixer alle `.cs`-bestanden van de betrokken projecten terug
  naar de originele tekst en worden hun Fixed-findings Manual ("Not rewritten: Could not edit the project files: …"). Nooit
  herschreven code zonder het nieuwe pakket.

## Gelijknamige typen (TAAK-024)

- `IFix.SwappedTypes` (default leeg): typen als `MediatR.IRequest\`1`met een tweeling in de nieuwe namespace. Symbolen van
die typen en hun leden tellen niet als gebruik in`UsingDirectives.UsedNamespaces`en`ProjectPackages`.
- Een bestand zonder patroon-wijziging krijgt de using-swap toch als alleen swapped typen de oude using nodig hebben.
- `PackageMigration.AlsoRemovedPackages` (MediatR: `MediatR.Contracts`) wordt mee verwijderd. Zie
  `docs/projectinfo/features/gelijknamige-typen-swap-plan.md`.

## Inactieve `#if`-code en global usings (TAAK-043, TAAK-044)

- Code in een inactieve `#if`-tak is `DisabledTextTrivia`: patronen zien hem niet en niets bindt eraan. Heeft een gewijzigd
  bestand zulke trivia, dan blijft de oude using staan (de nieuwe komt erbij) en meldt de Fixer Manual (pattern `usings`,
  regel van de inactieve code). `ProjectPackages` houdt dan ook het pakket. Gekozen boven parsen per symbolenset: welke
  symbolen bestaan (`DEBUG`, eigen) is niet te weten, en alles tegelijk aanzetten parst `#else`-takken niet.
- `global using <oud>;` in een eigen bestand (niet `*.g.cs`; `<Using Include>` doet `XmlProjectFileStore`) geldt voor het
  hele project. Bestanden in zo'n project krijgen geen eigen `using <nieuw>;`. Na alle bestanden beslist
  `Fixer.SwapGlobalUsingsAsync` per projectbestand (alle TFM's): bindt geen bestand meer aan de oude namespace en is er geen
  inactieve code, dan wordt hij in place `global using <nieuw>;`; anders blijft hij met de nieuwe ernaast + Manual `usings`.
  Een overgebleven global using houdt het pakket in `ProjectPackages`.

## SOLID in het kort

| Principe | Hoe                                                                                                                    |
| -------- | ---------------------------------------------------------------------------------------------------------------------- |
| S        | Een pattern beschrijft alleen wat er herschreven wordt. Laden, schrijven en rapporteren gebeurt elders                 |
| O        | Een nieuwe bibliotheek of nieuw patroon is een nieuwe klasse. De Fixer verandert niet                                  |
| L        | Elk pattern geeft `Fixed` of `Manual` terug en gooit nooit een exception, dus de Fixer behandelt ze allemaal hetzelfde |
| I        | Kleine interfaces: zoeken/herschrijven, solution-opslag en rapport-schrijven zijn apart                                |
| D        | Core hangt af van interfaces. MSBuild, het bestandssysteem en CSV zitten in DataAccess                                 |

## Zie ook

- `docs/projectinfo/README.md` — overzicht en C4-diagrammen
- `claude/docs/design-patterns.md` — GoF-patronen in deze opzet
- `src/Unshackle.Core/Fixer.cs` — implementatie
