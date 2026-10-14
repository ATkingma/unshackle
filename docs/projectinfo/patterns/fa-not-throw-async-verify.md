# Verification: fa-not-throw-async

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-not-throw-async-plan.md`](fa-not-throw-async-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis bcf1156).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowAsyncPattern.cs`; registration in `FluentAssertionsFix.cs` (`s_assertions`, also as a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrowAsync/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple, lambda, reference-result` |
| REQ-2 | Met | `because` |
| REQ-3 | Met | `configure-await` |
| REQ-4 | Met | `generic, not-awaited, result-used, value-result, awaiting, assertion-scope` |
| Extension TAAK-097 | Met | `because-args.after.cs` (`string.Format(CultureInfo.InvariantCulture, ...)`) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (579 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Corpus (TAAK-170, TAAK-173)

- Polly 7.2.4 (`b1a5a17`, 2026-10-13, harness without clone changes): Fixed 4449 → 5255, Manual 964 → 171, build 0 errors, tests 5685/5685 → 5685/5685, 0 regressions.

## See also

- [fa-not-throw-async-plan.md](fa-not-throw-async-plan.md)
- [fa-not-throw-async-modellen.md](fa-not-throw-async-modellen.md)
- `claude/docs/patroon-loop.md`
