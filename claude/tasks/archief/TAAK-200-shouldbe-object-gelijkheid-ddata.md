# TAAK-200 — `actual.Should().Be(expected)` op `object` → `ShouldBe` geeft andere uitkomst (Akka DData)

**Status**: ✅ voltooid (2026-10-13) — heropend en afgerond: Fixed met waarschuwing (besluit gebruiker)
**Aangemaakt**: 2026-10-13 (TAAK-174 stap 3, Akka-herhaling)
**Doel**: oorzaak vinden van de deterministische testregressie in `ReplicatorMessageSerializerSpec` en `fa.should-be` zo nodig aanpassen of Manual maken.

## Context voor de volgende agent

- Akka.NET (`23a8566`), `src/contrib/cluster/Akka.DistributedData.Tests/Serialization/ReplicatorMessageSerializerSpec.cs`: `ReplicatorMessageSerializer_should_serialize_Replicator_message` is groen in de baseline en faalt na de fix, ook los gedraaid (2×).
- Herschreven in `CheckSerialization<T>(T expected)`: `object actual = _serializer.FromBinary(…)`; `actual.Should().Be(expected)` → `actual.ShouldBe(expected)`. Melding: `actual should be Akka.DistributedData.Internal.DeltaPropagation (37787978) but was Akka.DistributedData.Internal.DeltaPropagation (29656116)`.
- `DeltaPropagation` heeft `IEquatable<DeltaPropagation>` + `Equals(object)`, vergelijkt `Deltas` (`ImmutableDictionary<string, Delta>`) per sleutel; `Delta : IEquatable<Delta>`.
- FA 7 `ObjectAssertions.Be(object)` gebruikt `Subject.IsSameOrEqualTo(expected)` (`actual.Equals(expected)`). Shouldly 4.3 `ShouldBe<T>` gaat via `Is.Equal` met een eigen `EqualityComparer<T>` (T = `object`); die volgt mogelijk een andere route (argumentvolgorde, `IEquatable`/`IComparable`-detectie, of iets in `Delta`/`DataEnvelope` dat als collectie wordt gezien).

## Stappen

- [x] 1. Reproduceren in een unittest/scratch: Shouldly `ShouldBe` vs `object.Equals` op twee gelijke `DeltaPropagation`'s; vaststellen welk pad in Shouldly afwijkt.
- [x] 2. Beslissen: rewrite aanpassen (bijv. `actual.Equals(expected).ShouldBeTrue()` is onleesbaar — liever Manual bij `object`-subject met een type dat alleen `Equals` overschrijft?) en overleggen met de gebruiker.
- [x] 3. (vervallen: geen gedragswijziging; risico gedocumenteerd in `fa-should-be-plan.md`/`-verify.md`) Unittest + proof.
- [x] 4. (test blijft rood; oorzaak in de doelcode, zie Keuzes) Bewijs: Akka-herhaling, test groen.

## Bevinding (probe in Akka DData-test, 2026-10-13)

Tijdelijk in `CheckSerialization` voor de `DeltaPropagation`-case:

```
a.Equals(e)=True  e.Equals(a)=False  object.Equals(a,e)=True  EqualityComparer<object>.Default=True
ShouldBe → faalt
Delta per sleutel: A, B, C symmetrisch; "DC" (ORMultiValueDictionaryDelta): a.Equals(e)=False, e.Equals(a)=True
```

- Echte oorzaak: **volgorde van `Equals`**. FA 7 `ObjectAssertions.Be` doet `actual.Equals(expected)`; Shouldly 4.3 `ShouldBe` doet `expected.Equals(actual)`. Geen collectie- of `IEquatable`-pad.
- De asymmetrie zit in Akka: `ORMultiValueDictionaryDelta.Equals(IDeltaOperation)` vergelijkt een `DeltaGroup` met één operatie anders dan andersom; na deserialisatie is de structuur anders (groep vs losse operatie). `DeltaPropagation.Equals` roept per sleutel `other.Delta.Equals(this.Delta)` aan, waardoor de richting nog eens omdraait.

## Keuzes

- **Geen rewrite-wijziging, geen Manual.** Statisch niet te zien of `Equals` symmetrisch is; Manual op elke `Be` met een type met eigen `Equals` (of `object`-subject) zou heel veel correcte rewrites weggooien. Argumenten omdraaien (`expected.ShouldBe(actual)`) maakt de foutmelding fout. De asymmetrische `Equals` is een bug in de doelcode die FA toevallig niet raakte.
- Vastgelegd als risico in `docs/projectinfo/patterns/fa-should-be-plan.md` (Risks) en `fa-should-be-verify.md` (rij TAAK-200). Gebruiker kan dit heroverwegen; geen overleg mogelijk vanuit deze agent.
## Besluit gebruiker (2026-10-13): waarschuwing in het rapport

- `ShouldBePattern.EqualsOrderWarning` (geldt ook voor `NotBePattern`): Fixed met `Warning` "Shouldly compares expected.Equals(actual), FluentAssertions actual.Equals(expected); check symmetry of X.Equals" als subject- of expected-type (of een basistype) buiten de BCL een eigen `Equals(x)` declareert; bij twee onbekende runtime-typen (`object`, typeparameter, interface) "the runtime types are unknown here …". Akka's `object actual` vs `T expected` valt onder de tweede regel.
- Geen waarschuwing: BCL-typen (assembly `System*`/`Microsoft*`/`mscorlib`/`netstandard`), records zonder eigen `Equals` (gegenereerd, symmetrisch), één kant concreet BCL-type tegenover `object`.
- Unittests `src/Unshackle.Tests/Core/ShouldBeEqualsOrderTests.cs`; proof `Proofs/FluentAssertions/ShouldBe/own-equals.*`.
- De Keuzes hierboven ("geen Manual") blijven; de test in Akka blijft rood, maar het rapport wijst er nu op.
## Zie ook

- `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`
- `claude/tasks/CORPUSRUN-fa.md`
