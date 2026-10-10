# Patroon-wachtrij (FluentAssertions → Shouldly)

Werklijst voor de patroon-loop (`claude/docs/patroon-loop.md`). Eén rij per patroon, bovenaan eerst.
Een agent claimt de **eerste** rij met status `open` (of `geblokkeerd` waarvan de blokkade klaar is) door de status
meteen te wijzigen. Voorbeelden van gebruik: `samples/SampleShop/SampleShop.Tests/`.

Statussen: `open` · `bezig (TAAK-NNN)` · `geblokkeerd (TAAK-NNN, wacht op TAAK-MMM)` · `klaar (TAAK-NNN)` · `mislukt (TAAK-NNN, poging N)`

| #   | Patroon-ID                   | FluentAssertions                                  | Shouldly (verwacht)                                                                      | Status |
| --- | ---------------------------- | ------------------------------------------------- | ---------------------------------------------------------------------------------------- | ------ |
| 0   | _fundament_                  | contracten, Fixer, proof-runner + `fa.should-be`  | —                                                                                        | klaar (TAAK-001)         |
| 1   | `fa.have-count`              | `x.Should().HaveCount(n)`                         | List → `x.Count.ShouldBe(n)`, IEnumerable → `x.Count().ShouldBe(n)`, dictionary → Manual | klaar (TAAK-002)        |
| 2   | `fa.be-true-false`           | `x.Should().BeTrue()` / `BeFalse()`               | `x.ShouldBeTrue()` / `ShouldBeFalse()`                                                   | klaar (TAAK-003) |
| 3   | `fa.be-null`                 | `x.Should().BeNull()` / `NotBeNull()`             | `x.ShouldBeNull()` / `ShouldNotBeNull()`                                                 | open   |
| 4   | `fa.be-empty`                | `x.Should().BeEmpty()` / `NotBeEmpty()`           | `x.ShouldBeEmpty()` / `ShouldNotBeEmpty()`                                               | open   |
| 5   | `fa.contain`                 | `x.Should().Contain(item)` / `Contain(predicate)` | `x.ShouldContain(item)` / `ShouldContain(predicate)`                                     | open   |
| 6   | `fa.throw`                   | `act.Should().Throw<T>()` (+ `.WithMessage`)      | `Should.Throw<T>(act)` (+ message-check)                                                 | open   |
| 7   | `fa.throw-async`             | `await act.Should().ThrowAsync<T>()`              | `await Should.ThrowAsync<T>(act)`                                                        | open   |
| 8   | `fa.not-throw`               | `act.Should().NotThrow()`                         | `Should.NotThrow(act)`                                                                   | open   |
| 9   | `fa.be-equivalent-to-string` | `s.Should().BeEquivalentTo("x")`                  | `s.ShouldBe("x", StringCompareShould.IgnoreCase)`                                        | open   |
| 10  | `fa.be-greater-less`         | `BeGreaterThan` / `BeLessThan` (+ `.And`-ketens)  | `ShouldBeGreaterThan` / `ShouldBeLessThan`, keten splitsen                               | open   |
| 11  | `fa.contain-key`             | `dict.Should().ContainKey(k)`                     | `dict.ShouldContainKey(k)`                                                               | open   |
| 12  | `fa.only-contain`            | `x.Should().OnlyContain(pred)`                    | `x.ShouldAllBe(pred)`                                                                    | open   |
| 13  | `fa.all-satisfy`             | `x.Should().AllSatisfy(a => ...)`                 | `x.ShouldAllBe(...)` of Manual                                                           | open   |
| 14  | `fa.be-equivalent-to-object` | `obj.Should().BeEquivalentTo(other)`              | `obj.ShouldBeEquivalentTo(other)` of Manual (semantiek!)                                 | open   |

## Zie ook

- `claude/docs/patroon-loop.md` — hoe de loop en de agents werken
- `docs/projectinfo/project.md` sectie "Voorbeelden van herschrijvingen"
