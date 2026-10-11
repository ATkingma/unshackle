# TAAK-017 — Projectbestanden migreren

**Status**: stap 8/8 — checks, commit door hoofdsessie
**Aangemaakt**: 2026-10-11
**Doel**: `unshackle fix` past ook `.csproj`, `Directory.Build.props` en `Directory.Packages.props` aan (FluentAssertions eruit, Shouldly 4.3.0 erin, `<Using>` omgezet), zodat de gefixte solution zonder handwerk bouwt.

## Context voor de volgende agent

- Keten uit TAAK-005: `Cli.cs` (composition root) → `ScanCommand.RunAsync` → `IFixer.FixAsync` → `FixReport`; `FixCommand` toont de
  diff (`LineDiff`) of schrijft via `ISolutionStore.SaveAsync` (`MsBuildSolutionStore`, alleen documenten).
- `samples/SampleShop/SampleShop.Tests/SampleShop.Tests.csproj` heeft `<PackageReference Include="FluentAssertions" />` (CPM, zonder
  versie) en `<Using Include="FluentAssertions" />`; `samples/Directory.Packages.props` heeft `<PackageVersion Include="FluentAssertions" Version="7.2.2" />`.
- Na `fix` blijft één Manual over (`RepositoryTests.cs:16`, `fa.have-count` op een dictionary): die regel gebruikt FA nog.
- Een andere agent werkt aan TAAK-018 in `src/Unshackle.Tests/` (samenwerking tussen patronen): diens bestanden niet aanraken;
  `ProofFix.cs` en `FixerTests.cs` daarom ook niet (contract zo uitbreiden dat ze ongewijzigd blijven compileren).
- `samples/` niet wijzigen; integratietest werkt op een kopie.

## Analyse

### Huidige situatie

`IFix` beschrijft alleen code (`Patterns`, `RemovedNamespaces`, `AddedNamespaces`). Niets leest of schrijft projectbestanden.
TAAK-005 noemde dit als vervolgtaak (risicotabel).

### Probleem

- Na `fix` staan FA-pakket en `<Using Include="FluentAssertions" />` er nog en ontbreekt Shouldly: de code bouwt niet.
- De integratietest kan de gefixte sample daardoor niet bouwen en testen; "fix werkt" is onbewezen.

### Nieuwe situatie

`fix` (en `scan`, `--dry-run`) wijzigt per project dat FA-assembly's refereert: Shouldly erbij (PackageReference, met `Version` of via
`PackageVersion` bij CPM), FA eruit en `<Using>` omgezet, maar alleen als geen `.cs`-bestand van dat project FA nog gebruikt; anders blijft
FA staan en komt er een Manual-rij op projectniveau. Rapport en diff tonen de projectbestanden (Line 0).

### Verbetering

De gefixte solution bouwt en de tests draaien zonder handwerk (op de Manual-findings na); bewezen met `dotnet build` + `dotnet test` in de integratietest.

## Aanpak

### Wat

1. Core: model `PackageMigration` (welk pakket weg, welk erbij, versie) als default-member `IFix.Package` (null = niets aan projectbestanden).
   Model `ProjectMigration` (project, pakket, namespaces, `KeepRemovedPackage`) en `ProjectFileChange` (pad, oude en nieuwe tekst).
   Interface `IProjectFileStore` (`EditAsync` → wijzigingen zonder schrijven, `SaveAsync`). `FixReport.ProjectFileChanges`.
2. Core: `Fixer` bepaalt per project of FA nog gebruikt wordt (semantisch, zoals `UsingDirectives`), vraagt de wijzigingen op en zet
   rijen in de findings (Fixed per gewijzigd bestand, Manual per project dat FA houdt).
3. DataAccess: `XmlProjectFileStore` (XDocument, `IFileSystem`) voor csproj, dichtstbijzijnde `Directory.Build.props` en `Directory.Packages.props`.
4. Interface: `Cli` geeft de store aan `Fixer` en `FixCommand`; `FixCommand` toont de diff en schrijft de projectbestanden.

### Waar

`src/Unshackle.Core/Abstractions/{IFix,IProjectFileStore}.cs`, `src/Unshackle.Core/Models/{PackageMigration,ProjectMigration,ProjectFileChange,FixReport}.cs`,
`src/Unshackle.Core/{Fixer,ProjectPackages}.cs`, `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`,
`src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs`, `src/Unshackle.Interface/{Cli.cs,Commands/FixCommand.cs}`,
tests in `src/Unshackle.Tests/{DataAccess/XmlProjectFileStoreTests.cs,Core/FixerProjectFileTests.cs,Interface/Commands/CommandTests.cs,Integration/SampleShopTests.cs}`.

### Hoe

KingmaDoc-plan eerst; dan DataAccess-store met `MockFileSystem`-tests; dan Core (Fixer + modellen) met NSubstitute-mock van
`IProjectFileStore`; dan Interface; als laatste de integratietest (fix → handwerk Manual → fix → build + test op de kopie).

### Waarom deze aanpak

Beslissen "FA nog gebruikt?" vraagt het SemanticModel → Core. XML lezen/schrijven is I/O → DataAccess achter een Core-interface.
Eén batch-aanroep (`EditAsync` met alle projecten) omdat `Directory.*.props` gedeeld is: FA mag daar alleen weg als geen enkel project het houdt.

## Stappen

- [x] 1. KingmaDoc-plan `docs/projectinfo/features/projectbestanden-migreren-plan.md`, check + approve
- [x] 2. Core-contract: modellen, `IProjectFileStore`, `IFix.Package`, `FluentAssertionsFix.Package`
- [x] 3. `XmlProjectFileStore` + unittests (CPM, niet-CPM, Using, props, FA blijft)
- [x] 4. `Fixer`: projecten bepalen, store aanroepen, findings + unittests met NSubstitute
- [x] 5. `Cli` + `FixCommand` (diff en schrijven) + command-tests
- [x] 6. Integratietest: fix op kopie SampleShop, `dotnet build` + `dotnet test`, geen FA in csproj/props
- [x] 7. KingmaDoc-verify, `core.md`, `modellen/klassen.md` (render), README Documenten, INDEX
- [x] 8. Drie checks groen (`claude/docs/agent-werkwijze.md` §6); commit/push door de hoofdsessie

## Risico's & randgevallen

| Risico                                                        | Mitigatie                                                                                                |
| ------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| FA weghalen terwijl code het nog gebruikt → build kapot       | Alleen weg als geen `.cs` in het project FA-symbolen bindt; anders Manual op projectniveau               |
| Gedeelde `Directory.*.props`: één project houdt FA            | FA blijft in een gedeeld bestand als één project in de batch het houdt                                   |
| Opmaak/commentaar van XML gaat verloren                       | `XDocument` met `PreserveWhitespace`; tekst buiten `<Project>` letterlijk behouden; CRLF en BOM behouden |
| Projecten buiten de solution delen `Directory.Packages.props` | Bekende grens, in Keuzes; alleen projecten in de solution tellen                                         |
| Bestand gewijzigd tussen lezen en schrijven                   | `SaveAsync` vergelijkt de huidige tekst met `OldText` en weigert bij verschil                            |

## Gewijzigde bestanden

Zie `files_expected` in het plan; extra `src/Unshackle.DataAccess/Projects/ProjectFile.cs`. Docs: `projectbestanden-migreren-verify.md`, `architectuur/core.md`, `modellen/klassen.md` (+ `img/klassen-2.*`), README, INDEX.

## Keuzes

- Store-tests in `DataAccess/Projects/` (zoals `Reports/`).
- Integratietest slaat `obj/` over bij de FA-check: restore-bestanden van vóór de fix.
- Projecten buiten de solution die `Directory.Packages.props` delen tellen niet mee (bekende grens).

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `claude/tasks/archief/TAAK-005-end-to-end-keten.md` — CLI, `MsBuildSolutionStore`, vervolgtaak projectbestanden
