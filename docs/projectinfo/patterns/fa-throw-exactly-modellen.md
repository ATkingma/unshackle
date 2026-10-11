# Modellen: fa.throw-exactly

`act.Should().ThrowExactly<T>()` wordt `Should.Throw<T>(act).ShouldBeOfType<T>()`; `await act.Should().ThrowExactlyAsync<T>()` wordt `(await Should.ThrowAsync<T>(act)).ShouldBeOfType<T>()`; `.WithMessage` zoals `fa.throw`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowExactlyPattern.cs`, `InvokingPattern.cs`, `ThrowPattern.cs`, `ThrowAsyncPattern.cs`, `NotThrowPattern.cs` en `ThrowMessage.cs`.

**Figure 1.** Klassen rond `ThrowExactlyPattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-throw-exactly-modellen-1.d2 -->
![Modellen: fa.throw-exactly](img/fa-throw-exactly-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `ThrowExactlyPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowExactlyPattern.cs` | Dit patroon, Id `fa.throw-exactly`: claimt de nodes |
| `InvokingPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs` | `RewriteAssertion` kiest het herschrijvende patroon; `TryGetAction` maakt `() => ...` |
| `ThrowPattern`, `ThrowAsyncPattern`, `NotThrowPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/` | Herschrijven (ook ThrowExactly en Invoking) |
| `ThrowMessage` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowMessage.cs` | `OfType`, message-check, AggregateException-heuristiek |

**Figure 2.** Volgorde van `Find` en `Rewrite`.

<!-- kingmadoc:diagram img/fa-throw-exactly-modellen-2.d2 -->
![Modellen: fa.throw-exactly](img/fa-throw-exactly-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `ThrowExactlyPattern.Find` | `Find(root, model, owns)` van de Throw-patterns met het eigen filter |
| Rewrite | `InvokingPattern.RewriteAssertion` | Kiest Throw/ThrowAsync/NotThrow op de naam van de assertion |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowExactly/`.

<!-- kingmadoc:diagram img/fa-throw-exactly-modellen-3.d2 -->
![Modellen: fa.throw-exactly](img/fa-throw-exactly-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm act.Should().ThrowExactly<T>()? | is not in the form | `geen proof` |
| Resultaat weggegooid? | is used | `value-used` |
| Delegate die Should.Throw(Async) accepteert? | The subject is not | `geen proof (fa.throw: value-func)` |
| Geen becauseArgs? | becauseArgs | `because-args` |

## Zie ook

- [fa-throw-exactly-plan.md](fa-throw-exactly-plan.md) - plan
- [fa-throw-exactly-verify.md](fa-throw-exactly-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowExactlyPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowExactly/` - de proofs
- [../README.md](../README.md)
