# TAAK-035 — Patroon fa.string-start-end

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `StartWith`/`EndWith`/`NotStartWith`/`NotEndWith` (+ `EquivalentOf`) en `(Not)BeNullOr(Empty|WhiteSpace)` op strings → Shouldly, met dezelfde hoofdlettergevoeligheid.

## Context voor de volgende agent

- Rij 20 in `claude/tasks/patroon-queue.md` (ronde 2).

## Analyse

FA 7.2.2 `StringAssertions`: alle methodes hebben `(expected, because, becauseArgs)`; `StartWith` e.d. zijn case-sensitive, de `EquivalentOf`-varianten niet. Shouldly 4.3.0 (gecontroleerd met reflectie op `Shouldly.dll`): `ShouldStartWith/ShouldEndWith/ShouldNotStartWith(string? actual, string expected, Case caseSensitivity = Case.Insensitive, string? customMessage = null)`; `ShouldNotEndWith` wijkt af: `(expected, Case)` en `(expected, string? customMessage = null, Case caseSensitivity = Case.Insensitive)`. `Should(Not)BeNullOr(Empty|WhiteSpace)(string? actual, string? customMessage = null)`.

## Keuzes

| #   | Keuze                                                                                          | Reden                                                                                                     |
| --- | ---------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| 1   | Niet-`EquivalentOf` krijgt altijd `Shouldly.Case.Sensitive`                                    | Shouldly's default is Insensitive; zonder zou de test stil zwakker worden                                 |
| 2   | `EquivalentOf` zonder `Case`, behalve als `because` volgt (dan `Case.Insensitive` positioneel) | Default is al gelijk; positioneel omdat customMessage na `Case` komt                                      |
| 3   | `ShouldNotEndWith`: volgorde `(expected, because, Case)`                                       | Enige overload-volgorde die met een message bestaat                                                       |
| 4   | `Shouldly.Case` volledig gekwalificeerd                                                        | Gebruikerscode kan een eigen type `Case` hebben (proofs hebben er zelfs een); zelfde als `ContainPattern` |
| 5   | Alleen `StringAssertions` (`ContainingType.Name`)                                              | Collecties hebben ook `StartWith`/`NotBeNullOrEmpty`, zonder string-equivalent → Manual                   |
| 6   | Geen `NotNull(subject)`                                                                        | Shouldly neemt `string?` en faalt zelf op null, zoals FA                                                  |
| 7   | AssertionScope, becauseArgs, `.And`, EquivalentOf met options → Manual                         | Zelfde regels als andere patronen; `.And`-ketens splitst `fa.and-chain`                                   |

## Stappen

- [x] 1. Taak, onderzoek, keuzes
- [x] 2. KingmaDoc-plan (6 REQ)
- [x] 3. `StringStartEndPattern` + registratie in `s_assertions` (ook schakel van `fa.and-chain`)
- [x] 4. Proofs (10 cases in `StringStartEnd/`)
- [x] 5. Build/test groen (432 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/StringStartEndPattern.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/StringStartEnd/`
- `docs/projectinfo/patterns/fa-string-start-end-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-036-fa-contain-single-which.md`
- `docs/projectinfo/patterns/fa-string-start-end-plan.md`
