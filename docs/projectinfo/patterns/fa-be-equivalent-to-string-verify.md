# Verification: fa-be-equivalent-to-string

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-equivalent-to-string-plan.md`](fa-be-equivalent-to-string-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 41ff4a3).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToStringPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/`, run by `ProofTests`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `simple.*.cs`, `edge-interpolated.*.cs` (after compiles against Shouldly) |
| REQ-2 | Met | `because.*.cs` |
| REQ-3 | Met | `lambda.*.cs` |
| REQ-4 | Met | `not-equivalent.*` |
| REQ-5 | Replaced | by REQ-8 (TAAK-097): `because-args` is now Fixed |
| REQ-6 | Met | `chain.*`, `value-used.*` |
| REQ-7 | Met | `and-chain.*` |
| REQ-8 | Met | `because-args.*.cs` (`string.Format`) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because`; a documented difference | Low |
| 2 | Coverage | Objects/collections stay untouched | Only `StringAssertions` is found; no own proof (ProofTests requires a finding), covered by the symbol filter in `Find` | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | passed: 98 of 98; sample tests 15 of 15 |
| Lint  | _none_         | _not run_: no lint command configured   |
