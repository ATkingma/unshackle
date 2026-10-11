# TAAK-110 — Niet-herkende `.Should()` mag `using FluentAssertions` niet laten verwijderen

**Status**: open
**Aangemaakt**: 2026-10-11 (corpus-run, `claude/tasks/CORPUSRUN-fa.md` volgt)

## Context

CSharpFunctionalExtensions (corpus-run 2026-10-11): `maybe2.HasValue.Should().BeTrue()` (MaybeTests/Extensions/BindTests.cs r54) krijgt **geen** finding, maar na `fix` staat alleen nog `using Xunit;` in het bestand → 42× CS1061 (`'bool'`/`'TestBase.T'` heeft geen `Should`). 4481 Fixed / 49 Manual; build na fix faalt.
Vermoeden: subject/aanroep bindt niet (type via lambda-inferentie van `maybe2`), dus `UsedRemovedNamespaces` ziet FluentAssertions niet als gebruikt en de using gaat weg.

## Stappen

1. Minimale proof/unittest: `.Should()` op een member van een local waarvan het type uit een lambda volgt (ook: niet-gebonden `Should`-aanroep, error-type subject).
2. Root cause in `UsingDirectives`/`Fixer`: een overgebleven `Should()`-aanroep (syntactisch, naam `Should` op member-access) of een niet-gebonden symbool in een bestand met een verwijderde using → using **houden** + Manual-finding ("assertion not recognised; using kept").
3. Waarom het patroon de regel niet herkende: aparte bevinding (kan een `fa.be-true-false`-gat zijn).
4. Bewijs: corpus-kopie CSFE opnieuw `fix` + build groen.

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
