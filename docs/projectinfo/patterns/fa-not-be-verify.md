# Verification: fa-not-be

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-not-be-plan.md`](fa-not-be-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis ed2e155).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotBePattern.cs`; registration in `FluentAssertionsFix.cs`; `ConvertExpected` (now public in `FluentAssertionsSyntax`) shared. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/NotBe/`, run by `ProofTests`.

| REQ   | Verdict | Evidence                       |
| ----- | ------- | ------------------------------ |
| REQ-1 | Met | `simple.*.cs`                  |
| REQ-2 | Met | `because.*.cs`                 |
| REQ-3 | Met | `nullable.*.cs`                |
| REQ-4 | Met | `record.*.cs`                  |
| REQ-5 | Met | `constant-cast.*.cs`           |
| REQ-6 | Met | `assertion-scope.*`            |
| REQ-7 | Met (becauseArgs replaced) | `comparer.*` |
| REQ-8 | Met | `chain.*`                      |
| Extension TAAK-097 | Met | `because-args.after.cs` (`string.Format(CultureInfo.InvariantCulture, ...)`) |

Extension TAAK-277: Met, `NotBe/type.*`; probe against FA 7.2.2.

## Deviations

| #   | Area | Plan says              | Code does                                                                  | Impact |
| --- | ---- | ---------------------- | -------------------------------------------------------------------------- | ------ |
| 1   | Code | Only its own files     | `FluentAssertionsSyntax.ConvertExpected` became `internal` to share the conversion | Low    |

## Validation results

| Check  | Command                                                     | Result              |
| ------ | ----------------------------------------------------------- | ------------------- |
| Build  | `dotnet build`                                              | passed (0 warnings) |
| Test   | `dotnet test`                                               | passed (320 tests)  |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests)   |

## See also

- [fa-not-be-modellen.md](fa-not-be-modellen.md)
- `claude/docs/patroon-loop.md`
