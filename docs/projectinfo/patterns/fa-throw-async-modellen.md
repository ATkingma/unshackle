# Modellen: fa.throw-async

Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs` en `ThrowMessage.cs`.

## Figuur 1: klassen

**Figure 1.** `ThrowAsyncPattern`, zijn contract en helpers; de helper `ThrowMessage` wordt gedeeld met `ThrowPattern`.

<!-- kingmadoc:diagram img/fa-throw-async-modellen-1.d2 -->
![Figuur 1: klassen](img/fa-throw-async-modellen-1.png)

| Element | Rol |
| ------- | --- |
| `ThrowAsyncPattern` | Vindt `ThrowAsync`/`ThrowExactlyAsync` (+ `.WithMessage`, + omringende `await`) en bouwt `await Should.ThrowAsync<T>(subject)` |
| `FluentAssertionsSyntax` | Gedeelde stappen: onderwerp, `because`-argumenten (negeert de receiver van extensiemethoden) |
| `ThrowMessage` | Vertaalt het `WithMessage`-wildcard naar `Message.ShouldContain/StartWith/EndWith/Be`; `null` bij andere wildcards |
| `PatternResult` | `Fixed` met de nieuwe expressie, of `Manual(reden)` |
| FA- en Shouldly-methoden | `ThrowAsync<T>`, `WithMessage` worden `Should.ThrowAsync<T>` en een `Message`-check |

## Figuur 2: sequence

**Figure 2.** `Find` geeft per aanroep de `await`-expressie; `Rewrite` beslist tussen `Fixed` en `Manual`.

<!-- kingmadoc:diagram img/fa-throw-async-modellen-2.d2 -->
![Figuur 2: sequence](img/fa-throw-async-modellen-2.png)

| Stap | Methode in de code | Wat gebeurt er |
| ---- | ------------------ | -------------- |
| Vinden | `Find` | `FindInvocations("ThrowAsync", "ThrowExactlyAsync")`, daarna naar `.WithMessage` en de omringende `await` |
| Subject | `GetSubject` | Moet `x.Should()` zijn |
| Subjecttype | `SemanticModel.GetTypeInfo` | `Func<Task>` of `Func<Task<T>>` met T referentietype |
| Argumenten | `TryGetArguments` | `because`; `becauseArgs` geeft Manual |
| Bericht | `ThrowMessage.Check` | Wildcard naar `ShouldContain`/`StartWith`/`EndWith`/`Be` |

## Figuur 3: beslisboom

**Figure 3.** Elke vraag in `Rewrite`; de bladeren zijn de Shouldly-vorm of de Manual-reden.

<!-- kingmadoc:diagram img/fa-throw-async-modellen-3.d2 -->
![Figuur 3: beslisboom](img/fa-throw-async-modellen-3.png)

| Blad | Voorwaarde | Proof (`Proofs/FluentAssertions/ThrowAsync/`) |
| ---- | ---------- | --------------------------------------------- |
| Fixed | `Func<Task>`, `Func<Task<string>>`, met/zonder `because` | `simple`, `func-task-reference`, `because` |
| Fixed | `.WithMessage` met `*x*`, `x*`, `*x`, `x` | `message-contains`, `message-starts`, `message-ends`, `message-exact`, `message-because`, `message-exact-because` |
| Manual "requires the exact exception type" | `ThrowExactlyAsync` | `throw-exactly` |
| Manual "not directly awaited" | geen `await`, of `.Where` erachter | `no-await`, `where` |
| Manual "is used" | `var x = await ...` | `value-used` |
| Manual "Invoking" | subject via `Awaiting(...)` | `awaiting` |
| Manual "reference type" | `Func<Task<int>>` | `value-func` |
| Manual "becauseArgs" | format-argumenten | `because-args`, `message-because-args` |
| Manual "not a string constant" / "wildcards other than" | niet-vertaalbaar patroon | `message-variable`, `message-wildcard` |

## Zie ook

- [fa-throw-async-plan.md](fa-throw-async-plan.md)
- [fa-throw-async-verify.md](fa-throw-async-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/`
