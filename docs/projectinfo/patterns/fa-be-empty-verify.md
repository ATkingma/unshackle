# Verification: fa-be-empty

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-empty-plan.md`](fa-be-empty-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 810e68c).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/`, gedraaid door `ProofTests`.

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `be-empty-list.before.cs` / `.after.cs` |
| REQ-2 | Gehaald | `not-be-empty-list.before.cs` / `.after.cs` |
| REQ-3 | Gehaald | `string.before.cs` / `.after.cs` |
| REQ-4 | Gehaald | `dictionary.*.cs`, `enumerable.*.cs` (IEnumerable, array); after compileert tegen Shouldly |
| REQ-5 | Gehaald | `because.before.cs` / `.after.cs` |
| REQ-6 | Gehaald | `lambda.before.cs` / `.after.cs` |
| REQ-7 | Gehaald | `because-args.before.cs` / `.manual.txt` |
| REQ-8 | Gehaald | `chain.*`, `value-used.*` |
| REQ-9 | Gehaald | `and-chain.before.cs` / `.manual.txt` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed |
| Tests | `dotnet test`  | passed: 57 of 57 (inclusief alle ProofTests) |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |
