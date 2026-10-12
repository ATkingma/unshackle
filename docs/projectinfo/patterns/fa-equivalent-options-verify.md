# Verification: fa-equivalent-options

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-equivalent-options-plan.md`](fa-equivalent-options-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 met de hand                                         |

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs` (+ kleine wijziging in `BeEquivalentToObjectPattern.cs`: `Find` slaat options over, helpers protected), registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/EquivalentOptions/`, gedraaid door `ProofTests`. De oude proof `BeEquivalentToObject/options` is hierheen verhuisd (`excluding`).

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `strict-ordering`, `strict-ordering-lambda-parenthesized`, `-because`, `-because-args`, `-strings`, `-record`, `-record-struct`, `-struct`, `-with-missing-members`, `without-then-strict` |
| REQ-2 | Gehaald | `without-strict-ordering`, `strict-then-without`, `strict-ordering-object`, `excluding-missing-members`, `throwing-on-missing-members`, `lenient-collection-ints` |
| REQ-3 | Gehaald | `strict-ordering-class`, `-record-unsealed`, `-record-computed`, `-record-list-member`, `-item-types` |
| REQ-4 | Gehaald | `excluding`, `excluding-with-strict`, `excluding-two`, `including`, `using-comparer`, `comparing-by-value`, `respecting-runtime-types`, `with-tracing`, `collection-excluding` |
| REQ-5 | Gehaald | `excluding-missing-members-other-type` |
| REQ-6 | Gehaald | `block-body`, `options-variable` |

Uitkomst: `dotnet build` 0 warnings; `dotnet test` 839 geslaagd, 35 gefaald (de bekende CRLF-falers op schone main: XmlProjectFileStoreTests, FixerTests, CommandTests, 2 SampleShopTests); alle ProofTests groen.

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |
