# TAAK-010 — Patroon fa.be-greater-less

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `BeGreaterThan`/`BeLessThan`/`...OrEqualTo`/`BeInRange`/`BePositive`/`BeNegative` (+ `.And`-ketens) wordt `ShouldBe...`, keten gesplitst, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 10 in `claude/tasks/patroon-queue.md`. Voorbeelden: TAAK-001 (statement splitsen), TAAK-009. Sample: `OrderTests.cs:84`.

## Analyse

FA 7.2.2 heeft deze methoden op `NumericAssertions<T>`, `ComparableTypeAssertions<T>` en `TimeSpanAssertions`; allemaal `(expected, because, becauseArgs)` met een `AndConstraint`. Shouldly 4.3.0: `ShouldBeGreaterThan<T>(T, string? customMessage) where T : IComparable<T>` (idem LessThan, OrEqualTo, `ShouldBeInRange`, `ShouldNotBeInRange`) en `ShouldBePositive`/`ShouldBeNegative` alleen voor int, long, short, decimal, double, float. `DateTime` heeft in FA `BeAfter`/`BeBefore`: buiten scope.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Scope: 4 vergelijkingen + aliassen `BeGreaterOrEqualTo`/`BeLessOrEqualTo`, `BeInRange`, `NotBeInRange`, `BePositive`, `BeNegative`; vertaaltabel `s_methods` | Zelfde vorm; Shouldly kent ze allemaal |
| 2 | Nullable subject (`int?`) is Manual | `T` wordt `int?`, dat geen `IComparable<int?>` is: compileert niet |
| 3 | Subject moet `IComparable<T>` van zichzelf zijn | Shouldly-constraint; dekt TimeSpan, Version, numeriek |
| 4 | `BePositive`/`BeNegative` alleen voor de zes Shouldly-types, anders Manual | Andere types (byte) hebben geen overload |
| 5 | `Find` klimt langs `.And` naar de bovenste aanroep en geeft bij een keten het hele `ExpressionStatement` | Splitsen vervangt een statement door meerdere (`Fixed(IEnumerable)`) |
| 6 | Keten alleen splitsen als alle schakels vergelijkingen zijn, het statement direct in een blok/switch-sectie staat en het subject een naam of member-access op namen is | Subject wordt n keer geëvalueerd; getters worden als zuiver aangenomen (shortcut) |
| 7 | Elke schakel houdt zijn eigen `because`; `becauseArgs` is Manual | Shouldly kan niet formatteren |
| 8 | Manual-redenen: "is not in the form", "not a comparison", "is used", "not directly in a block", "evaluate it twice", "nullable or has an unknown type", "becauseArgs", "only exists for" | `ProofTests` eist één reden per proof |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan (14 REQ) en modellen-document (3 figuren)
- [x] 4. `BeGreaterLessPattern` + registratie
- [x] 5. Proofs (32 bestanden in `BeGreaterLess/`)
- [x] 6. Build/test groen (180 + 15 sample), verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/`
- `docs/projectinfo/patterns/fa-be-greater-less-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij + kolom Modellen)

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-009-fa-be-equivalent-to-string.md`
