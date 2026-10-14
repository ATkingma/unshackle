# Models: fa.catch-assertion-exception

Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CatchAssertionExceptionPattern.cs`. The pattern implements `IPattern` directly (no `FluentAssertionPattern`: there is no `x.Should()`), and comes after `CustomAssertionPattern` in `FluentAssertionsFix.Patterns`.

| Element                          | Path                                                                                   | Role                                                                                   |
| -------------------------------- | -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `IPattern`                       | `src/Unshackle.Core/Abstractions/IPattern.cs`                                          | Contract: `Find` and `Rewrite`                                                         |
| `CatchAssertionExceptionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CatchAssertionExceptionPattern.cs` | This pattern, Id `fa.catch-assertion-exception`                                        |
| `Fixer`                          | `src/Unshackle.Core/Fixer.cs`                                                          | Replaces one catch with two catches (`ReplaceWithTrivia`), adds `using Shouldly`       |

| Step    | What happens                                                                                                                                                                                     |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Find    | Per try the first `catch (T)`; per condition (upwards via `()`, `!`, `\|\|`, `&&`) the first `is T`/is-pattern; T = FA or xunit assertion exception; sites that mention `ShouldAssertException` are skipped |
| Rewrite | Catch → `[catch (T), catch (ShouldAssertException)]`; `e is T` → `e is T \|\| e is ShouldAssertException`; otherwise Manual                                                                       |

Decision tree of `Rewrite`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/CatchAssertionException/`.

| Condition                                          | Outcome                                                                      | Proof / test                              |
| -------------------------------------------------- | ---------------------------------------------------------------------------- | ----------------------------------------- |
| `catch (T)` or `catch (T e)` with e unused         | extra catch for `ShouldAssertException`                                      | `catch-clause`                            |
| `catch (T e)` with e used (body or filter)         | Manual                                                                       | `catch-uses-exception`                    |
| `e is T` with an identifier on the left            | `\|\| e is ShouldAssertException`, parentheses only inside another operator  | `retry-filter`, `when-filter`             |
| is-pattern (`e is T x`, `is not T`), or `x.Y is T` | Manual                                                                       | `Rewrite_IsPatternWithDesignation_Manual` |

## See also

- [fa-catch-assertion-exception-plan.md](fa-catch-assertion-exception-plan.md) - plan
- [fa-catch-assertion-exception-verify.md](fa-catch-assertion-exception-verify.md) - verification
- [../README.md](../README.md)
