# TAAK-113 — `fa.equal-sequence` wikkelt collection expression in een array

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11 (corpus-run, `claude/tasks/CORPUSRUN-fa.md` volgt)

## Context

PowerApps-Tooling (42× CS0029, 12× CS1729, 12× CS1061 na fix): `x.Should().Equal([ "a", "b" ])` (collection expression als enig argument) wordt `x.ShouldBe(new string[] { [ "a", "b" ] })` — het patroon behandelt de collection expression als params-element.

## Stappen

1. Proof `EqualSequence/collection-expression` (before compileert tegen FA 7.2.2).
2. Eén argument dat zelf een collectie is (collection expression, array, IEnumerable) → doorgeven; alleen echte params-losse elementen in `new T[] { … }` wikkelen. Collection expression heeft een target type nodig: `ShouldBe((string[])[...])` of `new string[] { "a", "b" }` — kies wat compileert en bewijs.
3. Bewijs: PowerApps-kopie opnieuw `fix` + build groen.

## Resultaat

- `EqualSequencePattern`: een collection expression als enig argument levert de elementen: `Equal(["a", "b"])` →
  `ShouldBe(new string[] { "a", "b" })`. Met een spread (`[.. t, "b"]`) → Manual.
- Proofs: `EqualSequence/collection-expression.*`, `EqualSequence/collection-expression-spread.*`.
- Corpus: **niet herbewezen**. Er is geen schone PowerApps-kopie (`run-fa/src` en `casus/` zijn al gefixt; terugzetten vraagt
  versiebeheer, dat in deze sessie buiten de worktree geweigerd wordt). Opnieuw draaien op een verse clone.

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
