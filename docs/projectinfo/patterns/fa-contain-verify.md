# Verification: fa-contain

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-contain-plan.md`](fa-contain-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 135f8e5).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/`, run by `ProofTests`.

| REQ    | Verdict | Evidence |
| ------ | ------- | -------- |
| REQ-1  | Met | `list.*.cs`, `enumerable.*.cs` |
| REQ-2  | Met | `predicate.*.cs` |
| REQ-3  | Met | `not-contain.*.cs` |
| REQ-4  | Met | `string.*.cs` (after contains `Shouldly.Case.Sensitive`, compiles against Shouldly) |
| REQ-5  | Met | `because.*.cs` |
| REQ-6  | Met | `lambda.*.cs` |
| REQ-7  | Replaced | By Extension TAAK-097: `because-args` is now Fixed |
| REQ-8  | Met | `chain.*`, `value-used.*` |
| REQ-9  | Met | `and-chain.*` |
| REQ-10 | Met | `dictionary.*` |
| REQ-11 | Met | `items.*`, `occurrence.*` |
| REQ-12 | Met | `nullable-string.*` |
| Extension TAAK-097 | Met | `because-args.before.cs` / `because-args.after.cs` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because`; a documented difference | Low |
| 2 | Behaviour | Substring check equal to FA | Shouldly is case-insensitive by default; `Case.Sensitive` is added explicitly | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | passed: 71 of 71 (including all ProofTests); sample tests 15 of 15 |
| Lint  | _none_         | _not run_: no lint command configured   |
