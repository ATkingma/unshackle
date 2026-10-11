# TAAK-116 — Eén `fix`-run volstaat bij een herschreven lambda-argument

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11 (vervolg op TAAK-110)

## Context

Na TAAK-110 had CSharpFunctionalExtensions twee runs nodig. `fa.should-be` herschrijft `ctx.Should().Be(context)` in het
lambda-argument van `maybe.Bind(...)` naar `ctx.ShouldBe(context)`. Shouldly zat niet in het geladen model, dus dat bond niet,
de overload-resolutie van `Bind` faalde, `maybe2` werd een fouttype en `fa.be-true-false` zag `maybe2.HasValue.Should().BeTrue()`
niet: 21 Manuals `usings` ("run the fix again").

## Keuze

- (a) gekozen: Shouldly als metadata-referentie in de werkkopie. Klein, en lost het in de bron op.
- (b) herkenning op de originele compilation: patronen draaien bewust na elkaar op het tussenresultaat (geneste findings van
  verschillende patronen, AndChain, AssertionScope); alles op het origineel zou die neststructuur breken. De per-bestand-werkkopie
  van TAAK-130 helpt hier niet: het probleem zit binnen één bestand.
- (c) fixpoint zonder (a) helpt niet: `ShouldBe` blijft onbekend.

## Resultaat

- `IFix.AddedReferences` (default leeg). `FluentAssertionsFix(IReadOnlyList<MetadataReference> shouldly)`; de parameterloze
  constructor blijft (geen referenties, oud gedrag).
- `Fixer.AddReferences`: per C#-project de ontbrekende referenties (op bestandsnaam) plus document `Unshackle.AddedUsings.g.cs`
  met `global using Shouldly;` in de `baseline` waarop de patronen draaien. Het resultaat bouwt op `original`, dus de projecten
  krijgen niets extra. `UsingDirectives.GlobalUsings` negeert dat document (anders kreeg een bestand geen `using Shouldly;`).
- Composition root (`Cli`): `References/Shouldly.dll` naast de tool, de **netstandard2.0**-build uit het pakket
  (`GeneratePathProperty`, `ExcludeAssets="all"`). De net9.0-build gaf CS1705 in een net8.0-project en loste dan niets op.
- Proof: `_Combined/unbound-should.*` → `_Combined/lambda-overload-one-run.*`, nu volledig Fixed (`ShouldBeTrue`, geen FA-using).
  `CombinedProofTests` gebruikt `new FluentAssertionsFix(TestReferences.ShouldlyAssembly)` zoals de CLI.
- Corpus: verse kopie CSFE uit `scratchpad/corpus/tgz/vkhorikov_CSharpFunctionalExtensions.tgz` (`scratchpad/t116/run4`):
  één fix-run 4502 Fixed / 49 Manual, **0 `usings`** (was 4481/70 met 21 `usings`), build groen.

## Zie ook

- `claude/tasks/archief/TAAK-110-unrecognized-should-keeps-using.md`
- `docs/projectinfo/architectuur/core.md`
