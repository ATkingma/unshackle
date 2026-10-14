# Verification: fa-string-start-end

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-string-start-end-plan.md`](fa-string-start-end-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis beaa9c6).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/StringStartEndPattern.cs`; registration in `FluentAssertionsFix.cs` (also as a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/StringStartEnd/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple` |
| REQ-2 | Met | `equivalent-of` |
| REQ-3 | Met | `because` |
| REQ-4 | Met | `null-or` |
| REQ-5 | Met | `nullable`, `edge-member-subject` |
| REQ-6 | Met (becauseArgs part replaced) | `assertion-scope`, `and`, `collection` |
| TAAK-097 | Met | `because-args.after.cs` (`string.Format`) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (432 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## See also

- [fa-string-start-end-modellen.md](fa-string-start-end-modellen.md)
- `claude/docs/patroon-loop.md`
