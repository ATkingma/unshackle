# Testen

## Proofs: elk patroon testen zonder nieuwe testcode

```
src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/
   simple.before.cs      simple.after.cs
   dictionary.before.cs  dictionary.manual.txt    ← verwacht: handwerk
```

Eén test (`ProofTests`) vindt alle proofs zelf en draait de Fixer erop. Een nieuw patroon of
afwijkend geval testen betekent twee bestanden toevoegen. De map `Proofs/` wordt niet
meegecompileerd: het is testdata.

**Veiligheidscheck in elke proof:** de compilatie van `before` moet foutloos zijn met de oude
bibliotheek, en die van `after` met de nieuwe. Zo vang je een vergeten referentie af (anders
geeft het `SemanticModel` stilletjes error types terug) en een rewrite die niet compileert.

## Combined proofs: patronen samen

```
src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/<case>.before.cs + <case>.after.cs
```

`ProofTests` draait één pattern per proof; dat laat samenwerking onbewezen (bv. `fa.all-satisfy` kopieert zijn
lambda-body in een `foreach` en de latere patterns herschrijven die body; `.And`-splitsing naast andere patterns;
usings). `CombinedProofTests` draait de **echte** `FluentAssertionsFix` (alle patterns, echte volgorde) en vergelijkt
de eindtekst exact met `.after.cs`. Zelfde compileercheck: before tegen FluentAssertions, after tegen Shouldly (bevat
de after nog `using FluentAssertions;` door een Manual-restant, dan Shouldly + FluentAssertions). `ProofTests` slaat
de map `_Combined` over. Een aparte test legt vast dat `AllSatisfyPattern` eerst staat. Nieuw geval = twee bestanden.

## Pending proofs: bekende fouten

`src/Unshackle.Tests/Proofs/_Pending/<Fix>/<Pattern>/<case>.*` bevat proofs van bekende patroonbugs, met de gewenste
`.after.cs`. `ProofTests` slaat de map over; elke pending proof hoort bij een taak. Fix klaar → map terugzetten.
Herkomst van de edge cases: `architectuur/edge-cases.md`; frequenties en fix-runs: `architectuur/corpus.md`.

## Types zonder te builden

Unittests gebruiken een `Solution` in het geheugen (`AdhocWorkspace`). De types komen uit:

1. **Echte assemblies** (standaard). Het testproject verwijst naar de laatste vrije versies
   (FluentAssertions 7.2.2, MediatR 12.5.0, AutoMapper 14.0.0, plus Shouldly en Mediator.Abstractions).
   .NET zelf komt uit `Basic.Reference.Assemblies.Net100`. Een helper `TestReferences` geeft per
   fix de juiste set `MetadataReference`s.
2. **Stubs**: alleen voor uitzonderingen die je niet met het echte pakket kunt namaken.
3. **Echte sample-projecten** in `samples/` (nu: `samples/SampleShop`, een webshop met FluentAssertions 7.2.2,
   MediatR 12.5.0 en AutoMapper 14.0.0; elk FA-patroon heeft een commentaar met de verwachte rewrite). Samples
   staan los van onze build: eigen `Directory.Build.props`, `Directory.Packages.props` en `.editorconfig` (root).
   `Unshackle.Tests/Integration/` laadt ze met MSBuild, fixt, bouwt en test ze. Traag, dus weinig, en apart te draaien via een trait/filter.

## Mocking (NSubstitute)

Mock alleen je **eigen** interfaces, nooit Roslyn-typen. Voor Roslyn gebruik je een echte
in-memory solution.

| Wat test je     | Hoe                                                             |
| --------------- | --------------------------------------------------------------- |
| Een pattern     | Proofs met echte assemblies                                     |
| De Fixer-logica | Gemockte `IFix` en `IPattern`, kleine in-memory solution        |
| Commands        | Gemockte `IFixer`, `IReportWriter` en `IConsoleView`            |
| Report writers  | `MockFileSystem` (System.IO.Abstractions), geen echte bestanden |
| End-to-end      | `samples/` in de integratietests                                |

Moq gebruiken we bewust niet (SponsorLink-ophef in 2023, en het is zelf een migratiekandidaat).

## Zie ook

- `docs/projectinfo/architectuur/proofs.md` — overzicht van alle proofs (genereren: `python3 claude/tools/proofs-doc.py`)
- `docs/projectinfo/README.md` — overzicht en C4-diagrammen
- `src/Unshackle.Tests/ProofTests.cs` — proof-runner
- `src/Unshackle.Tests/CombinedProofTests.cs` — runner voor samenwerking van patterns
