# Verification: fa-contain

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-contain-plan.md`](fa-contain-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 135f8e5).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/`, gedraaid door `ProofTests`.

| REQ    | Oordeel | Bewijs |
| ------ | ------- | ------ |
| REQ-1  | Gehaald | `list.*.cs`, `enumerable.*.cs` |
| REQ-2  | Gehaald | `predicate.*.cs` |
| REQ-3  | Gehaald | `not-contain.*.cs` |
| REQ-4  | Gehaald | `string.*.cs` (after bevat `Shouldly.Case.Sensitive`, compileert tegen Shouldly) |
| REQ-5  | Gehaald | `because.*.cs` |
| REQ-6  | Gehaald | `lambda.*.cs` |
| REQ-7  | Gehaald | `because-args.before.cs` / `.manual.txt` |
| REQ-8  | Gehaald | `chain.*`, `value-used.*` |
| REQ-9  | Gehaald | `and-chain.*` |
| REQ-10 | Gehaald | `dictionary.*` |
| REQ-11 | Gehaald | `items.*`, `occurrence.*` |
| REQ-12 | Gehaald | `nullable-string.*` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |
| 2 | Behaviour | Substring-check gelijk aan FA | Shouldly is standaard hoofdletterongevoelig; `Case.Sensitive` wordt expliciet toegevoegd | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | passed: 71 of 71 (inclusief alle ProofTests); sample-tests 15 van 15 |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |
