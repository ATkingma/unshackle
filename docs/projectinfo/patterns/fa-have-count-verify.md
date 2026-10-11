# Verification: fa-have-count

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-have-count-plan.md`](fa-have-count-plan.md)     |
| **Status**    | Deviations found                                                        |
| **Generated** | 2026-10-10T23:35+02:00 by KingmaDoc skill 1.1.0                             |

> Verified by an AI agent against the code at uncommitted changes (basis b1ab588).
> Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/`, gedraaid door `ProofTests`.

| REQ    | Oordeel | Bewijs |
| ------ | ------- | ------ |
| REQ-1  | Gehaald | `list.before.cs` / `list.after.cs` |
| REQ-2  | Gehaald | `list-because.before.cs` / `list-because.after.cs` |
| REQ-3  | Gehaald | `collection-interfaces.before.cs` / `.after.cs` (`HasIntProperty` zoekt ook in `AllInterfaces`) |
| REQ-4  | Deels   | `array.before.cs` / `array.after.cs` bewijst arrays; de `Length`-tak voor `ImmutableArray<T>` is gecodeerd maar zonder proof |
| REQ-5  | Gehaald | `enumerable.before.cs` / `enumerable.after.cs` (`Count()` + `System.Linq`) |
| REQ-6  | Gehaald | `enumerable-existing-using.before.cs` / `.after.cs` |
| REQ-7  | Gehaald | `enumerable-because.before.cs` / `.after.cs` |
| REQ-8  | Gehaald | `receiver-parens.before.cs` / `.after.cs` (`IsSafeReceiver`) |
| REQ-9  | Gehaald | `dictionary.before.cs` / `dictionary.manual.txt` |
| REQ-10 | Gehaald | `predicate.before.cs` / `predicate.manual.txt` (eerste parameter van het resolved symbool is geen `int`) |
| REQ-11 | Gehaald | `because-args.before.cs` / `because-args.manual.txt` |
| REQ-12 | Gehaald | `chain.before.cs` / `chain.manual.txt` |
| REQ-13 | Gehaald in code, geen proof | Laatste `else` in `Rewrite` (geen generieke `IEnumerable`); FA 7.2.2 laat een before-proof niet compileren |
| REQ-14 | Gehaald in code, geen proof | `subject is ConditionalAccessExpressionSyntax` geeft Manual |
| REQ-15 | Gehaald in code, geen proof | `type is null or IErrorTypeSymbol` geeft Manual |
| REQ-16 | Gehaald | `nullable.*` (after-compileercheck faalt op CS8602/CS8604) |

## Deviations

| #   | Area         | Plan says                                                         | Code does                                                                                                     | Impact |
| --- | ------------ | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- | ------ |
| 1   | Requirements | REQ-13 tot REQ-15 en de `ImmutableArray<T>`-tak van REQ-4 zijn gedekt | Gecodeerd in `HaveCountPattern.cs`, maar geen proof; TAAK-002 noemt dit zelf (non-generic proof vervallen) | Low    |
| 2   | Scope        | TAAK-002 keuze 4: alleen een 1-dimensionale array wordt `Length` | `type is IArrayTypeSymbol` (`HaveCountPattern.cs`) neemt ook multidimensionale arrays mee; `Length` is dan het totaal aantal elementen, zoals FA telt | Low    |
| 3   | Architecture | TAAK-002 "Hoe" 1: symbool in `FluentAssertions.Collections.*`     | `FindInvocations` accepteert elke `FluentAssertions`-namespace (`FluentAssertionsSyntax.cs:18`); de parameter-test in `Rewrite` vangt de rest | Low    |
| 4   | Open question | "Kan een pattern een using toevoegen?" was open                  | Opgelost: `PatternResult.Fixed(node, "System.Linq")` (bewijs: `enumerable`-proofs)                        | Low (opgelost) |

## Validation results

| Check | Command        | Result                                                             |
| ----- | -------------- | ------------------------------------------------------------------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors                                       |
| Tests | `dotnet test`  | passed: 29 of 29 (hele solution, inclusief alle ProofTests)        |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld in `.featuredoc.yml`       |

