# Verification: fa-equal-sequence

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-equal-sequence-plan.md`](fa-equal-sequence-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs`; registration in `FluentAssertionsFix.cs` (also as a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/EqualSequence/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple`, `params-array` |
| REQ-2 | Met | `params` |
| REQ-3 | Met | `because` |
| REQ-4 | Met | `nullable` |
| REQ-5 | Met (becauseArgs replaced) | `not-equal`, `dictionary`, `predicate`, `assertion-scope`, `and` |
| REQ-6 | Met | `AndChain/equal-length-approximately` |
| Extension TAAK-225 | Met | `dictionary`, `dictionary-because` (Fixed); `dictionary-class-values`, `dictionary-pair-list` (Manual); dotnet build + test green (2026-10-13) |
| Extension TAAK-097 | Met | `because-args.after.cs` |

## Aanvulling TAAK-113

A collection expression as the only argument (`Equal(["a", "b"])`) is the sequence itself, not a params element: →
`ShouldBe(new string[] { "a", "b" })`. With a spread element → Manual. Proofs: `collection-expression.*`, `collection-expression-spread.*`.

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (474 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## See also

- [fa-equal-sequence-modellen.md](fa-equal-sequence-modellen.md)
- `claude/docs/patroon-loop.md`
