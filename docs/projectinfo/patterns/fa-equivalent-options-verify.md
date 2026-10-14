# Verification: fa-equivalent-options

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-equivalent-options-plan.md`](fa-equivalent-options-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 by hand                                             |

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs` (+ a small change in `BeEquivalentToObjectPattern.cs`: `Find` skips options, helpers protected), registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/EquivalentOptions/`, run by `ProofTests`. The old proof `BeEquivalentToObject/options` moved here (`excluding`).

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `strict-ordering`, `strict-ordering-lambda-parenthesized`, `-because`, `-because-args`, `-strings`, `-record`, `-record-struct`, `-struct`, `-with-missing-members`, `without-then-strict` |
| REQ-2 | Met | `without-strict-ordering`, `strict-then-without`, `strict-ordering-object`, `excluding-missing-members`, `throwing-on-missing-members`, `lenient-collection-ints` |
| REQ-3 | Met | `strict-ordering-class`, `-record-unsealed`, `-record-computed`, `-record-list-member`, `-item-types` |
| REQ-4 | Met | `excluding`, `excluding-with-strict`, `excluding-two`, `including`, `using-comparer`, `comparing-by-value`, `respecting-runtime-types`, `with-tracing`, `collection-excluding` |
| REQ-5 | Met | `excluding-missing-members-other-type` |
| REQ-6 | Met | `block-body`, `options-variable` |

Outcome: `dotnet build` 0 warnings; `dotnet test` 839 passed, 35 failed (the known CRLF failures on a clean main: XmlProjectFileStoreTests, FixerTests, CommandTests, 2 SampleShopTests); all ProofTests green.

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |
