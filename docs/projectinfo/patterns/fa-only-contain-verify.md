# Verification: fa-only-contain

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-only-contain-plan.md`](fa-only-contain-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 31c568e).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OnlyContainPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/OnlyContain/`, gedraaid door `ProofTests`.

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `simple.*.cs` |
| REQ-2 | Gehaald | `enumerable.*`, `array.*`, `dictionary.*` (after compileert tegen Shouldly) |
| REQ-3 | Gehaald | `member-subject.*.cs` |
| REQ-4 | Gehaald | `because.*.cs` |
| REQ-5 | Vervangen | door Uitbreiding TAAK-097 |
| REQ-6 | Gehaald | `chain.*`, `used.*` |
| REQ-7 | Gehaald | `and-chain.*` |
| REQ-8 | Vervangen | door Uitbreiding TAAK-098 |
| REQ-9 | Gehaald | `lambda.*`, `no-block.*` |
| TAAK-097 | Gehaald | `because-args.after.cs` (`string.Format` op beide statements) |
| TAAK-098 | Gehaald | `subject-call.after.cs` (`var subject = Load();`) |
| TAAK-100 | Gehaald | `FluentAssertionPattern.Rewrite` (`IsInsideAssertionScope`); geen eigen proof |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Gelijke semantiek op lege collectie | Extra `ShouldNotBeEmpty()` houdt het falen op een lege collectie in stand | Low |
| 2 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; `null` geeft een andere uitzondering | Low |
| R-A | Structure | Eigen preamble, eigen `IsSimple` | REVIEW-A: erft van `FluentAssertionPattern`; `Find` geeft het statement (ook buiten een blok), splitsen via `FluentAssertionsSyntax.TryGetSplittableStatement`, subject via `IsSafeToEvaluateTwice` (symboolgebaseerd) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | ProofTests voor `OnlyContain/` alle groen (zie verslag voor het totaal) |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |

## Zie ook

- [fa-only-contain-modellen.md](fa-only-contain-modellen.md)
