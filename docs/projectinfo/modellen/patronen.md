# Patronen: FluentAssertions naar Shouldly

Overzicht van de vijftien patterns, in vier groepen (een figuur per groep, elk met hoogstens twaalf pijlen). De uitvoervolgorde is die van `FluentAssertionsFix.Patterns`: `AllSatisfyPattern` staat bewust **eerst**, zie [Volgorde](#volgorde). Alle patterns vragen ook de gedeelde controles van [pattern-beslissing.md](pattern-beslissing.md).

## Waarden, booleans, null, leeg, tellen, vergelijken

**Figure 1.** Zes patterns voor losse waarden en getallen.

<!-- kingmadoc:diagram img/patronen-1.d2 -->
![Waarden, booleans, null, leeg, tellen, vergelijken](img/patronen-1.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Type-afhankelijk | Extra Manual | Taak |
| ---------- | ------ | ------- | ------------- | ---------------- | ------------ | ---- |
| `fa.should-be` | `ShouldBePattern` | `Be(y)` | `ShouldBe(y)` | Verwachte waarde krijgt een cast als FA hem naar het type van het subject zou omzetten | `FluentAssertions.Xml`/`Types`-overloads, comparer, niet te casten type | TAAK-001 |
| `fa.have-count` | `HaveCountPattern` | `HaveCount(n)` | `.Length`, `.Count` of `.Count()` plus `ShouldBe(n)` | Array, type met `Count`, `IEnumerable<T>` (dan `System.Linq`) | Predicate, dictionary, `?.`, niet-generiek `IEnumerable`, onbekend type | TAAK-002 |
| `fa.be-true-false` | `BeTrueFalsePattern` | `BeTrue()`, `BeFalse()` | `ShouldBeTrue()`, `ShouldBeFalse()`; `bool?`: `ShouldBe(true)` | `bool` of `bool?` | Elk ander type | TAAK-003 |
| `fa.be-null` | `BeNullPattern` | `BeNull()`, `NotBeNull()` | `ShouldBeNull()`, `ShouldNotBeNull()` | Geen: dezelfde extension voor referentietypes en `Nullable<T>` | Alleen de gedeelde gevallen | TAAK-004 |
| `fa.be-empty` | `BeEmptyPattern` | `BeEmpty()`, `NotBeEmpty()` | `ShouldBeEmpty()`, `ShouldNotBeEmpty()` | Geen: Shouldly heeft ze voor string en `IEnumerable<T>` (ook dictionary) | Alleen de gedeelde gevallen | TAAK-006 |
| `fa.be-greater-less` | `BeGreaterLessPattern` | `BeGreaterThan`, `BeLessThan`, `BeGreaterOrEqualTo`, `BeLessOrEqualTo`, `BeInRange`, `NotBeInRange`, `BePositive`, `BeNegative` | `ShouldBeGreaterThan` e.d., `ShouldBeInRange`, `ShouldNotBeInRange`, `ShouldBePositive`, `ShouldBeNegative` | Subject moet `IComparable<T>` van zichzelf zijn; `BePositive`/`BeNegative` alleen voor int, long, short, decimal, double, float | Nullable subject (`int?`), subject twee keer geëvalueerd, `becauseArgs` | TAAK-010 |

## Collecties

**Figure 2.** Vier patterns voor collecties en dictionaries.

<!-- kingmadoc:diagram img/patronen-2.d2 -->
![Collecties](img/patronen-2.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Type-afhankelijk | Extra Manual | Taak |
| ---------- | ------ | ------- | ------------- | ---------------- | ------------ | ---- |
| `fa.contain` | `ContainPattern` | `Contain(item)`, `Contain(predicate)`, `NotContain(...)` | `ShouldContain(...)`, `ShouldNotContain(...)` | String: extra argument `Shouldly.Case.Sensitive` (FA is hoofdlettergevoelig, Shouldly niet); collectie: geen wijziging | Dictionary, `Contain(IEnumerable)` (meerdere items), occurrence-constraint, subject geen string of `IEnumerable<T>` | TAAK-007 |
| `fa.contain-key` | `ContainKeyPattern` | `ContainKey(k)`, `NotContainKey(k)` | `ShouldContainKey(k)`, `ShouldNotContainKey(k)` | Geen typecontrole; FA biedt ze alleen op dictionaries | `ContainKeys`, `ContainValue(s)` (geen Shouldly-equivalent), `.WhoseValue`/`.And`, `becauseArgs` | TAAK-013 |
| `fa.only-contain` | `OnlyContainPattern` | `OnlyContain(pred)` | Twee statements: `ShouldNotBeEmpty()` en `ShouldAllBe(pred)` (FA faalt op leeg, `ShouldAllBe` niet) | Subject moet generieke `IEnumerable<T>` zijn | Niet direct in een blok, subject niet eenvoudig (zou twee keer geëvalueerd), resultaat gebruikt, `becauseArgs` | TAAK-015 |
| `fa.all-satisfy` | `AllSatisfyPattern` | `AllSatisfy(a => { ... })` | `ShouldNotBeEmpty()` plus `foreach (var a in x) { ... }` | Subject moet naam of member access zijn | `because`/`becauseArgs`, methodgroep, async lambda, `return` in de body, parameter met modifier, niet in blok, resultaat gebruikt, `.And` | TAAK-014 |

## Exceptions

**Figure 3.** Drie patterns voor het gooien van exceptions.

<!-- kingmadoc:diagram img/patronen-3.d2 -->
![Exceptions](img/patronen-3.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Type-afhankelijk | Extra Manual | Taak |
| ---------- | ------ | ------- | ------------- | ---------------- | ------------ | ---- |
| `fa.throw` | `ThrowPattern` | `act.Should().Throw<T>()`, `.WithMessage(p)` | `Should.Throw<T>(act)`; `.WithMessage` wordt `.Message.ShouldBe/ShouldContain/ShouldStartWith/ShouldEndWith` (hoofdletterongevoelig) | Subject `Action` of `Func<T>` met referentietype; wildcard `*x*`, `x*`, `*x` of geen wildcard | `ThrowExactly`, `Invoking`, resultaat gebruikt (`.Which`, `.And`), wildcard in het midden of niet-constante pattern | TAAK-008 |
| `fa.throw-async` | `ThrowAsyncPattern` | `await act.Should().ThrowAsync<T>()`, `.WithMessage(p)` | `await Should.ThrowAsync<T>(act)`; bij een message-check `(await ...).Message...` | `Func<Task>` en `Func<Task<referentietype>>`; `Func<Task<waardetype>>` is Manual | `ThrowExactlyAsync`, `Awaiting`, niet direct awaited, resultaat gebruikt | TAAK-012 |
| `fa.not-throw` | `NotThrowPattern` | `NotThrow()` | `Should.NotThrow(act)` | Elke parameterloze `Action`/`Func<T>`, ook waardetypen | `NotThrow<T>()`, `NotThrowAfter`, resultaat gebruikt (`.Which`, `.And`), `Invoking`, `becauseArgs` | TAAK-011 |

## Equivalentie

**Figure 4.** Twee patterns voor `BeEquivalentTo`: string en object.

<!-- kingmadoc:diagram img/patronen-4.d2 -->
![Equivalentie](img/patronen-4.png)

| Pattern-ID | Klasse | FA-vorm | Shouldly-vorm | Type-afhankelijk | Extra Manual | Taak |
| ---------- | ------ | ------- | ------------- | ---------------- | ------------ | ---- |
| `fa.be-equivalent-to-string` | `BeEquivalentToStringPattern` | `BeEquivalentTo(y)` op een string | `ShouldBe(y, StringCompareShould.IgnoreCase)` | Alleen `StringAssertions` (FA negeert hoofdletters, `ShouldBe` niet) | `NotBeEquivalentTo` (geen Shouldly-equivalent), niet-string subject blijft ongemoeid | TAAK-009 |
| `fa.be-equivalent-to-object` | `BeEquivalentToObjectPattern` | `BeEquivalentTo(b)` op een object | `ShouldBeEquivalentTo(b)` | Alleen gelijk compile-time type aan beide zijden; gewone class/struct/record met leaf-leden | Collectie, `Equals`-override, cyclus, geen publieke leden, `options =>`, `NotBeEquivalentTo`, `becauseArgs` | TAAK-016 |

## Volgorde

`FluentAssertionsFix.Patterns` draait in deze volgorde: `AllSatisfy`, `ShouldBe`, `HaveCount`, `BeTrueFalse`, `BeNull`, `BeEmpty`, `Contain`, `BeEquivalentToString`, `Throw`, `ThrowAsync`, `BeGreaterLess`, `NotThrow`, `ContainKey`, `BeEquivalentToObject`, `OnlyContain`. `AllSatisfyPattern` moet vooraan: de `using Shouldly` komt pas na alle patterns. Staat het pattern achteraan, dan is de body van de lambda al herschreven en onbindbaar voor het semantic model, en vindt `Find` niets. Vooraan blijft de body FluentAssertions en herschrijven de andere patterns hem daarna als losse statements (TAAK-014, keuze 4; zie ook [fix-flow.md](fix-flow.md)).

De gedeelde Manual-gevallen: andere vorm dan `x.Should().Methode()` (bijv. na `.And`), resultaat gebruikt, `becauseArgs` of benoemde argumenten. Proofs per pattern: `src/Unshackle.Tests/Proofs/FluentAssertions/<Naam>/`. Plan en verificatie: [../README.md](../README.md#patronen).

## Zie ook

- [pattern-beslissing.md](pattern-beslissing.md) — gedeelde stappen
- [fix-flow.md](fix-flow.md) — hoe patterns na elkaar draaien
- [klassen.md](klassen.md) — `IPattern` en `PatternResult`
- [../README.md](../README.md)
