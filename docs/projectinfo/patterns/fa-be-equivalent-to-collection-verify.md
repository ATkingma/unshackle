# Verification: fa-be-equivalent-to-collection

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-equivalent-to-collection-plan.md`](fa-be-equivalent-to-collection-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 by hand                                             |

## Requirements

Code: `BeEquivalentToObjectPattern.cs`, `ParamsArraySyntax.cs` (new, also used by `EqualSequencePattern.cs`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `edge-array-literal` |
| REQ-2 | Met | `collection-record`, `collection-struct` |
| REQ-3 | Met | `params-strings`, `edge-collection-params`, `params-multi-line` |
| REQ-4 | Met | `params-array` |
| REQ-5 | Met | `collection`, `collection-record-unsealed`, `collection-record-nested`, `collection-element-types` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| 1 | Array | task: `new[] { ... }` | `new string[] { ... }` | Equal to `fa.equal-sequence`; type explicit, also with `null` items |
| 2 | Corpus harness | task step 3: FluentResults/CSFE harness | not run | The shallow clones were removed; semantics established with a probe (FA 7.2.2 vs Shouldly 4.3.0, 13 cases equal) |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Proofs | `dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"` | all BeEquivalentToObject/EqualSequence proofs green; failures elsewhere belong to other tasks (AndChain, MediatR) |
| Full suite | `dotnet test --solution Unshackle.slnx` (copy of the working folder) | 33 known CRLF failures plus other people's half-finished work; none of my proofs |

## See also

- [fa-be-equivalent-to-collection-modellen.md](fa-be-equivalent-to-collection-modellen.md)
