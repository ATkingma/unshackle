# Modellen: fa.catch-assertion-exception

Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CatchAssertionExceptionPattern.cs`. Het patroon implementeert `IPattern` direct (geen `FluentAssertionPattern`: er is geen `x.Should()`), staat na `CustomAssertionPattern` in `FluentAssertionsFix.Patterns`.

| Element                          | Pad                                                                                    | Rol                                                                                    |
| -------------------------------- | -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `IPattern`                       | `src/Unshackle.Core/Abstractions/IPattern.cs`                                          | Contract: `Find` en `Rewrite`                                                          |
| `CatchAssertionExceptionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CatchAssertionExceptionPattern.cs` | Dit patroon, Id `fa.catch-assertion-exception`                                         |
| `Fixer`                          | `src/Unshackle.Core/Fixer.cs`                                                          | Vervangt een catch door twee catches (`ReplaceWithTrivia`), voegt `using Shouldly` toe |

| Stap    | Wat gebeurt er                                                                                                                                                                                   |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Find    | Per try de eerste `catch (T)`; per conditie (omhoog via `()`, `!`, `\|\|`, `&&`) de eerste `is T`/is-pattern; T = FA- of xunit-assertie-exception; plekken met `ShouldAssertException` vallen af |
| Rewrite | Catch → `[catch (T), catch (ShouldAssertException)]`; `e is T` → `e is T \|\| e is ShouldAssertException`; anders Manual                                                                         |

Beslisboom van `Rewrite`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/CatchAssertionException/`.

| Voorwaarde                                         | Uitkomst                                                                     | Proof / test                              |
| -------------------------------------------------- | ---------------------------------------------------------------------------- | ----------------------------------------- |
| `catch (T)` of `catch (T e)` met e ongebruikt      | extra catch voor `ShouldAssertException`                                     | `catch-clause`                            |
| `catch (T e)` met e gebruikt (body of filter)      | Manual                                                                       | `catch-uses-exception`                    |
| `e is T` met identifier links                      | `\|\| e is ShouldAssertException`, haakjes alleen binnen een andere operator | `retry-filter`, `when-filter`             |
| is-pattern (`e is T x`, `is not T`), of `x.Y is T` | Manual                                                                       | `Rewrite_IsPatternWithDesignation_Manual` |

## Zie ook

- [fa-catch-assertion-exception-plan.md](fa-catch-assertion-exception-plan.md) - plan
- [fa-catch-assertion-exception-verify.md](fa-catch-assertion-exception-verify.md) - verificatie
- [../README.md](../README.md)
