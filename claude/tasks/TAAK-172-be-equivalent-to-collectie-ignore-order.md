# TAAK-172 — `BeEquivalentTo` op een collectie met waarde-gelijke items → `ShouldBe(…, ignoreOrder: true)`

**Status**: stap 1/3 — nog te starten
**Aangemaakt**: 2026-10-12 (corpus-run, `claude/tasks/CORPUSRUN-fa.md`)
**Doel**: 207 Manuals met de reden "FluentAssertions ignores the item order by default, Shouldly does not", verspreid over 13 projecten, worden Fixed als de items waarde-gelijkheid hebben.

## Context voor de volgende agent

- Voorbeelden: FluentResults `src/FluentResults.Test/MergeTests.cs:70` (`List<string>` vs `new[] { "A", … }`) en CSFE `ResultTests/Extensions/CombineInOrderTests.cs:80` (`IEnumerable<string>`).
- Verdeling: FluentResults 53, LiteDB 50, Steeltoe 30, CSFE 28, Spec 15, EventFlow 9, PowerApps 8, de rest ≤ 4.
- De reden komt uit `BeEquivalentToObjectPattern.cs:33` (params) en `:96` (collectie).

## Nieuwe situatie

- Itemtype is string, primitief, enum, of een record/struct met alleen zulke leden (dezelfde leaf-check als `ContainEquivalentOfPattern`) → `x.ShouldBe(expected, ignoreOrder: true)`.
- Params-vorm → `new[] { … }`, met behoud van opmaak (zie TAAK-153).
- In alle andere gevallen blijft het Manual.

## Risico's & randgevallen

| Risico | Mitigatie |
| --- | --- |
| Strings: FA vergelijkt in BeEquivalentTo exact | Komt overeen met `ShouldBe`; vastleggen in een proof |
| Dubbele items | Beide tellen het aantal keer mee; vastleggen in een proof |

## Stappen

- [ ] 1. Proofs: `List<string>`, params, `IEnumerable<int>`, record.
- [ ] 2. Implementatie; de leaf-check delen met `ContainEquivalentOfPattern`.
- [ ] 3. Bewijs: FluentResults- en CSFE-harnas, 0 regressies.

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
