# TAAK-172 — `BeEquivalentTo` op een collectie met waarde-gelijke items → `ShouldBe(…, ignoreOrder: true)`

**Status**: ✅ voltooid — 2026-10-12
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

## Keuzes

- Het collectiegeval bestond al voor leaf-items (TAAK-092); het is uitgebreid naar `IsValueLike` (sealed record, record struct, struct met leaf-leden), dezelfde check als `ContainEquivalentOfPattern`.
- Alleen `StringCollectionAssertions` heeft een `params`-overload (`IEnumerable<int>.Should().BeEquivalentTo(1, 2)` compileert niet): de params-vorm is dus alleen strings. Wordt `new string[] { ... }` (zoals `fa.equal-sequence`, niet `new[]`: expliciet type werkt ook met `null`-items). Array-bouw verplaatst naar `ParamsArraySyntax`, gedeeld met `EqualSequencePattern`.
- Een array als argument is een gewone verwachting (normale vorm), geen params.
- Manual-redenen verbeterd: twee collecties met ander elementtype of niet-waarde-items kregen eerder "Different compile-time types".
- Probe (13 gevallen: herordenen, dubbele items, ander aantal, hoofdletters, null-items, leeg, null-subject, struct, record struct): FA en Shouldly gaven overal dezelfde uitkomst.
- Bestanden: `BeEquivalentToObjectPattern.cs`, `ParamsArraySyntax.cs` (nieuw), `EqualSequencePattern.cs`, proofs `BeEquivalentToObject/` (params-*, collection-record*, collection-struct, edge-collection-params nu Fixed), docs `fa-be-equivalent-to-collection-*`.

## Risico's & randgevallen

| Risico | Mitigatie |
| --- | --- |
| Strings: FA vergelijkt in BeEquivalentTo exact | Komt overeen met `ShouldBe`; vastleggen in een proof |
| Dubbele items | Beide tellen het aantal keer mee; vastleggen in een proof |

## Stappen

- [x] 1. Proofs: `List<string>`, params, `IEnumerable<int>`, record.
- [x] 2. Implementatie; de leaf-check delen met `ContainEquivalentOfPattern`.
- [x] 3. Bewijs: probe FA 7.2.2 vs Shouldly 4.3.0 (de corpusclones zijn verwijderd; harnas niet herdraaid).

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
