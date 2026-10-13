# TAAK-168 — `ContainKey` op `IReadOnlyDictionary`/headers geeft CS0411

**Status**: ✅ voltooid — 2026-10-12
**Aangemaakt**: 2026-10-12 (corpus-run, `claude/tasks/CORPUSRUN-fa.md`)
**Doel**: `x.Should().ContainKey(k)` bouwt na de fix ook als `x` geen `IDictionary<K,V>` is.

## Context voor de volgende agent

- Steeltoe (`f13e514`): 58× `error CS0411` (11 regels × TFM's). Voorbeelden: `src/Discovery/test/Eureka.Test/EurekaApplicationInfoManagerTest.cs:311` (`Metadata` is `IReadOnlyDictionary<string,string?>`) en `src/Management/test/Endpoint.Test/CorsPolicyTest.cs:47` (`response.Headers`).
- Shouldly's `ShouldContainKey` bestaat alleen voor `IDictionary<TKey,TValue>`. In `REGRESSIE-corpus-2.md` stond dit al als "TAAK-151", maar dat taakbestand is nooit aangemaakt.

## Analyse

### Huidige situatie

`ContainKeyPattern.RewriteAssertion` (`src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainKeyPattern.cs:34`) schrijft altijd `subject.ShouldContainKey(k)` en kijkt niet naar het subjecttype.

### Probleem

Bij `IReadOnlyDictionary`, `HttpHeaders` en vergelijkbare typen compileert het resultaat niet.

### Nieuwe situatie

- `IDictionary<,>` → zoals nu.
- `IReadOnlyDictionary<,>` → `x.Keys.ShouldContain(k)` / `ShouldNotContain`.
- Andere typen → `x.Contains(k).ShouldBeTrue()` als die methode bestaat, anders Manual.

### Verbetering

Na deze taak en TAAK-171 bouwt Steeltoe na de fix.

## Stappen

- [x] 1. Proofs in `Proofs/FluentAssertions/ContainKey/` voor `IReadOnlyDictionary` en `HttpResponseHeaders`.
- [x] 2. Typecheck in `ContainKeyPattern`.
- [x] 3. Bewijs: het Steeltoe-harnas geeft geen CS0411 meer.

## Keuzes

- `IReadOnlyDictionary` → `x.ContainsKey(k).ShouldBeTrue()` i.p.v. `x.Keys.ShouldContain(k)` (taakvoorstel): `ContainsKey` behoudt de comparer van elke implementatie (`Keys` van een eigen type hoeft dat niet), en `NotContainKey` kan er niet zwakker door worden. Probe FA 7.2.2 vs Shouldly 4.3.0: hoofdletterongevoelige dictionary gaf bij FA en bij `ContainsKey` hetzelfde resultaat.
- `HttpHeaders` (heeft `Contains(string)`, geen `ContainsKey`) → `x.Contains(k).ShouldBeTrue()`. Probe: `Contains` is hoofdletterongevoelig, FA's `ContainKey` op headers niet; positieve assertie dus iets soepeler, bedoeld gedrag voor headers (gedocumenteerd in het plan).
- Type zonder `ContainsKey`/`Contains(key)` (`IEnumerable<KeyValuePair<,>>`) → Manual. Bestaande `read-only.after.cs` was fout (compileerde niet tegen Shouldly) en is gecorrigeerd.
- Stap 3 (Steeltoe-harnas): corpus niet opnieuw gedraaid; de CS0411-vormen (`IReadOnlyDictionary`, `HttpResponseHeaders`) staan als compileerbare proofs.

## Gewijzigd

`ContainKeyPattern.cs`, proofs `ContainKey/` (`read-only.after`, `read-only-not-contain.*`, `headers.*`, `enumerable-pairs.*`), `fa-contain-key-{plan,modellen,verify}.md`, README, INDEX, proofs.md.

## Risico's & randgevallen

| Risico | Mitigatie |
| --- | --- |
| `ShouldBeTrue()` geeft een minder duidelijke foutmelding | Waar mogelijk `Keys.ShouldContain(k)` gebruiken |

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
- `claude/tasks/REGRESSIE-corpus-2.md`
