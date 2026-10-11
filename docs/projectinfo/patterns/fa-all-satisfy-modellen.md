# Modellen: fa.all-satisfy

Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AllSatisfyPattern.cs`.

## Figuur 1: klassen

**Figure 1.** `AllSatisfyPattern`, zijn contract en de helpers; rechts de FluentAssertions-aanroep die hij herkent en de Shouldly-vorm die hij maakt.

<!-- kingmadoc:diagram img/fa-all-satisfy-modellen-1.d2 -->
![Figuur 1: klassen](img/fa-all-satisfy-modellen-1.png)

| Element | Rol |
| ------- | --- |
| `AllSatisfyPattern` | Vindt `AllSatisfy` (`Find`, geeft het hele statement terug) en bouwt twee statements (`Rewrite`) |
| `FluentAssertionsSyntax` | Gedeelde stappen: aanroepen vinden, onderwerp, `because`-argumenten |
| `Reindent` / `IsSimple` | Eigen helpers: loop-body op de juiste inspringing; subject mag twee keer geëvalueerd worden |
| `PatternResult` | `Fixed` met twee statements, of `Manual(reden)` |
| FA / Shouldly | `AllSatisfy(Action<T>)` wordt `ShouldNotBeEmpty()` plus `foreach`, want `ShouldAllBe` verwacht een bool-predicate |

## Figuur 2: sequence

**Figure 2.** `Find` verzamelt de statements, `Rewrite` beslist per statement tussen `Fixed` en `Manual`.

<!-- kingmadoc:diagram img/fa-all-satisfy-modellen-2.d2 -->
![Figuur 2: sequence](img/fa-all-satisfy-modellen-2.png)

| Stap | Methode in de code | Wat gebeurt er |
| ---- | ------------------ | -------------- |
| Vinden | `Find` | `FindInvocations` met `AllSatisfy`; bij een statement-ouder wordt het statement de node |
| Subject | `GetSubject` | Moet `x.Should()` zijn, anders Manual |
| Argumenten | `TryGetArguments` | Lambda en `because`; `becauseArgs` of `because` geeft Manual |
| Controles | `IsSimple`, `Reindent` | Simpel subject, lambda zonder `return`; blok wordt opnieuw ingesprongen |
| Resultaat | `PatternResult.Fixed` | Twee statements; de Fixer geeft het tweede alleen de inspringing |

## Figuur 3: beslisboom

**Figure 3.** Elke vraag in `Rewrite`; de bladeren zijn de Shouldly-vorm of de Manual-reden.

<!-- kingmadoc:diagram img/fa-all-satisfy-modellen-3.d2 -->
![Figuur 3: beslisboom](img/fa-all-satisfy-modellen-3.png)

| Blad | Voorwaarde | Proof (`Proofs/FluentAssertions/AllSatisfy/`) |
| ---- | ---------- | --------------------------------------------- |
| Fixed | expressie- of blok-lambda, getypeerde parameter, member-subject | `simple`, `block`, `typed-parameter`, `member-subject` |
| Manual "is not in the form" | na `.And` | `and-chain` |
| Manual "is used" | `.And` erna of waarde gebruikt | `used` |
| Manual "because message" / "becauseArgs" | bericht kan niet in de loop | `because`, `because-args` |
| Manual "not an inline lambda" / "is async" | methodgroep, async lambda | `method-group`, `async` |
| Manual "contains a return" | `return` zou de methode verlaten | `return` |
| Manual "not directly in a block" | `if (...) x.Should()...;` | `not-in-block` |
| Manual "evaluate it twice" | subject met aanroep | `subject-call` |

## Zie ook

- [fa-all-satisfy-plan.md](fa-all-satisfy-plan.md)
- [fa-all-satisfy-verify.md](fa-all-satisfy-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AllSatisfyPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AllSatisfy/`
