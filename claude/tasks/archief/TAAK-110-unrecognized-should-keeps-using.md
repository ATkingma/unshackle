# TAAK-110 — Niet-herkende `.Should()` mag `using FluentAssertions` niet laten verwijderen

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11 (corpus-run, `claude/tasks/CORPUSRUN-fa.md` volgt)

## Context

CSharpFunctionalExtensions (corpus-run 2026-10-11): `maybe2.HasValue.Should().BeTrue()` (MaybeTests/Extensions/BindTests.cs r54) krijgt **geen** finding, maar na `fix` staat alleen nog `using Xunit;` in het bestand → 42× CS1061 (`'bool'`/`'TestBase.T'` heeft geen `Should`). 4481 Fixed / 49 Manual; build na fix faalt.
Vermoeden: subject/aanroep bindt niet (type via lambda-inferentie van `maybe2`), dus `UsedRemovedNamespaces` ziet FluentAssertions niet als gebruikt en de using gaat weg.

## Stappen

1. Minimale proof/unittest: `.Should()` op een member van een local waarvan het type uit een lambda volgt (ook: niet-gebonden `Should`-aanroep, error-type subject).
2. Root cause in `UsingDirectives`/`Fixer`: een overgebleven `Should()`-aanroep (syntactisch, naam `Should` op member-access) of een niet-gebonden symbool in een bestand met een verwijderde using → using **houden** + Manual-finding ("assertion not recognised; using kept").
3. Waarom het patroon de regel niet herkende: aparte bevinding (kan een `fa.be-true-false`-gat zijn).
4. Bewijs: corpus-kopie CSFE opnieuw `fix` + build groen.

## Resultaat

- Oorzaak r54 (BindTests.cs): `fa.should-be` herschrijft eerst `ctx.Should().Be(context)` in het lambda-argument van `maybe.Bind(...)`.
  In het geladen model is Shouldly nog niet gerefereerd, dus `ctx.ShouldBe` bindt niet; daardoor kiest de overload-resolutie van
  `Bind` (Maybe/Task/ValueTask-varianten) niets en is `maybe2` een fouttype. `fa.be-true-false` ziet `maybe2.HasValue.Should()` dan
  niet meer. Geen gat in het patroon; de opeenvolgende patronen op het tussenresultaat zijn de oorzaak.
- Fix: `IFix.RemovedExtensionMethods` (FA: `Should`). `UsingDirectives.UnboundCalls` vindt `x.Should()` die nergens aan bindt;
  dan blijft de oude using (`UpdateAsync`), blijft het pakket (`ProjectPackages`) en komt er een Manual-finding `usings`
  ("Run the fix again once the project builds"). Een tweede run herschrijft de rest.
- Proof: `src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/unbound-should.*` (Maybe/Bind-nabootsing; sinds TAAK-116 `lambda-overload-one-run.*`, in één run Fixed).
- Corpus: verse kopie CSFE (`scratchpad/v110/work/csfe`): fix 4481 Fixed / 70 Manual (21 `usings`), **build groen** (was 42x CS1061).

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
