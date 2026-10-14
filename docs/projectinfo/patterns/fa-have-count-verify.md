# Verification: fa-have-count

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-have-count-plan.md`](fa-have-count-plan.md)     |
| **Status**    | Deviations found                                                        |
| **Generated** | 2026-10-10T23:35+02:00 by KingmaDoc skill 1.1.0                             |

> Verified by an AI agent against the code at uncommitted changes (basis b1ab588).
> Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/`, run by `ProofTests`.

| REQ    | Verdict | Evidence |
| ------ | ------- | -------- |
| REQ-1  | Met | `list.before.cs` / `list.after.cs` |
| REQ-2  | Met | `list-because.before.cs` / `list-because.after.cs` |
| REQ-3  | Met | `collection-interfaces.before.cs` / `.after.cs` (`HasIntProperty` also searches `AllInterfaces`) |
| REQ-4  | Partial   | `array.before.cs` / `array.after.cs` proves arrays; the `Length` branch for `ImmutableArray<T>` is coded but has no proof |
| REQ-5  | Met | `enumerable.before.cs` / `enumerable.after.cs` (`Count()` + `System.Linq`) |
| REQ-6  | Met | `enumerable-existing-using.before.cs` / `.after.cs` |
| REQ-7  | Met | `enumerable-because.before.cs` / `.after.cs` |
| REQ-8  | Met | `receiver-parens.before.cs` / `.after.cs` (`FluentAssertionsSyntax.NotNullReceiver`) |
| REQ-9  | Met | `dictionary.before.cs` / `dictionary.manual.txt` |
| REQ-10 | Met | `predicate.before.cs` / `predicate.manual.txt` (first parameter of the resolved symbol is not an `int`) |
| REQ-11 | Replaced | becauseArgs is Fixed since TAAK-097: `because-args.before.cs` / `because-args.after.cs` |
| REQ-12 | Met | `chain.before.cs` / `chain.manual.txt` |
| REQ-13 | Met in code, no proof | Last `else` in `Rewrite` (no generic `IEnumerable`); FA 7.2.2 does not let a before-proof compile |
| REQ-14 | Met in code, no proof | `subject is ConditionalAccessExpressionSyntax` gives Manual |
| REQ-15 | Met in code, no proof | `type is null or IErrorTypeSymbol` gives Manual |
| REQ-16 | Met | `nullable.*` (after compile check fails on CS8602/CS8604) |
| Extension TAAK-095 | Met | `count-comparisons.*`, `count-comparison-because-args.*` |
| Extension TAAK-097 | Met | `because-args.after.cs`, `count-comparison-because-args.after.cs` (`string.Format(CultureInfo.InvariantCulture, ...)`) |

## Deviations

| #   | Area         | Plan says                                                         | Code does                                                                                                     | Impact |
| --- | ------------ | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- | ------ |
| 1   | Requirements | REQ-13 to REQ-15 and the `ImmutableArray<T>` branch of REQ-4 are covered | Coded in `HaveCountPattern.cs`, but no proof; TAAK-002 itself mentions this (non-generic proof dropped) | Low    |
| 2   | Scope        | TAAK-002 choice 4: only a one-dimensional array becomes `Length` | `type is IArrayTypeSymbol` (`HaveCountPattern.cs`) also includes multidimensional arrays; `Length` is then the total number of elements, as FA counts | Low    |
| 3   | Architecture | TAAK-002 "How" 1: symbol in `FluentAssertions.Collections.*`     | `FindInvocations` accepts any `FluentAssertions` namespace (`FluentAssertionsSyntax.cs:18`); the parameter test in `Rewrite` catches the rest | Low    |
| 4   | Open question | "Can a pattern add a using?" was open                  | Resolved: `PatternResult.Fixed(node, "System.Linq")` (evidence: `enumerable` proofs)                        | Low (resolved) |

## Validation results

| Check | Command        | Result                                                             |
| ----- | -------------- | ------------------------------------------------------------------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors                                       |
| Tests | `dotnet test`  | passed: 29 of 29 (whole solution, including all ProofTests)        |
| Lint  | _none_         | _not run_: no lint command configured in `.featuredoc.yml`       |
