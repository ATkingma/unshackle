# Modellen: fa.be-greater-less

Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs`.

## Figuur 1: klassen

**Figure 1.** `BeGreaterLessPattern`, zijn contract en de helpers; rechts de FluentAssertions-methoden die hij herkent en de Shouldly-methoden die hij maakt.

<!-- kingmadoc:diagram img/fa-be-greater-less-modellen-1.d2 -->
![Figuur 1: klassen](img/fa-be-greater-less-modellen-1.png)

| Element | Rol |
| ------- | --- |
| `BeGreaterLessPattern` | Vindt de vergelijkingen (`Find`) en bouwt `Should...`-aanroepen (`RewriteAssertion`, na de gedeelde controles van `FluentAssertionPattern`); `.And`-ketens splitst `AndChainPattern` |
| `FluentAssertionsSyntax` | Gedeelde stappen: onderwerp (`GetSubject`), weggegooid resultaat, `because`-argumenten (`TryGetArguments`), `ConvertExpected` (cast naar het subjecttype) |
| `FluentAssertionPattern` | Template Method: `Rewrite` doet scope-, vorm-, resultaat- en argumentcontroles en roept `RewriteAssertion` aan |
| `PatternResult` | `Fixed` met één node, of `Manual(reden)` |
| FA- en Shouldly-methoden | Vaste vertaaltabel `s_methods`; aliassen `BeGreaterOrEqualTo` en `BeLessOrEqualTo` vallen samen met hun lange naam; `BeAfter`/`BeOnOrAfter`/`BeBefore`/`BeOnOrBefore` (TAAK-095) worden `ShouldBeGreaterThan`/`…OrEqualTo`/`ShouldBeLessThan`/`…OrEqualTo` |

## Figuur 2: sequence

**Figure 2.** `Find` geeft elke vergelijking; `FluentAssertionPattern.Rewrite` doet de gedeelde controles en roept `RewriteAssertion` aan. `.And`-ketens splitst `AndChainPattern`.

<!-- kingmadoc:diagram img/fa-be-greater-less-modellen-2.d2 -->
![Figuur 2: sequence](img/fa-be-greater-less-modellen-2.png)

| Stap | Methode in de code | Wat gebeurt er |
| ---- | ------------------ | -------------- |
| Vinden | `Find` | Elke vergelijking (`FindInvocations`) |
| Gedeeld | `FluentAssertionPattern.Rewrite` | AssertionScope, `GetSubject` (`x.Should()`), resultaat weggegooid |
| Type | `RewriteAssertion` → `SemanticModel.GetTypeInfo` | Niet-nullable en `IComparable<T>` van zichzelf |
| Argumenten | `TryGetArguments` | Waarden en `because`; `becauseArgs` met constante `because` → `string.Format(CultureInfo.InvariantCulture, ...)`, anders Manual |
| Waarden | `ConvertExpected` | Cast naar het subjecttype (`(byte)1`); lukt dat niet: Manual |

## Figuur 3: beslisboom

**Figure 3.** Elke vraag in `FluentAssertionPattern.Rewrite` → `RewriteAssertion`; de bladeren zijn de Shouldly-vorm of de Manual-reden, met de proof in de tabel.

<!-- kingmadoc:diagram img/fa-be-greater-less-modellen-3.d2 -->
![Figuur 3: beslisboom](img/fa-be-greater-less-modellen-3.png)

| Blad | Voorwaarde | Proof (`Proofs/FluentAssertions/BeGreaterLess/`) |
| ---- | ---------- | ------------------------------------------------ |
| Fixed, 1 aanroep | losse vergelijking, `because` mee | `simple`, `because`, `because-args`, `in-range`, `sign`, `comparable`, `lambda`, `dates`, `edge-double`, `edge-interpolated-because` |
| Manual "is used" | resultaat gebruikt | `value-used` |
| Fixed met cast | waarde van een ander type dan `byte`/`short`-subject | `byte-short` |
| Manual "nullable or has an unknown type" | `int?`, `DateTime?` | `nullable`, `dates-nullable` |
| Manual "named arguments, or becauseArgs" | named args of niet-constante `because` | `named-because`, `because-variable` (`because-args` is sinds TAAK-097 Fixed) |
| Manual "only exists for" | `BePositive` op `byte` | `sign-byte` |
| Manual "NaN" | `BeNegative` op double/float | `negative-double` |
| Manual "Value type ... differs" | waarde niet naar het subjecttype te converteren | `value-type-differs` |

## Zie ook

- [fa-be-greater-less-plan.md](fa-be-greater-less-plan.md)
- [fa-be-greater-less-verify.md](fa-be-greater-less-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/`
