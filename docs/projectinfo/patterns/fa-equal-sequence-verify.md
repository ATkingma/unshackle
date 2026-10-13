# Verification: fa-equal-sequence

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-equal-sequence-plan.md`](fa-equal-sequence-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs`; registratie in `FluentAssertionsFix.cs` (ook als schakel van `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/EqualSequence/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple`, `params-array` |
| REQ-2 | Gehaald | `params` |
| REQ-3 | Gehaald | `because` |
| REQ-4 | Gehaald | `nullable` |
| REQ-5 | Gehaald (becauseArgs vervangen) | `not-equal`, `dictionary`, `predicate`, `assertion-scope`, `and` |
| REQ-6 | Gehaald | `AndChain/equal-length-approximately` |
| Uitbreiding TAAK-225 | Gehaald | `dictionary`, `dictionary-because` (Fixed); `dictionary-class-values`, `dictionary-pair-list` (Manual); dotnet build + test groen (2026-10-13) |
| Uitbreiding TAAK-097 | Gehaald | `because-args.after.cs` |

## Aanvulling TAAK-113

Een collection expression als enig argument (`Equal(["a", "b"])`) is de reeks zelf, geen params-element: →
`ShouldBe(new string[] { "a", "b" })`. Met een spread-element → Manual. Proofs: `collection-expression.*`, `collection-expression-spread.*`.

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

## Zie ook

- [fa-equal-sequence-modellen.md](fa-equal-sequence-modellen.md)
- `claude/docs/patroon-loop.md`
