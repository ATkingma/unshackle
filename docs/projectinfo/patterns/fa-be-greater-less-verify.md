# Verification: fa-be-greater-less

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-greater-less-plan.md`](fa-be-greater-less-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/`, run by `ProofTests`.

| REQ    | Verdict | Evidence |
| ------ | ------- | -------- |
| REQ-1  | Met | `simple.*.cs`, `edge-double.*.cs` |
| REQ-2  | Met | `comparable.*.cs` |
| REQ-3  | Met | `in-range.*.cs` |
| REQ-4  | Met | `sign.*.cs` |
| REQ-5  | Met | `because.*.cs`, `sign.*.cs`, `edge-interpolated-because.*.cs` |
| REQ-6  | Met | `lambda.*.cs` |
| REQ-7  | Met | Since REVIEW-A via `fa.and-chain`: `AndChain/greater-less.*.cs` (two chains, also with `because` and three links) |
| REQ-8  | Met | `nullable.*` |
| REQ-9  | Met | `sign-byte.*` |
| REQ-10 | Replaced | by REQ-17 (TAAK-097): `because-args` is now Fixed |
| REQ-11 | Met | `value-used.*`; a chain in a lambda: `AndChain/lambda.*` |
| REQ-12 | Dropped (REVIEW-A M4) | Chains belong to `fa.and-chain`, which hands every link to its own pattern: a non-comparison in the chain is no longer Manual |
| REQ-13 | Replaced | Via `fa.and-chain` since TAAK-098 Fixed with a hoist: `AndChain/subject-call.*` |
| REQ-14 | Met | Via `fa.and-chain`: `AndChain/no-block.*` |
| REQ-15 | Met (REVIEW-A H1) | `byte-short.*.cs`: values are cast to the subject type (`(byte)1`), otherwise Shouldly infers `T = int` and it does not compile |
| REQ-16 | Met | `negative-double.*`, `negative-float.*` (Fixed `(x < 0).ShouldBeTrue()`) |
| TAAK-169 | Met | `implicit-conversion.before.cs` / `.after.cs`: `ShouldBeGreaterThan((Doc)0)`, `ShouldBeLessThanOrEqualTo((Doc)7, "small")` |
| REQ-17 | Met | `because-args.*.cs` (`string.Format`) |
| TAAK-095 | Met | `dates.*.cs`, `dates-nullable.*` (Manual), `AndChain/dates.*.cs` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Plan | C4 section | Replaced by a Models section with a link to `fa-be-greater-less-modellen.md` (new rule of the coordinator) | Low |
| 2 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because` | Low |
| 3 | Structure | Own `.And` chain logic | REVIEW-A M4: chain logic removed, `fa.and-chain` is the only owner; the pattern inherits from `FluentAssertionPattern` | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | passed: 180 of 180 (including all ProofTests); sample tests 15 of 15 |
| Lint  | _none_         | _not run_: no lint command configured   |
