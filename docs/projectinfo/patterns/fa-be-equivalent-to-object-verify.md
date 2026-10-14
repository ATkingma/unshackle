# Verification: fa-be-equivalent-to-object

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-equivalent-to-object-plan.md`](fa-be-equivalent-to-object-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 31c568e).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/`, run by `ProofTests`.

| REQ    | Verdict | Evidence |
| ------ | ------- | -------- |
| REQ-1  | Met | `simple.*.cs`, `new-expectation.*.cs`, `record.*.cs`, `nested.*.cs` (after compiles against Shouldly) |
| REQ-2  | Met | `because.*.cs` |
| REQ-3  | Met | `lambda.*.cs` |
| REQ-4  | Met | `not-equivalent.*` |
| REQ-5  | Met | `and-chain.*`, `value-used.*` |
| REQ-6  | Replaced | by REQ-14 (TAAK-097): `because-args` is now Fixed |
| REQ-7  | Met | `options.*` |
| REQ-8  | Met | `anonymous.*`, `different-types.*`, `base-vs-derived.*` |
| REQ-9  | Met | `collection.*`, `collection-member.*` |
| REQ-10 | Met | `object-subject.*`, `object-member.*` |
| REQ-11 | Met | `equals-override.*`, `equals-member.*` |
| REQ-12 | Met | `cyclic.*` |
| REQ-13 | Met | `no-members.*` |
| TAAK-092 | Met | `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `edge-array-literal`, `AndChain/equivalent-collection`; Manual: `collection-element-types`, `collection` |
| REQ-14 | Met | `because-args.*.cs`, `collection-because-args.*.cs` |
| REQ-15 | Met | `edge-collection-params.*` (Manual "is a collection") |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because`; a different message text | Low |
| 2 | Behaviour | Same decision | Shouldly compares runtime types (also nested), FA only the declared members (probe: derived subject, derived nested member and both derived with a different extra member: FA PASS, Shouldly FAIL). User decision (TAAK-049): stays Fixed, with the warning `FixedResult.Warning` in the report (column Reason) and the console count, unless the type and nested class members are `sealed`/struct or both sides are a `new` expression. Unit test `BeEquivalentToObjectPatternTests`, proofs `sealed`, `both-new` | Medium |
| 3 | Coverage | Unknown type gives Manual | The branch "type unknown" (null literal) has no proof: `BeEquivalentTo(null)` does not compile (ambiguous overload) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | passed: 236 of 236; sample tests 15 of 15 |
| Lint  | _none_         | _not run_: no lint command configured   |
