# Edge cases from real projects

Unusual forms of the 15 existing FA patterns, found by reading the tests of open-source projects (grep only,
nothing executed). The proofs (`edge-<slug>`) are our own minimal examples, not copied code.

## Sources

| Project                    | Kind                                              | FA version | URL                                                     |
| -------------------------- | ------------------------------------------------- | ---------- | ------------------------------------------------------- |
| CSharpFunctionalExtensions | domain lib, Result/Maybe, lots of `HasValue`/`NotBe` | 6.12    | https://github.com/vkhorikov/CSharpFunctionalExtensions |
| EventFlow                  | CQRS/ES, async-heavy, ASP.NET integration         | 5.10       | https://github.com/eventflow/EventFlow                  |
| Ardalis.Specification      | EF specifications, global usings                  | 7.2        | https://github.com/ardalis/Specification                |
| NUKE                       | build tool, paths/strings, multiline chains       | 6.x        | https://github.com/nuke-build/nuke                      |
| Ardalis.SmartEnum          | enums/records, `Throw` + `WithInnerException`     | 6.12       | https://github.com/ardalis/SmartEnum                    |
| Refit                      | HTTP client, `#if NETx_OR_GREATER`, raw strings   | (partly)   | https://github.com/reactiveui/refit                     |

## Forms → proofs

| Form                                         | Source                           | Proof                                                                                                            | Status                        |
| -------------------------------------------- | -------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| Interpolated string as expected value        | NUKE                             | `ShouldBe/edge-interpolated-expected`, `Contain/edge-interpolated`, `BeEquivalentToString/edge-interpolated`     | green                         |
| Interpolated string as `because`             | general                          | `ShouldBe/`, `BeTrueFalse/`, `BeGreaterLess/edge-interpolated-because`                                           | green                         |
| Named `because:`                             | general                          | `ShouldBe/edge-named-because` (Manual)                                                                           | green                         |
| Cast as subject `((T)x).Should()`            | NUKE                             | `ShouldBe/edge-cast-subject`                                                                                     | green                         |
| `await` in subject                           | EventFlow                        | `ShouldBe/edge-await-subject`, `BeEmpty/edge-await-subject`                                                      | green                         |
| Method chain / LINQ as subject               | CSharpFunctionalExtensions, NUKE | `ShouldBe/edge-method-chain-subject`, `BeTrueFalse/edge-linq-subject`, `BeEmpty/`, `HaveCount/edge-linq-subject` | green                         |
| `x.HasValue.Should().BeTrue()`               | CSharpFunctionalExtensions       | `BeTrueFalse/edge-has-value`                                                                                     | green                         |
| Pattern expression as subject                | general                          | `BeTrueFalse/edge-pattern-subject`                                                                               | green                         |
| Multiline chain (`.Should()` on its own line) | NUKE, SmartEnum                 | `ShouldBe/`, `HaveCount/edge-multiline`, `Throw/edge-multiline-message`                                          | green (chain becomes one line) |
| Comments around the assertion                | general                          | `ShouldBe/edge-trailing-comment`                                                                                 | green                         |
| Generics (`T`, `List<T>`)                    | general                          | `ShouldBe/edge-generic`, `HaveCount/edge-generic-element`, `Contain/edge-generic-list`                           | green                         |
| Enum / nested generic dictionary             | SmartEnum                        | `ShouldBe/edge-enum`, `ContainKey/edge-nested-generic`                                                           | green                         |
| Raw string literal                           | Refit                            | `ShouldBe/edge-raw-string`                                                                                       | green                         |
| `!` as subject                               | general                          | `BeNull/edge-null-forgiving`                                                                                     | green                         |
| `NotBeNull().And.…`                          | SmartEnum                        | `BeNull/edge-not-null-and` (Manual), `AndChain/simple` (split)                                                   | green (TAAK-033)              |
| Multiline `.And` chain                       | NUKE                             | `AndChain/edge-multiline`                                                                                        | green (TAAK-033)              |
| Multiline lambda / lambda inspector          | general                          | `OnlyContain/edge-multiline-lambda`, `AllSatisfy/edge-expression-lambda`                                         | green                         |
| `double` / `BeEquivalentTo(new[] {…})`       | SmartEnum                        | `BeGreaterLess/edge-double`, `BeEquivalentToObject/edge-array-literal` (Manual)                                  | green                         |
| `Throw<T>().Which…` / `.WithInnerException`  | NUKE, SmartEnum                  | `Throw/edge-which-message`, `Throw/edge-inner-exception` (Manual)                                                | green                         |
| Local `Func<Task> act = async () => …`       | EventFlow                        | `ThrowAsync/edge-local-async-lambda`                                                                             | green                         |
| Expression-bodied void method                | general                          | `ShouldBe`, `BeNull`, `NotThrow/edge-expression-bodied`                                                          | green, TAAK-040               |
| `a?.B.Should()`                              | general                          | `BeNull/edge-null-conditional`                                                                                   | green, TAAK-041               |
| `.ConfigureAwait(false)` after `ThrowAsync`  | EventFlow                        | `ThrowAsync/edge-configure-await`                                                                                | green, TAAK-042               |
| `#if` around assertions                      | Refit                            | `ShouldBe/edge-ifdef` (old using stays, Manual)                                                                  | green (TAAK-043)              |
| `global using FluentAssertions;`             | Ardalis.Specification            | `ShouldBe/edge-global-using`                                                                                     | green (TAAK-044)              |

### Corpus round 2 (2026-10-11, see `corpus.md`)

| Form                                              | Source        | Proof                                                         | Status            |
| ------------------------------------------------- | ------------- | ------------------------------------------------------------- | ----------------- |
| Assertion in lambda, `Throw<XunitException>` on it | FluentResults | `FluentAssertions/ShouldBe/edge-assertion-in-throw-lambda`, `Throw/…` | Manual (both), TAAK-070 |
| `coll.Should().BeEquivalentTo("a", "b")` (params) | FluentResults | `FluentAssertions/BeEquivalentToObject/edge-collection-params` | Manual (collection), TAAK-071 |
| `ContainSingle().Subject` instead of `.Which`     | Akka.NET      | `FluentAssertions/ContainSingle/edge-subject`                 | Fixed, TAAK-072 |

## Not as a proof

- `BeSameAs`, `Equal(...)` (`ContainSingle`: now `fa.contain-single-which`, TAAK-036): widely used (CSharpFunctionalExtensions,
  SmartEnum, NUKE), but no existing pattern. Candidates for the pattern queue.
- Top-level statements: the proof runner compiles as a library (CS8805), so they cannot be proven.
- `using FA = FluentAssertions;`, `using static`, `dynamic`: not found; `dynamic.Should()` does not compile.

## See also

- `docs/projectinfo/architectuur/testen.md` — proofs and the `_Pending` folder
- `docs/projectinfo/architectuur/corpus.md` — frequency table and fix runs on real projects
- `claude/tasks/archief/TAAK-040-…` through `TAAK-042-…`; `claude/tasks/TAAK-043-…`, `TAAK-044-fa-global-using.md`
