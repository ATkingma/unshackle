---
kingmadoc: 1
feature: "fa-be-null"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/
---
# Feature: fa.be-null: x.Should().BeNull()/NotBeNull() wordt x.ShouldBeNull()/ShouldNotBeNull()

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-10T23:42+02:00 by KingmaDoc 0.3.0.dev9 |

> Opgesteld uit TAAK-004. Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

fa.be-null: x.Should().BeNull()/NotBeNull() wordt x.ShouldBeNull()/ShouldNotBeNull(), voor referentietypes en `Nullable<T>`.

## Scope (in / out)

**In scope**

- `x.Should().BeNull()` / `NotBeNull()` (met optionele `because` zonder `becauseArgs`) als losse assertion, op referentietypes (string, object, collecties) en `Nullable<T>`.
- `Manual(reden)` voor `becauseArgs`, ketens (`.And`, `.Which`) en gebruikt resultaat.

**Out of scope**

- `NotBeNull().Which...` omzetten naar `var v = x.ShouldNotBeNull()` (Shouldly geeft de waarde terug, maar dat is ketens splitsen).
- `BeNull`/`NotBeNull` op `Action`/`Func`-subjects: FA heeft daar geen `BeNull`.
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `x.Should().BeNull()` op een referentietype staat THE SYSTEM SHALL het herschrijven naar `x.ShouldBeNull()` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/be-null.*.cs`).
- **REQ-2**: WHEN `x.Should().NotBeNull()` op een referentietype staat THE SYSTEM SHALL het herschrijven naar `x.ShouldNotBeNull()` (proof `.../BeNull/not-be-null.*.cs`).
- **REQ-3**: WHEN `BeNull`/`NotBeNull` een `because` zonder `becauseArgs` heeft THE SYSTEM SHALL die als `customMessage` overnemen (proof `.../BeNull/because.*.cs`).
- **REQ-4**: WHEN het subject een `Nullable<T>` is (`int?`, `bool?`) THE SYSTEM SHALL dezelfde `ShouldBeNull`/`ShouldNotBeNull` gebruiken (proof `.../BeNull/nullable-value.*.cs`).
- **REQ-5**: WHEN het subject een collectie of array is THE SYSTEM SHALL het herschrijven (proof `.../BeNull/collection.*.cs`); WHEN de assertion de body van een void-lambda is THE SYSTEM SHALL het herschrijven (proof `.../BeNull/lambda.*.cs`).
- **REQ-6**: IF `becauseArgs` of benoemde argumenten gebruikt worden THEN THE SYSTEM SHALL Manual geven (proof `.../BeNull/because-args.*`).
- **REQ-7**: IF een vervolg op de assertion staat (`.And`, `.Which`) of het resultaat als waarde wordt gebruikt THEN THE SYSTEM SHALL Manual geven (proofs `.../BeNull/chain.*`, `value-used.*`).
- **REQ-8**: IF de assertion zelf een vervolg op een andere assertion is (`x.Should().NotBeEmpty().And.NotBeNull()`) THEN THE SYSTEM SHALL Manual geven (proof `.../BeNull/and-chain.*`).
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als `customMessage` geven (proof `.../BeNull/because-args.after.cs`); benoemde argumenten of een niet-constante `because` blijven Manual. Vervangt REQ-6 voor dit geval.
- **Uitbreiding TAAK-100**: IF de assertion in een `AssertionScope` staat die `fa.assertion-scope` niet omzet THEN THE SYSTEM SHALL Manual geven ("Inside an AssertionScope").

## Assumptions

- Shouldly 4.3.0 heeft `ShouldBeNull<T>(this T? actual, string? customMessage)` en `ShouldNotBeNull<T>(...)` die voor referentietypes en `Nullable<T>` compileren (bewezen door de compileercheck van `nullable-value.after.cs`).
- `ShouldNotBeNull()` geeft de waarde terug; bij een weggegooid resultaat maakt dat niet uit.
- De `Fixer` beheert `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- Foutmelding krijgt "Additional Info:" in plaats van FA's "because"-voorvoegsel: gedocumenteerd verschil.
- Na `x.Should().NotBeNull(); x!.Y` blijft de `!` staan: onschadelijk, Shouldly's `[NotNull]` maakt hem alleen overbodig.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-be-null-modellen.md](fa-be-null-modellen.md).

## Open questions

- Geen. `NotBeNull().Which`-herschrijving kan later een eigen patroon worden.
