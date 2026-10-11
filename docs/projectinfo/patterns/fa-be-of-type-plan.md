---
kingmadoc: 1
feature: "fa-be-of-type"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/BeOfType/
---

# Feature: fa.be-of-type: BeOfType/BeAssignableTo wordt ShouldBeOfType/ShouldBeAssignableTo

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-034.

## One-sentence summary

fa.be-of-type: `BeOfType<T>()`, `BeAssignableTo<T>()`, `NotBeOfType<T>()`, `NotBeAssignableTo<T>()` (ook met `Type`) worden de Shouldly-tegenhangers; `var v = ….Which` wordt `var v = x.ShouldBeOfType<T>()`.

## Scope (in / out)

**In scope**

- De vier methodes, generiek of met `Type`-argument, met optionele `because`.
- `.Which`/`.Subject` als enige initializer van een declaratie.

**Out of scope**

- `.Which.P.Should()...`-ketens (rij `fa.which-chain`).
- `TypeAssertions` (`typeof(T).Should()`).

## Requirements

- **REQ-1**: WHEN `x.Should().BeOfType<T>()` (of een van de andere drie) als statement staat THE SYSTEM SHALL `x.ShouldBeOfType<T>()` enz. geven (proof `simple`).
- **REQ-2**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL die als customMessage meegeven (proof `because`).
- **REQ-3**: WHEN een `Type`-argument wordt gebruikt THE SYSTEM SHALL de niet-generieke Shouldly-overload geven (proof `type-argument`).
- **REQ-4**: WHEN `var v = x.Should().BeOfType<T>().Which` (of `.Subject`) staat THE SYSTEM SHALL `var v = x.ShouldBeOfType<T>()` geven (proof `which`).
- **REQ-5**: WHEN het subject nullable is THE SYSTEM SHALL zonder extra null-check herschrijven (proof `nullable`).
- **REQ-6**: IF het resultaat anders wordt gebruikt (`.And`, `.Which.P`, meerdere declarators) THEN THE SYSTEM SHALL Manual geven (proofs `and`, `which-chain`, `which-multiple`).
- **REQ-7**: IF becauseArgs, een AssertionScope of een `Type`-subject THEN THE SYSTEM SHALL Manual geven (proofs `because-args`, `assertion-scope`, `type-subject`).
- **Uitbreiding TAAK-096**: WHEN `.Which`/`.Subject` verder gebruikt wordt (`….Which.Name`, meerdere declarators) THE SYSTEM SHALL `.Which` vervangen door de Shouldly-aanroep, die de cast-waarde teruggeeft (proofs `which-chain`, `which-multiple`); alleen `.And` en ander gebruik blijven Manual (`and`). Vervangt dat deel van REQ-6.
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als customMessage geven (proof `because-args`); benoemde argumenten of een niet-constante `because` blijven Manual. Vervangt dat deel van REQ-7.

## Assumptions

- Shouldly 4.3.0 heeft `ShouldBeOfType<T>(this object? actual, string? customMessage)` en de `Type`-overloads, die de cast-waarde teruggeven.

## Risks

- Meldingstekst verschilt van FA; gedrag (exact type vs. toewijsbaar) is gelijk.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-be-of-type-modellen.md](fa-be-of-type-modellen.md).
