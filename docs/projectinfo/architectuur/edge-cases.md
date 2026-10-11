# Edge cases uit echte projecten

Afwijkende vormen van de 15 bestaande FA-patronen, gevonden door de tests van open-source projecten te lezen (alleen
grep, niets uitgevoerd). De proofs (`edge-<slug>`) zijn eigen minimale voorbeelden, geen gekopieerde code.

## Bronnen

| Project                    | Soort                                             | FA-versie | URL                                                     |
| -------------------------- | ------------------------------------------------- | --------- | ------------------------------------------------------- |
| CSharpFunctionalExtensions | domein-lib, Result/Maybe, veel `HasValue`/`NotBe` | 6.12      | https://github.com/vkhorikov/CSharpFunctionalExtensions |
| EventFlow                  | CQRS/ES, async-heavy, ASP.NET-integratie          | 5.10      | https://github.com/eventflow/EventFlow                  |
| Ardalis.Specification      | EF-specificaties, global usings                   | 7.2       | https://github.com/ardalis/Specification                |
| NUKE                       | build-tool, paden/strings, multiline-ketens       | 6.x       | https://github.com/nuke-build/nuke                      |
| Ardalis.SmartEnum          | enums/records, `Throw` + `WithInnerException`     | 6.12      | https://github.com/ardalis/SmartEnum                    |
| Refit                      | HTTP-client, `#if NETx_OR_GREATER`, raw strings   | (deels)   | https://github.com/reactiveui/refit                     |

## Vormen → proofs

| Vorm                                         | Bron                             | Proof                                                                                                            | Status                        |
| -------------------------------------------- | -------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| Interpolated string als verwachting          | NUKE                             | `ShouldBe/edge-interpolated-expected`, `Contain/edge-interpolated`, `BeEquivalentToString/edge-interpolated`     | groen                         |
| Interpolated string als `because`            | algemeen                         | `ShouldBe/`, `BeTrueFalse/`, `BeGreaterLess/edge-interpolated-because`                                           | groen                         |
| Named `because:`                             | algemeen                         | `ShouldBe/edge-named-because` (Manual)                                                                           | groen                         |
| Cast als subject `((T)x).Should()`           | NUKE                             | `ShouldBe/edge-cast-subject`                                                                                     | groen                         |
| `await` in subject                           | EventFlow                        | `ShouldBe/edge-await-subject`, `BeEmpty/edge-await-subject`                                                      | groen                         |
| Method-chain / LINQ als subject              | CSharpFunctionalExtensions, NUKE | `ShouldBe/edge-method-chain-subject`, `BeTrueFalse/edge-linq-subject`, `BeEmpty/`, `HaveCount/edge-linq-subject` | groen                         |
| `x.HasValue.Should().BeTrue()`               | CSharpFunctionalExtensions       | `BeTrueFalse/edge-has-value`                                                                                     | groen                         |
| Pattern-expressie als subject                | algemeen                         | `BeTrueFalse/edge-pattern-subject`                                                                               | groen                         |
| Multiline-keten (`.Should()` op eigen regel) | NUKE, SmartEnum                  | `ShouldBe/`, `HaveCount/edge-multiline`, `Throw/edge-multiline-message`                                          | groen (keten wordt één regel) |
| Comments rond de assertion                   | algemeen                         | `ShouldBe/edge-trailing-comment`                                                                                 | groen                         |
| Generics (`T`, `List<T>`)                    | algemeen                         | `ShouldBe/edge-generic`, `HaveCount/edge-generic-element`, `Contain/edge-generic-list`                           | groen                         |
| Enum / geneste generic dictionary            | SmartEnum                        | `ShouldBe/edge-enum`, `ContainKey/edge-nested-generic`                                                           | groen                         |
| Raw string literal                           | Refit                            | `ShouldBe/edge-raw-string`                                                                                       | groen                         |
| `!` als subject                              | algemeen                         | `BeNull/edge-null-forgiving`                                                                                     | groen                         |
| `NotBeNull().And.…`                          | SmartEnum                        | `BeNull/edge-not-null-and` (Manual)                                                                              | groen                         |
| Multiline-lambda / lambda-inspector          | algemeen                         | `OnlyContain/edge-multiline-lambda`, `AllSatisfy/edge-expression-lambda`                                         | groen                         |
| `double` / `BeEquivalentTo(new[] {…})`       | SmartEnum                        | `BeGreaterLess/edge-double`, `BeEquivalentToObject/edge-array-literal` (Manual)                                  | groen                         |
| `Throw<T>().Which…` / `.WithInnerException`  | NUKE, SmartEnum                  | `Throw/edge-which-message`, `Throw/edge-inner-exception` (Manual)                                                | groen                         |
| Lokale `Func<Task> act = async () => …`      | EventFlow                        | `ThrowAsync/edge-local-async-lambda`                                                                             | groen                         |
| Expression-bodied void-methode               | algemeen                         | `ShouldBe`, `BeNull`, `NotThrow/edge-expression-bodied`                                                         | groen, TAAK-040               |
| `a?.B.Should()`                              | algemeen                         | `BeNull/edge-null-conditional`                                                                                  | groen, TAAK-041               |
| `.ConfigureAwait(false)` na `ThrowAsync`     | EventFlow                        | `ThrowAsync/edge-configure-await`                                                                               | groen, TAAK-042               |
| `#if` rond assertions                        | Refit                            | `_Pending/…/ShouldBe/edge-ifdef`                                                                                 | pending, TAAK-043             |
| `global using FluentAssertions;`             | Ardalis.Specification            | `_Pending/…/ShouldBe/edge-global-using`                                                                          | pending, TAAK-044             |

## Niet als proof

- `NotBe(...)`, `BeSameAs`, `BeOfType`, `ContainSingle`, `Equal(...)`: veel gebruikt (CSharpFunctionalExtensions,
  SmartEnum, NUKE), maar geen bestaand patroon. Kandidaten voor de patroon-queue.
- Top-level statements: de proof-runner compileert als library (CS8805), dus niet te bewijzen.
- `using FA = FluentAssertions;`, `using static`, `dynamic`: niet gevonden; `dynamic.Should()` compileert niet.

## Zie ook

- `docs/projectinfo/architectuur/testen.md` — proofs en de map `_Pending`
- `claude/tasks/archief/TAAK-040-…` t/m `TAAK-042-…`; `claude/tasks/TAAK-043-…`, `TAAK-044-fa-global-using.md`
