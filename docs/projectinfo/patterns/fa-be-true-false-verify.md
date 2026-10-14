# Verification: fa-be-true-false

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-true-false-plan.md`](fa-be-true-false-plan.md) |
| **Status**    | Deviations found                                          |
| **Generated** | 2026-10-10 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis b1ab588).
> Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/`, run by `ProofTests`.

| REQ   | Verdict                     | Evidence |
| ----- | --------------------------- | ------ |
| REQ-1 | Met                         | `be-true.before.cs` / `be-true.after.cs` |
| REQ-2 | Met                         | `be-false.before.cs` / `be-false.after.cs` (`(a == b).Should().BeFalse()`) |
| REQ-3 | Met                         | `because.before.cs` / `because.after.cs` |
| REQ-4 | Met                         | `nullable.before.cs` / `nullable.after.cs` (`ShouldBe(true)`, `ShouldBe(false, "why")`) |
| REQ-5 | Superseded | By Extension TAAK-097: `because-args` is now Fixed |
| REQ-6 | Met                         | `chain.before.cs` / `chain.manual.txt` |
| REQ-7 | Met                         | `value-used.before.cs` / `value-used.manual.txt` |
| REQ-8 | Met in code, no proof | Type check (`bool`/`bool?`) in `RewriteAssertion`; an unknown type keeps `before` from compiling |
| Extension TAAK-097 | Met | `because-args.before.cs` / `because-args.after.cs` (`string.Format(...)`) |
| Extension TAAK-100 | Met | Shared check in `FluentAssertionPattern.Rewrite`; proofs in `AssertionScope/` |
| Extension TAAK-198 | Met | `own-shouldbe-in-scope.before.cs` / `.after.cs`: own `ShouldBe<T>` in scope, `BeTrue` → `ShouldBeTrue` with `using static Shouldly.ShouldBeBooleanExtensions;`, the existing own `ShouldBe` call stays unambiguous; unit tests `AmbiguousAddedNamespaceTests` (mixed file, namespace variant, fallback on a clashing alias). |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Requirements | REQ-8 is covered | Only guarded in code, no proof (before does not compile) | Low |
| 2 | Architecture | TAAK-003 choice 8: separate Manual for `GetSubject` null | `chain.before.cs` uses `.BeTrue().And.NotBe(false)`: `ProofTests` requires all findings to contain the reason, so a second FA pattern in the chain (`Be`) would give a different reason | Low |
| 3 | Behaviour | Failure message equal to FA | Shouldly puts "Additional Info:" before the `because`; documented difference | Low |

## Validation results

| Check | Command        | Result                                                |
| ----- | -------------- | ----------------------------------------------------- |
| Build | `dotnet build` | passed: 0 warnings, 0 errors                          |
| Tests | `dotnet test`  | passed: 36 of 36 (whole solution, including all ProofTests) |
| Lint  | _none_         | _not run_: no lint command configured                 |
