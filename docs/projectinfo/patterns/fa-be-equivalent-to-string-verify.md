# Verification: fa-be-equivalent-to-string

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-equivalent-to-string-plan.md`](fa-be-equivalent-to-string-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 41ff4a3).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToStringPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/`, gedraaid door `ProofTests`.

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `simple.*.cs`, `edge-interpolated.*.cs` (after compileert tegen Shouldly) |
| REQ-2 | Gehaald | `because.*.cs` |
| REQ-3 | Gehaald | `lambda.*.cs` |
| REQ-4 | Gehaald | `not-equivalent.*` |
| REQ-5 | Vervangen | door REQ-8 (TAAK-097): `because-args` is nu Fixed |
| REQ-6 | Gehaald | `chain.*`, `value-used.*` |
| REQ-7 | Gehaald | `and-chain.*` |
| REQ-8 | Gehaald | `because-args.*.cs` (`string.Format`) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |
| 2 | Coverage | Objecten/collecties blijven ongemoeid | Alleen `StringAssertions` wordt gevonden; geen eigen proof (ProofTests eist een vondst), gedekt door de symboolfilter in `Find` | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | passed: 98 of 98; sample-tests 15 van 15 |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |
