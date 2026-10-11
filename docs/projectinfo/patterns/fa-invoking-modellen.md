# Modellen: fa.invoking

`x.Invoking(s => s.M()).Should().Throw<T>()` wordt `Should.Throw<T>(() => x.M())`; ook `Awaiting`/`ThrowAsync`, `ThrowExactly(Async)` en `NotThrow`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs`, `InvokingPattern.cs`, `ThrowPattern.cs`, `ThrowAsyncPattern.cs`, `NotThrowPattern.cs` en `ThrowMessage.cs`.

**Figure 1.** Klassen rond `InvokingPattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-invoking-modellen-1.d2 -->
![Modellen: fa.invoking](img/fa-invoking-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `InvokingPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs` | Dit patroon, Id `fa.invoking`: claimt de nodes |
| `InvokingPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs` | `RewriteAssertion` kiest het herschrijvende patroon; `TryGetAction` maakt `() => ...` |
| `ThrowPattern`, `ThrowAsyncPattern`, `NotThrowPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/` | Herschrijven (ook ThrowExactly en Invoking) |
| `ThrowMessage` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowMessage.cs` | `OfType`, message-check, AggregateException-heuristiek |

**Figure 2.** Volgorde van `Find` en `Rewrite`.

<!-- kingmadoc:diagram img/fa-invoking-modellen-2.d2 -->
![Modellen: fa.invoking](img/fa-invoking-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `InvokingPattern.Find` | `Find(root, model, owns)` van de Throw-patterns met het eigen filter |
| Rewrite | `InvokingPattern.RewriteAssertion` | Kiest Throw/ThrowAsync/NotThrow op de naam van de assertion |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/Invoking/`.

<!-- kingmadoc:diagram img/fa-invoking-modellen-3.d2 -->
![Modellen: fa.invoking](img/fa-invoking-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Resultaat weggegooid? | is used | `geen proof (sample: .And)` |
| Receiver local/parameter/field/this? | may have side effects | `side-effect-receiver` |
| Receiver reference type? | is a value type | `value-type` |
| Lambda met één parameter? | one-parameter lambda | `method-group` |
| Geen Wait/Result in de lambda (alleen Throw<T>)? | AggregateException | `aggregate` |
| Geen becauseArgs? | becauseArgs | `because-args` |

## Zie ook

- [fa-invoking-plan.md](fa-invoking-plan.md) - plan
- [fa-invoking-verify.md](fa-invoking-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/Invoking/` - de proofs
- [../README.md](../README.md)
