# Verification: fa-be-empty

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-empty-plan.md`](fa-be-empty-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (base 810e68c).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/`, run by `ProofTests`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `be-empty-list.before.cs` / `.after.cs`; also `nullable`, `edge-linq-subject`, `edge-await-subject` |
| REQ-2 | Met | `not-be-empty-list.before.cs` / `.after.cs` |
| REQ-3 | Met | `string.before.cs` / `.after.cs` |
| REQ-4 | Met | `dictionary.*.cs`, `enumerable.*.cs` (IEnumerable, array); after compiles against Shouldly |
| REQ-5 | Met | `because.before.cs` / `.after.cs` |
| REQ-6 | Met | `lambda.before.cs` / `.after.cs` |
| REQ-7 | Replaced | by REQ-10 (TAAK-097): `because-args` is now Fixed |
| REQ-8 | Met | `chain.*`, `value-used.*` |
| REQ-9 | Met | `and-chain.before.cs` / `.manual.txt` |
| TAAK-114 | Met | `guid.*` (`Guid`/`Guid?` → `ShouldBe(Guid.Empty)` / `ShouldNotBe(Guid.Empty)`), `guid-nullable-not-empty.*` (Manual: FA also fails on null) |
| REQ-10 | Met | `because-args.before.cs` / `.after.cs` (`string.Format(...InvariantCulture, "because {0}", 1)`) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Failure message equal to FA | Shouldly puts "Additional Info:" before the `because`; documented difference | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed |
| Tests | `dotnet test`  | passed: 57 of 57 (including all ProofTests) |
| Lint  | _none_         | _not run_: no lint command configured |
