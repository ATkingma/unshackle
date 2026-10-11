# Modellen: fa.only-contain

Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OnlyContainPattern.cs`.

## Figuur 1: klassen

**Figure 1.** `OnlyContainPattern`, zijn contract en de helpers; rechts de FluentAssertions-methode die hij herkent en de twee Shouldly-methoden die hij maakt.

<!-- kingmadoc:diagram img/fa-only-contain-modellen-1.d2 -->
![Figuur 1: klassen](img/fa-only-contain-modellen-1.png)

| Element | Rol |
| ------- | --- |
| `OnlyContainPattern` | Vindt `OnlyContain` (`Find`, de hele statement als die in een blok staat) en bouwt twee statements (`Rewrite`) |
| `FluentAssertionsSyntax` | Gedeelde stappen: onderwerp, weggegooid resultaat, `because`-argumenten |
| `PatternResult` | `Fixed` met twee statements, of `Manual(reden)` |
| FA- en Shouldly-methoden | `OnlyContain` faalt op een lege collectie, `ShouldAllBe` niet: `ShouldNotBeEmpty` vult dat aan |

## Figuur 2: sequence

**Figure 2.** Eén document: `Find` verzamelt statements of aanroepen, `Rewrite` beslist per node tussen `Fixed` (twee statements) en `Manual`.

<!-- kingmadoc:diagram img/fa-only-contain-modellen-2.d2 -->
![Figuur 2: sequence](img/fa-only-contain-modellen-2.png)

| Stap | Methode in de code | Wat gebeurt er |
| ---- | ------------------ | -------------- |
| Vinden | `Find` | `FindInvocations` met `OnlyContain`; staat de aanroep als statement in een blok, dan is de statement de node |
| Subject | `GetSubject` | Moet `x.Should()` zijn, anders Manual |
| Resultaat | `IsResultDiscarded` | Gebruikt resultaat geeft Manual |
| Argumenten | `TryGetArguments` | Predicaat en `because`; `becauseArgs` geeft Manual |
| Type | `SemanticModel.GetTypeInfo` | Moet generieke `IEnumerable<T>` zijn |

## Figuur 3: beslisboom

**Figure 3.** Elke vraag in `Rewrite`; de bladeren zijn de Shouldly-vorm of de Manual-reden.

<!-- kingmadoc:diagram img/fa-only-contain-modellen-3.d2 -->
![Figuur 3: beslisboom](img/fa-only-contain-modellen-3.png)

| Blad | Voorwaarde | Proof (`Proofs/FluentAssertions/OnlyContain/`) |
| ---- | ---------- | ---------------------------------------------- |
| Fixed | statement in blok, eenvoudig subject | `simple`, `enumerable`, `array`, `dictionary`, `member-subject` |
| Fixed | met `because` (op beide statements) | `because` |
| Manual "is not in the form" | na `.And` | `and-chain` |
| Manual "is used" | `.And`, variabele | `chain`, `used` |
| Manual "becauseArgs" | format-argumenten | `because-args` |
| Manual "cannot hold two statements" | lambda-body, `if` zonder accolades | `lambda`, `no-block` |
| Manual "evaluate the subject twice" | subject is een aanroep | `subject-call` |

## Zie ook

- [fa-only-contain-plan.md](fa-only-contain-plan.md)
- [fa-only-contain-verify.md](fa-only-contain-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OnlyContainPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/OnlyContain/`
