# Patronen: FluentAssertions naar Shouldly

Overzicht van de 31 FluentAssertions-patterns, in 6 groepen (een figuur per groep). Elk pattern heeft een plan, modellen en verify in [../patterns/](../patterns/); de details en alle Manual-gevallen staan daar. Alle assertion-patterns doorlopen eerst de gedeelde controles van [pattern-beslissing.md](pattern-beslissing.md). Uitvoervolgorde: zie [Volgorde](#volgorde).

## Waarden, booleans, null, leeg, identiteit

**Figure 1.** 6 patterns; Manual-gevallen in de tabel.

<!-- kingmadoc:diagram img/patronen-1.d2 -->
![Waarden, booleans, null, leeg, identiteit](img/patronen-1.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Extra Manual | Docs |
| --- | --- | --- | --- | --- | --- |
| `fa.should-be` | `ShouldBePattern` | `Be(y)` | `ShouldBe(y)` | Xml/Types-overloads, comparer, niet te casten type | [plan](../patterns/fa-should-be-plan.md), [modellen](../patterns/fa-should-be-modellen.md) |
| `fa.not-be` | `NotBePattern` | `NotBe(y)` | `ShouldNotBe(y)` | zoals `fa.should-be` | [plan](../patterns/fa-not-be-plan.md), [modellen](../patterns/fa-not-be-modellen.md) |
| `fa.be-true-false` | `BeTrueFalsePattern` | `BeTrue(), BeFalse()` | `ShouldBeTrue(), ShouldBeFalse(); bool?: ShouldBe(true)` | ander type dan bool/bool? | [plan](../patterns/fa-be-true-false-plan.md), [modellen](../patterns/fa-be-true-false-modellen.md) |
| `fa.be-null` | `BeNullPattern` | `BeNull(), NotBeNull()` | `ShouldBeNull(), ShouldNotBeNull()` | alleen de gedeelde gevallen | [plan](../patterns/fa-be-null-plan.md), [modellen](../patterns/fa-be-null-modellen.md) |
| `fa.be-empty` | `BeEmptyPattern` | `BeEmpty(), NotBeEmpty()` | `ShouldBeEmpty(), ShouldNotBeEmpty()` | alleen de gedeelde gevallen | [plan](../patterns/fa-be-empty-plan.md), [modellen](../patterns/fa-be-empty-modellen.md) |
| `fa.be-same-as` | `BeSameAsPattern` | `BeSameAs(y), NotBeSameAs(y)` | `ShouldBeSameAs(y), ShouldNotBeSameAs(y)` | alleen de gedeelde gevallen | [plan](../patterns/fa-be-same-as-plan.md), [modellen](../patterns/fa-be-same-as-modellen.md) |

## Getallen, datums, tellen, strings

**Figure 2.** 6 patterns; Manual-gevallen in de tabel.

<!-- kingmadoc:diagram img/patronen-2.d2 -->
![Getallen, datums, tellen, strings](img/patronen-2.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Extra Manual | Docs |
| --- | --- | --- | --- | --- | --- |
| `fa.be-greater-less` | `BeGreaterLessPattern` | `BeGreaterThan, BeLessThan, BeInRange, BePositive, BeAfter, ...` | `ShouldBeGreaterThan, ShouldBeInRange, ShouldBePositive, ...` | nullable subject, niet-vergelijkbaar type | [plan](../patterns/fa-be-greater-less-plan.md), [modellen](../patterns/fa-be-greater-less-modellen.md) |
| `fa.be-approximately` | `BeApproximatelyPattern` | `BeApproximately(y, tol), BeCloseTo(t, span), Not...` | `ShouldBe(y, tol); Not: ShouldNotBe of Math.Abs(x - y).ShouldBeGreaterThan(tol)` | zie plan | [plan](../patterns/fa-be-approximately-plan.md), [modellen](../patterns/fa-be-approximately-modellen.md) |
| `fa.have-count` | `HaveCountPattern` | `HaveCount(n), HaveCountGreaterThan(n), ...` | `.Length/.Count/.Count() plus ShouldBe(n) / ShouldBeGreaterThan(n)` | predicate, dictionary, ?., niet-generiek IEnumerable | [plan](../patterns/fa-have-count-plan.md), [modellen](../patterns/fa-have-count-modellen.md) |
| `fa.have-length` | `HaveLengthPattern` | `HaveLength(n)` | `s.Length.ShouldBe(n)` | zie plan | [plan](../patterns/fa-have-length-plan.md), [modellen](../patterns/fa-have-length-modellen.md) |
| `fa.string-start-end` | `StringStartEndPattern` | `StartWith, EndWith, NotBeNullOrEmpty, ...` | `ShouldStartWith(x, Case.Sensitive), ShouldNotBeNullOrEmpty(), ...` | zie plan | [plan](../patterns/fa-string-start-end-plan.md), [modellen](../patterns/fa-string-start-end-modellen.md) |
| `fa.match` | `MatchPattern` | `MatchRegex(r), Match("SO-*"), Not...` | `ShouldMatch(r), ShouldMatch(@"(?s)\ASO-.*$")` | geen niet-lege stringconstante, subject geen string | [plan](../patterns/fa-match-plan.md), [modellen](../patterns/fa-match-modellen.md) |

## Collecties en dictionaries

**Figure 3.** 7 patterns; Manual-gevallen in de tabel.

<!-- kingmadoc:diagram img/patronen-3.d2 -->
![Collecties en dictionaries](img/patronen-3.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Extra Manual | Docs |
| --- | --- | --- | --- | --- | --- |
| `fa.contain` | `ContainPattern` | `Contain(item of predicate), NotContain` | `ShouldContain, ShouldNotContain (string: Case.Sensitive)` | dictionary, meerdere items, occurrence-constraint | [plan](../patterns/fa-contain-plan.md), [modellen](../patterns/fa-contain-modellen.md) |
| `fa.contain-key` | `ContainKeyPattern` | `ContainKey(k), NotContainKey(k)` | `ShouldContainKey(k), ShouldNotContainKey(k)` | ContainKeys, ContainValue(s), .WhoseValue | [plan](../patterns/fa-contain-key-plan.md), [modellen](../patterns/fa-contain-key-modellen.md) |
| `fa.contain-single-which` | `ContainSinglePattern` | `ContainSingle(), ContainSingle(p)[.Which]` | `ShouldHaveSingleItem(), ShouldContain(p, 1), x.Where(p).ShouldHaveSingleItem()` | zie plan | [plan](../patterns/fa-contain-single-which-plan.md), [modellen](../patterns/fa-contain-single-which-modellen.md) |
| `fa.only-contain` | `OnlyContainPattern` | `OnlyContain(pred)` | `ShouldNotBeEmpty(); ShouldAllBe(pred)` | niet direct in een blok (subject met bijwerkingen wordt gehoist) | [plan](../patterns/fa-only-contain-plan.md), [modellen](../patterns/fa-only-contain-modellen.md) |
| `fa.all-satisfy` | `AllSatisfyPattern` | `AllSatisfy(a => { ... })` | `ShouldNotBeEmpty(); foreach (var a in x) { ... }` | because, methodgroep, async, return, niet in blok | [plan](../patterns/fa-all-satisfy-plan.md), [modellen](../patterns/fa-all-satisfy-modellen.md) |
| `fa.equal-sequence` | `EqualSequencePattern` | `Equal(seq), Equal(1, 2), NotEqual` | `ShouldBe(seq), ShouldBe(new[] { 1, 2 }) (volgorde telt)` | zie plan | [plan](../patterns/fa-equal-sequence-plan.md), [modellen](../patterns/fa-equal-sequence-modellen.md) |
| `fa.direct-mapping` | `DirectMappingPattern` | `BeOneOf, BeSubsetOf, OnlyHaveUniqueItems, HaveValue` | `ShouldBeOneOf, ShouldBeSubsetOf, ShouldBeUnique, ShouldNotBeNull` | ContainInOrder: Manual met voorbeeldcode | [plan](../patterns/fa-direct-mapping-plan.md), [modellen](../patterns/fa-direct-mapping-modellen.md) |

## Exceptions

**Figure 4.** 6 patterns; Manual-gevallen in de tabel.

<!-- kingmadoc:diagram img/patronen-4.d2 -->
![Exceptions](img/patronen-4.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Extra Manual | Docs |
| --- | --- | --- | --- | --- | --- |
| `fa.throw` | `ThrowPattern` | `act.Should().Throw<T>()[.WithMessage(p)][.Which]` | `Should.Throw<T>(act)[.Message.ShouldBe/ShouldContain/...]` | wildcard in het midden of niet-constant, AggregateException-risico, .Where/.And | [plan](../patterns/fa-throw-plan.md), [modellen](../patterns/fa-throw-modellen.md) |
| `fa.throw-async` | `ThrowAsyncPattern` | `await act.Should().ThrowAsync<T>()[.WithMessage(p)]` | `await Should.ThrowAsync<T>(act)` | niet direct awaited, .Which, Func<Task<waardetype>> | [plan](../patterns/fa-throw-async-plan.md), [modellen](../patterns/fa-throw-async-modellen.md) |
| `fa.throw-exactly` | `ThrowExactlyPattern` | `ThrowExactly<T>(), ThrowExactlyAsync<T>()` | `Should.Throw<T>(act).ShouldBeOfType<T>()` | zoals `fa.throw` | [plan](../patterns/fa-throw-exactly-plan.md), [modellen](../patterns/fa-throw-exactly-modellen.md) |
| `fa.invoking` | `InvokingPattern` | `x.Invoking(s => s.M()).Should().Throw<T>() / Awaiting / NotThrow` | `Should.Throw<T>(() => x.M()) e.d.` | receiver die niet stabiel is | [plan](../patterns/fa-invoking-plan.md), [modellen](../patterns/fa-invoking-modellen.md) |
| `fa.not-throw` | `NotThrowPattern` | `act.Should().NotThrow()` | `Should.NotThrow(act)` | NotThrow<T>(), NotThrowAfter, resultaat gebruikt | [plan](../patterns/fa-not-throw-plan.md), [modellen](../patterns/fa-not-throw-modellen.md) |
| `fa.not-throw-async` | `NotThrowAsyncPattern` | `await act.Should().NotThrowAsync()` | `await Should.NotThrowAsync(act)` | niet direct awaited, resultaat gebruikt | [plan](../patterns/fa-not-throw-async-plan.md), [modellen](../patterns/fa-not-throw-async-modellen.md) |

## Typen en equivalentie

**Figure 5.** 3 patterns; Manual-gevallen in de tabel.

<!-- kingmadoc:diagram img/patronen-5.d2 -->
![Typen en equivalentie](img/patronen-5.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Extra Manual | Docs |
| --- | --- | --- | --- | --- | --- |
| `fa.be-of-type` | `BeOfTypePattern` | `BeOfType<T>()[.Which], BeAssignableTo<T>(), NotBeOfType<T>()` | `ShouldBeOfType<T>(), ShouldBeAssignableTo<T>(), ...` | zie plan | [plan](../patterns/fa-be-of-type-plan.md), [modellen](../patterns/fa-be-of-type-modellen.md) |
| `fa.be-equivalent-to-string` | `BeEquivalentToStringPattern` | `BeEquivalentTo("x") op een string` | `ShouldBe("x", StringCompareShould.IgnoreCase)` | NotBeEquivalentTo | [plan](../patterns/fa-be-equivalent-to-string-plan.md), [modellen](../patterns/fa-be-equivalent-to-string-modellen.md) |
| `fa.be-equivalent-to-object` | `BeEquivalentToObjectPattern` | `BeEquivalentTo(b) op object of collectie` | `ShouldBeEquivalentTo(b); collectie van leaves: ShouldBe(b, ignoreOrder: true)` | verschillende typen, Equals-override, cyclus, options =>, NotBeEquivalentTo | [plan](../patterns/fa-be-equivalent-to-object-plan.md), [modellen](../patterns/fa-be-equivalent-to-object-modellen.md) |

## Structuur: scopes, ketens, eigen extensies

**Figure 6.** 3 patterns; Manual-gevallen in de tabel.

<!-- kingmadoc:diagram img/patronen-6.d2 -->
![Structuur: scopes, ketens, eigen extensies](img/patronen-6.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Extra Manual | Docs |
| --- | --- | --- | --- | --- | --- |
| `fa.assertion-scope` | `AssertionScopePattern` | `using (new AssertionScope()) { a; b; }` | `x.ShouldSatisfyAllConditions(() => { a; }, () => { b; })` | context-string, andere statements, await | [plan](../patterns/fa-assertion-scope-plan.md), [modellen](../patterns/fa-assertion-scope-modellen.md) |
| `fa.and-chain` | `AndChainPattern` | `x.Should().A().And.B()` | `x.ShouldA(); x.ShouldB(); (subject met bijwerkingen: var subject = x;)` | schakel Manual of onbekend, niet in blok | [plan](../patterns/fa-and-chain-plan.md), [modellen](../patterns/fa-and-chain-modellen.md) |
| `fa.custom-assertion` | `CustomAssertionPattern` | `x.Should().MijnEigenAssertion()` | `altijd Manual, gegroepeerd per extensie` | altijd | [plan](../patterns/fa-custom-assertion-plan.md), [modellen](../patterns/fa-custom-assertion-modellen.md) |

## Volgorde

`FluentAssertionsFix.Patterns` draait in deze volgorde: `AssertionScope`, `AllSatisfy`, `AndChain`, dan de assertion-patterns (`ShouldBe`, `NotBe`, `HaveCount`, `BeTrueFalse`, `BeNull`, `BeEmpty`, `Contain`, `BeEquivalentToString`, `Throw`, `ThrowAsync`, `ThrowExactly`, `Invoking`, `BeGreaterLess`, `NotThrow`, `ContainKey`, `BeEquivalentToObject`, `OnlyContain`, `BeOfType`, `StringStartEnd`, `ContainSingle`, `EqualSequence`, `HaveLength`, `BeApproximately`, `BeSameAs`, `NotThrowAsync`, `Match`, `DirectMapping`), en als laatste `CustomAssertion`. `AssertionScope` moet eerst: de lambdas van `ShouldSatisfyAllConditions` bevatten nog FA-statements die de andere patterns daarna herschrijven. `AllSatisfy` daarna: de `using Shouldly` komt pas na alle patterns, dus de body van de loop moet FA blijven tot de assertion-patterns hem als losse statements herschrijven (TAAK-014, keuze 4; zie [fix-flow.md](fix-flow.md)). `AndChain` staat vóór de assertion-patterns, die een schakel na `.And` als Manual zouden melden; het splitst de keten en roept dezelfde patterns per schakel aan. `CustomAssertion` vangt als laatste elke overgebleven `x.Should().Iets()` van een eigen extensie.

De gedeelde gevallen (zie [pattern-beslissing.md](pattern-beslissing.md)): binnen een niet-omgezette `AssertionScope`, andere vorm dan `x.Should().Methode()` (bijv. na `.And`), resultaat gebruikt, benoemde argumenten of `becauseArgs` bij een niet-constante `because`. `becauseArgs` met een constante formatstring worden `string.Format(CultureInfo.InvariantCulture, because, args)` (TAAK-097). Proofs per pattern: `src/Unshackle.Tests/Proofs/FluentAssertions/<Naam>/`.

MediatR-patterns: zie [../README.md](../README.md#patronen).

## Zie ook

- [pattern-beslissing.md](pattern-beslissing.md) — gedeelde stappen
- [fix-flow.md](fix-flow.md) — hoe patterns na elkaar draaien
- [klassen.md](klassen.md) — `IPattern` en `PatternResult`
- [../README.md](../README.md)
