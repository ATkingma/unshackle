# TAAK-225 — `Equal(..)` op een dictionary → `ShouldBe(.., ignoreOrder: true)`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-13 (FA-corpusrun ronde 4)
**Doel**: de grootste Manual-groep omzetten die nog niet gerechtvaardigd is (buiten de Akka-conflicten).

## Context voor de volgende agent

- Ronde 4: 344× `fa.equal-sequence` "Equal(...) on a dictionary compares without order; check ShouldBe or ShouldBeEquivalentTo by hand" (Akka 337, LiteDB 5, NUKE 1, Polly 1).
- FA `GenericDictionaryAssertions.Equal` eist dezelfde sleutels en per sleutel een gelijke waarde (`Equals`); de volgorde telt niet. Shouldly `dict.ShouldBe(expected, ignoreOrder: true)` vergelijkt de `KeyValuePair`s met `Equals` en is dus gelijkwaardig voor waarde-items.
- Alleen omzetten als sleutel- en waardetype een betrouwbare `Equals` hebben (leaf/record/struct, dezelfde regel als TAAK-172); anders Manual laten. Let op het type van `expected` (dictionary of lijst van paren).

## Stappen

- [x] 1. Uitzoeken hoe Shouldly `ShouldBe(IEnumerable<KVP>, ignoreOrder)` zich gedraagt bij dubbele sleutels en `null`-waarden, vergeleken met FA.
- [x] 2. Rewrite in `fa.equal-sequence` + unittests + proofs; plan/verify bijwerken.
- [x] 3. Akka-herhaling: build groen, geen extra regressies.

## Keuzes

- Gesondeerd (FA 7.2.2 vs Shouldly 4.3.0, console-probe): volgorde, ontbrekende/extra sleutel, andere waarde, `null`-waarden, leeg, immutable/read-only subject, `double`, lijst-waarden: zelfde uitkomst. Enige verschil: subject én verwachting `null` (FA gooit, Shouldly slaagt). Custom key-comparer (OrdinalIgnoreCase): beide falen.
- Omzetting in `EqualSequencePattern` alleen als: methode op `GenericDictionaryAssertions`, verwachting is zelf een dictionary (`IDictionary<,>`/`IReadOnlyDictionary<,>`) met hetzelfde `KeyValuePair<K,V>`, en K en V `IsValueLike` (TAAK-172-regel; daarvoor `protected internal` gemaakt). Anders Manual met specifieke reden.
- `because` → derde argument (`ShouldBe(y, ignoreOrder: true, because)`).
- Noot: de probe laat zien dat FA hier ook voor klasse-waarden gewoon `Equals` gebruikt; verbreden naar alle niet-object/interface-typen is mogelijk maar bewust niet gedaan (opdracht: alleen waarde-items).

## Zie ook

- `claude/tasks/archief/TAAK-172-be-equivalent-to-collectie-ignore-order.md`
- `claude/tasks/CORPUSRUN-fa.md` (ronde 4)
- `claude/tasks/patroon-queue.md`
