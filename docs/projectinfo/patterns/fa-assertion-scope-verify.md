# Verification: fa-assertion-scope

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-assertion-scope-plan.md`](fa-assertion-scope-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (base 8a05704).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs`, `FluentAssertionsSyntax.IsInsideAssertionScope` / `IsAssertionScope`, first check in `FluentAssertionPattern.Rewrite` (and `AndChainPattern.Rewrite`); registration in `FluentAssertionsFix.cs`. Proofs: `Proofs/FluentAssertions/AssertionScope/` and `Proofs/FluentAssertions/ShouldBe/assertion-scope-*`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Replaced | by REQ-7/REQ-8 (TAAK-100); proof `AssertionScope/using-block` no longer exists |
| REQ-2 | Replaced | by REQ-7/REQ-8 (TAAK-100); proofs `using-var`, `target-typed` no longer exist |
| REQ-3 | Met | `AssertionScope/context` |
| REQ-4 | Met | `ShouldBe/assertion-scope-using-block`, `ShouldBe/assertion-scope-nested`, unit test (nested scopes) |
| REQ-5 | Met | `ShouldBe/assertion-scope-using-var`, unit test (`using var` + deeper block) |
| REQ-6 | Met | unit test `IsInsideAssertionScope_DependsOnEnclosingScope` (before `using var`, after block, `MemoryStream` using, no scope) |
| REQ-7 | Met | `_Combined/scope-using-block`, `_Combined/scope-using-var` (Fixed, by `CombinedProofTests`) |
| REQ-8 | Met | `AssertionScope/context`, `discard`, `other-statement`, `await` (Manual "Shouldly has no scope") |

Scan of SampleShop (before TAAK-100): `AdvancedAssertionTests.cs` line 83 `fa.assertion-scope` Manual; line 85 `fa.should-be`, line 86 `fa.have-count`, line 87 `fa.be-greater-less` Manual (were Fixed).

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Scope | Task mentioned subclasses (`InheritsFrom`) | `AssertionScope` is `sealed` in FA 7.2.2; the check is an exact type comparison | None |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | 296/296 green |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 green |
| Lint  | _none_         | _not run_: no lint command configured |

## See also

- [fa-assertion-scope-plan.md](fa-assertion-scope-plan.md)
- [fa-assertion-scope-modellen.md](fa-assertion-scope-modellen.md)
