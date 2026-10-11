# TAAK-021 — Nullable subject breekt build na fa.have-count en fa.contain (string)

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Gevonden in**: TAAK-019 stap 1, casus microsoft/PowerApps-Tooling @ 630e6973

## Probleem

In een project met `<Nullable>enable</Nullable>` + warnings-as-errors levert de fix code op die niet bouwt. FluentAssertions' `Should()` accepteert een nullable subject; de Shouldly-vorm niet.

1. **fa.have-count** — `x.Should().HaveCount(n)` → `x.Count.ShouldBe(n)`. Is `x` nullable (`Dictionary<..>?`), dan CS8602.
   Voorbeeld: `src/Persistence.Tests/PaYaml/Serialization/PaYamlSerializerTests.cs:150,156,195-197`
   (`screen.Properties.Count.ShouldBe(...)`, `componentDefinition.Children.Count.ShouldBe(...)`).
2. **fa.contain (string)** — `s.Should().Contain("x")` → `s.ShouldContain("x", Shouldly.Case.Sensitive)`. `ShouldContain(string actual, ...)` heeft een niet-nullable `actual`; bij `string?` (bv. `Exception.StackTrace`) CS8604.
   Voorbeeld: `src/Persistence.Tests/Compression/PaArchiveTests.cs:335,389,390`.

8 unieke bouwfouten in totaal (x3 target frameworks).

## Aanpak (voorstel)

Gebruik het semantische model: is het subject nullable-annotated (`NullableAnnotation.Annotated` / flow-state MaybeNull), herschrijf dan met `x.ShouldNotBeNull().Count.ShouldBe(n)` resp. `x.ShouldNotBeNull().ShouldContain(...)`; dat behoudt ook FA's falen op null. Alternatief: Manual met reden. Proof per geval in `src/Unshackle.Tests/Proofs/FluentAssertions/<Pattern>/` met nullable enable.

## Stappen

- [x] Proofs (before/after) met nullable subject voor beide patronen
- [x] Fix in `HaveCountPattern.cs` en `ContainPattern.cs` (`src/Unshackle.Core/.../Patterns/`)
- [x] Controle op andere patronen die het subject als niet-nullable argument doorgeven (be-empty, all-satisfy, contain-key, only-contain)
- [ ] Casus PowerApps-Tooling opnieuw: Persistence.Tests bouwt (niet gedaan in deze sessie: hoort bij TAAK-019)

## Keuzes

- **ShouldNotBeNull-keten, geen Manual.** `FluentAssertionsSyntax.NotNull(subject, model)` zet `.ShouldNotBeNull()` voor de receiver als `GetTypeInfo(subject).Nullability.FlowState == MaybeNull`. Reden: het blijft automatisch, het gedrag blijft gelijk (FA faalt ook op null) en Shouldly's `ShouldNotBeNull` heeft `[NotNull]`/geeft `T` terug. Flow state i.p.v. annotatie: na een null-check of in een nullable-disabled project is de flow state niet MaybeNull, dus geen ruis.
- **Toegepast in** have-count, contain en contain-key. Be-empty, only-contain en all-satisfy hebben een nullable proof zonder wijziging: Shouldly's `ShouldBeEmpty`/`ShouldNotBeEmpty` accepteren `IEnumerable<T>?` en markeren het subject daarna als niet-null, dus ook de `foreach`/`ShouldAllBe` erna waarschuwt niet.
- **Bewaking**: `TestSolution.CompileErrors` telt nu ook CS8602/CS8604 mee, zodat elke after-proof met `#nullable enable` faalt op deze waarschuwingen (unittest `FluentAssertionsSyntaxTests`).
- Niet gedaan: de have-count-tak met een niet-veilige receiver (haakjes) krijgt geen `ShouldNotBeNull`; zo'n expressie is zelden MaybeNull.

## Zie ook

- `claude/tasks/TAAK-019-lancering.md` — casus-resultaten
- `docs/projectinfo/patterns/`
