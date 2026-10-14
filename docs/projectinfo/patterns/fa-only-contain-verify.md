# Verification: fa-only-contain

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-only-contain-plan.md`](fa-only-contain-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 31c568e).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OnlyContainPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/OnlyContain/`, run by `ProofTests`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `simple.*.cs` |
| REQ-2 | Met | `enumerable.*`, `array.*`, `dictionary.*` (after compiles against Shouldly) |
| REQ-3 | Met | `member-subject.*.cs` |
| REQ-4 | Met | `because.*.cs` |
| REQ-5 | Replaced | by Extension TAAK-097 |
| REQ-6 | Met | `chain.*`, `used.*` |
| REQ-7 | Met | `and-chain.*` |
| REQ-8 | Replaced | by Extension TAAK-098 |
| REQ-9 | Met | `lambda.*`, `no-block.*` |
| TAAK-097 | Met | `because-args.after.cs` (`string.Format` on both statements) |
| TAAK-098 | Met | `subject-call.after.cs` (`var subject = Load();`) |
| TAAK-100 | Met | `FluentAssertionPattern.Rewrite` (`IsInsideAssertionScope`); no own proof |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Equal semantics on an empty collection | An extra `ShouldNotBeEmpty()` keeps the failure on an empty collection | Low |
| 2 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because`; `null` gives another exception | Low |
| R-A | Structure | Own preamble, own `IsSimple` | REVIEW-A: inherits from `FluentAssertionPattern`; `Find` returns the statement (also outside a block), splitting via `FluentAssertionsSyntax.TryGetSplittableStatement`, subject via `IsSafeToEvaluateTwice` (symbol-based) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | ProofTests for `OnlyContain/` all green (see the report for the total) |
| Lint  | _none_         | _not run_: no lint command configured   |

## See also

- [fa-only-contain-modellen.md](fa-only-contain-modellen.md)
