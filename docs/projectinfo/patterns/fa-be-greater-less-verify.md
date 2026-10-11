# Verification: fa-be-greater-less

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-greater-less-plan.md`](fa-be-greater-less-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/`, gedraaid door `ProofTests`.

| REQ    | Oordeel | Bewijs |
| ------ | ------- | ------ |
| REQ-1  | Gehaald | `simple.*.cs` |
| REQ-2  | Gehaald | `comparable.*.cs` |
| REQ-3  | Gehaald | `in-range.*.cs` |
| REQ-4  | Gehaald | `sign.*.cs` |
| REQ-5  | Gehaald | `because.*.cs`, `sign.*.cs` |
| REQ-6  | Gehaald | `lambda.*.cs` |
| REQ-7  | Gehaald | `and-chain.*.cs` (twee ketens, ook met `because` en drie schakels) |
| REQ-8  | Gehaald | `nullable.*` |
| REQ-9  | Gehaald | `sign-byte.*` |
| REQ-10 | Gehaald | `because-args.*` |
| REQ-11 | Gehaald | `value-used.*`, `chain-used.*`, `chain-lambda.*` |
| REQ-12 | Gehaald | `chain-other.*` |
| REQ-13 | Gehaald | `chain-subject-call.*` |
| REQ-14 | Gehaald | `chain-no-block.*` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Plan | C4-sectie | Vervangen door sectie Modellen met link naar `fa-be-greater-less-modellen.md` (nieuwe regel van de coordinator) | Low |
| 2 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because` | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | passed: 180 of 180 (inclusief alle ProofTests); sample-tests 15 van 15 |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |
