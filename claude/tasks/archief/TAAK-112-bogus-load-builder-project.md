# TAAK-112 — Bogus laadt niet in Unshackle terwijl `dotnet build` slaagt

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11 (corpus-run, `claude/tasks/CORPUSRUN-fa.md` volgt)

## Context

Bogus: `fix` stopt met "The solution could not be loaded (6 errors)": 3× "Builder: The type or namespace name 'System' could not be found" en NU1701 (Rant 3.0.530, Z.ExtensionMethods.WithTwoNamespace, net4x-pakketten in net9.0-project) in Bogus.Tests.csproj. TAAK-090 vangt NU1701 als warning, maar deze meldingen komen als MSBuild-failure met een andere tekst/vorm binnen.

## Stappen

1. Reproduceer op de Bogus-kopie (scratchpad/run-fa/src/bchavez_Bogus).
2. NU1701 in deze vorm ('Msbuild failed when processing … Package … was restored using …') als waarschuwing behandelen.
3. 'Builder': waarschijnlijk een build-project (NUKE e.d.) dat niet gerestored is of geen test/FA bevat — onderzoeken; projecten zonder FA-referentie die niet laden mogen een waarschuwing geven i.p.v. fataal (motiveren, zie TAAK-090-regels).
4. Bewijs: Bogus laadt, fix + build groen.

## Resultaat

- NU1701 in de vorm "Msbuild failed when processing … was restored using …" was al een waarschuwing (`IsTolerable`: project geladen).
  Fataal was alleen de CS0246-check: 'Builder' (NUKE) wordt bij `dotnet restore Bogus.sln` niet gerestored (geen
  `project.assets.json`) en de design-time build vindt niet eens `System`.
- Fix (`src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs`): een project waarin `System.Object` niet resolvet, terwijl
  andere projecten wel resolven, en waar geen project van afhangt, wordt uit de solution gelaten met een waarschuwing. Een enkel
  ongerestored project of een project waar anderen van afhangen blijft fataal (de bestaande CS0246-check).
- Test: `SampleShopTests.Load_LeafProjectWithoutSystemNextToRestoredOne_LeavesItOutWithWarning` (nagebootst met
  `DisableImplicitFrameworkReferences`; een ongerestored project met packages vindt in de test wel System).
- Corpus: verse kopie Bogus (`scratchpad/v110/work/bogus`): laadt, 865 Fixed / 113 Manual, **build groen**.

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
