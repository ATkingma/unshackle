# TAAK-166 — Patroon fa.numeric-sign

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: `BeNegative()`/`BePositive()` op alle numerieke typen; `BeNegative` op double/float (was Manual sinds TAAK-101) automatisch als er een exacte vertaling bestaat.

## Context voor de volgende agent

- Rij 27 in `claude/tasks/patroon-queue.md`. `BePositive`, `BeNegative` en `BeInRange` zaten al in `BeGreaterLessPattern` (`fa.be-greater-less`); er is geen apart patroon-ID bijgekomen. Plan, verify en modellen van `fa-be-greater-less` zijn bijgewerkt (REQ-16).
- Reden van de Manual uit TAAK-101: NaN (zie hieronder).

## Probe (FA 7.2.2 vs Shouldly 4.3.0; scratchpad `probe166/`)

| Waarde | FA BeNegative | ShouldBeNegative | ShouldBeLessThan(0) | `x < 0` | BePositive FA = Shouldly |
|---|---|---|---|---|---|
| NaN (double en float) | faalt | **slaagt** | **slaagt** | onwaar | ja (beide falen) |
| -0.0 / 0.0 | faalt | faalt | faalt | onwaar | ja |
| ±Epsilon, ±Infinity, ±1 | gelijk | gelijk | gelijk | gelijk | ja |

int, long, short en decimal (-1, 0, 1): alle gelijk. `byte` heeft geen Shouldly-overload (blijft Manual).

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | `BeNegative` op double/float → `(x < 0).ShouldBeTrue([because])` | `ShouldBeNegative` en `ShouldBeLessThan(0)` slagen op NaN (CompareTo ordent NaN onder alles); `x < 0` is onwaar voor NaN en -0.0 en gelijk voor de rest: pass/fail exact gelijk |
| 2 | Subject tussen haakjes als het geen naam/member/aanroep/index is | `(a - b) < 0` en voorrang |
| 3 | Literal `0` (int) | Converteert impliciet; vergelijking blijft in het subjecttype |
| 4 | `BePositive` blijft `ShouldBePositive()` | In beide bibliotheken identiek voor alle probe-waarden |
| 5 | Nadeel gedocumenteerd | Foutmelding van `ShouldBeTrue` bevat de waarde niet |
| 6 | `BeInRange` ongewijzigd | Al gedekt (proof `in-range`) |

## Proofs

`BeGreaterLess/negative-double` (was `.manual.txt`, nu before+after), nieuw `negative-float` (float met because, samengestelde subject), bestaand `sign`, `sign-byte`.

## Resultaat

`dotnet build` 0 warnings; ProofTests groen (653). Let op: `kingmadoc` ontbrak op deze machine, dus de PNG/SVG van figuur 3 zijn niet opnieuw gerenderd; alleen de `.d2` en de tabel in het modellen-document zijn bijgewerkt.

## Zie ook

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs`
- `docs/projectinfo/patterns/fa-be-greater-less-plan.md`
- `claude/tasks/archief/TAAK-101-fa-direct-mappings.md`
