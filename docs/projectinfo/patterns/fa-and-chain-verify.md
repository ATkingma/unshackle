# Verification: fa-and-chain

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-and-chain-plan.md`](fa-and-chain-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (base b53e3cc).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AndChainPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/AndChain/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple`, `three-links`, `be-of-type`, `edge-multiline` |
| REQ-2 | Met | `because` |
| REQ-3 | Met | `member-subject`, `nullable` |
| REQ-4 | Replaced | by REQ-8 (TAAK-098): `subject-call`, `subject-indexer` are now Fixed |
| REQ-5 | Met | `unsupported-link`; Manual link without its own proof (`link-manual` no longer exists, `link-because-args` has been Fixed since TAAK-097) |
| REQ-6 | Met | `used`, `which`, `no-block` |
| REQ-7 | Met | `assertion-scope` |
| REQ-8 | Met | `subject-call`, `subject-indexer`, `subject-call-twice` (`subject`, `subject2`) |
| REQ-9 | Met | `be-same-as`, `dates`, `equal-length-approximately`, `equivalent-collection`, `greater-less`, `link-because-args` |
| REQ-10 | Met | `contain-keys`, `contain-keys-and`, `not-contain-keys`, `contain-keys-read-only`, `contain-keys-subject-call` (`subject2`) |
| REQ-11 | Met | `contain-value`, `contain-value-because`, `not-contain-value`, `contain-value-read-only`, `contain-values`, `not-contain-values`, `contain-keys-values` (Polly case) |
| REQ-12 | Met | `whose-value`, `whose-value-because`, `whose-value-other`, `whose-value-and`, `whose-value-read-only`, `whose-value-key-variable`, `whose-value-subject-call` |
| REQ-13 | Met | `contain-keys-collection`, `contain-values-collection`, `whose-value-no-indexer`, `whose-value-computed-key` |
| REQ-14 | Met | `not-empty-only-contain`; `_Combined/litedb-bson-assertions` (TAAK-241) |
| REVIEW-A | Met | `greater-less`, `lambda` (moved from BeGreaterLess); `AndChainPatternTests`: the `Warning` of a link is kept (H2) and a link of several statements (OnlyContain) is taken over (L3) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| 1 | Code | Own files only | `FluentAssertionsFix`: assertion patterns in `s_assertions`, shared with `AndChainPattern`; order unchanged | Low |
| 2 | Proof | - | `.And` type check without proof: no compilable FA 7.2.2 example found | Low |
| R-A | Performance | `Find` over the whole file per link | REVIEW-A M6: `Find` runs on the parent of the link statement | Low |
| 3 | Docs | Own pattern for TAAK-171 | TAAK-171 extends `fa.and-chain` (the split is already there); `ContainKeyPattern` is unchanged | Low |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (412 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## See also

- [fa-and-chain-modellen.md](fa-and-chain-modellen.md)
- `claude/docs/patroon-loop.md`
