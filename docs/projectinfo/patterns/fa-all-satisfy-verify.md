# Verification: fa-all-satisfy

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-all-satisfy-plan.md`](fa-all-satisfy-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (base 31c568e).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AllSatisfyPattern.cs`; registration (first in the list) in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/AllSatisfy/`, run by `ProofTests`.

| REQ    | Verdict | Evidence |
| ------ | ------- | -------- |
| REQ-1  | Met | `simple.*.cs`, `edge-expression-lambda.*.cs`, `nullable.*.cs` |
| REQ-2  | Met | `block.*.cs` |
| REQ-3  | Met | `typed-parameter.*.cs` |
| REQ-4  | Met | `member-subject.*.cs` |
| REQ-5  | Met | `because.before.cs` / `.manual.txt` |
| REQ-6  | Met | `because-args.*` (Manual "because message": since TAAK-097 the arguments succeed, but every `because` stays Manual) |
| REQ-7  | Met | `method-group.*` |
| REQ-8  | Met | `return.*`, `async.*` |
| REQ-9  | Replaced | by REQ-13 (TAAK-098): `subject-call` is now Fixed |
| REQ-10 | Met | `used.*`, `and-chain.*` |
| REQ-11 | Met | `not-in-block.*` |
| REQ-12 | Met | `CombinedProofTests` with `_Combined/all-satisfy-be.*` (the sample case: `ShouldNotBeEmpty(); foreach (var o in orders) { o.Total.ShouldBe(20m); }`) and `all-satisfy-other.*`; the order (AllSatisfy first) is pinned by `CombinedProofTests.AllSatisfyPattern_IsFirstInFix`. Previously only proven manually |
| REQ-13 | Met | `subject-call.*.cs` (`var subject = f();` before the check and the loop) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Failure message equal to FA | Loop stops at the first failed element; FA shows all indexes | Low |
| R-A | Structure | Own preamble, own `IsSimple` | REVIEW-A: inherits from `FluentAssertionPattern`; a void lambda body is now "not directly in a block" instead of "is used"; subject via `IsSafeToEvaluateTwice` (symbol-based) | Low |

## Validation results

REQ-12 has been automated since TAAK-018; deviation 2 (only proven manually) has therefore lapsed.

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | AllSatisfy proofs (13) green; total 236/237, the only failure is a proof of another pattern (`BeEquivalentToObject/null-expectation`, not mine) |
| Lint  | _none_         | _not run_: no lint command configured |

## See also

- [fa-all-satisfy-modellen.md](fa-all-satisfy-modellen.md)
