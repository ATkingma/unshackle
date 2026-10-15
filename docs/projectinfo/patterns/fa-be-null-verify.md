# Verification: fa-be-null

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-null-plan.md`](fa-be-null-plan.md) |
| **Status**    | Deviations found                                          |
| **Generated** | 2026-10-10 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 549bc56).
> Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/`, run by `ProofTests`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `be-null.before.cs` / `be-null.after.cs` |
| REQ-2 | Met | `not-be-null.before.cs` / `not-be-null.after.cs` |
| REQ-3 | Met | `because.before.cs` / `because.after.cs` |
| REQ-4 | Met | `nullable-value.before.cs` / `nullable-value.after.cs` (`int?`, `bool?`; after compiles against Shouldly) |
| REQ-5 | Met | `collection.*.cs` (List, array), `lambda.*.cs` |
| REQ-6 | Replaced | By Extension TAAK-097: `because-args` is now Fixed |
| REQ-7 | Met | `chain.*` (`.And`), `value-used.*` (variabele) |
| REQ-8 | Met | `and-chain.before.cs` / `and-chain.manual.txt` |
| Extension TAAK-269 | Met | `generic-*.before.cs` / `generic-*.after.cs`: unconstrained `T` is boxed (`((object?)x).ShouldNotBeNull()`, CS0452; `object?` because `T` may be null, CS8600 in blind round 2); `T : class` stays plain (`generic-class-constraint.*`) |
| Extension TAAK-097 | Met | `because-args.before.cs` / `because-args.after.cs` (`string.Format(...)`) |
| Extension TAAK-100 | Met | Gedeelde check in `FluentAssertionPattern.Rewrite`; proofs in `AssertionScope/` |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Architecture | `.Which` as a separate Manual proof | `.Which` does not exist on `NotBeNull` of `StringAssertions`/`ObjectAssertions` (FA 7.2.2): the `before` did not compile; `chain` (`.And`) and `value-used` cover the same Manual branch | Low |
| 2 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because`; a documented difference | Low |
| 3 | Scope | _(edge case)_ | `a?.B.Should().BeNull()` → `a?.B.ShouldBeNull()` (TAAK-041, `edge-null-conditional.*`). Same semantics: with `a == null` FA and Shouldly both skip the check; also for `NotBeNull`. Expression-bodied void members are rewritten (TAAK-040, `edge-expression-bodied.*`) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed: 46 of 46 (including all ProofTests); SampleShop solution 15 of 15 |
| Lint  | _none_         | _not run_: no lint command configured   |
