# Verification: fa-assertion-scope

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-assertion-scope-plan.md`](fa-assertion-scope-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 8a05704).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs`, `FluentAssertionsSyntax.IsInsideAssertionScope` / `IsAssertionScope`, eerste check in elke `Rewrite` van `Patterns/*Pattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `Proofs/FluentAssertions/AssertionScope/` en `Proofs/FluentAssertions/ShouldBe/assertion-scope-*`.

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `AssertionScope/using-block` |
| REQ-2 | Gehaald | `AssertionScope/using-var`, `AssertionScope/target-typed` |
| REQ-3 | Gehaald | `AssertionScope/context` |
| REQ-4 | Gehaald | `ShouldBe/assertion-scope-using-block`, `ShouldBe/assertion-scope-nested`, unittest (geneste scopes) |
| REQ-5 | Gehaald | `ShouldBe/assertion-scope-using-var`, unittest (`using var` + dieper blok) |
| REQ-6 | Gehaald | unittest `IsInsideAssertionScope_DependsOnEnclosingScope` (vóór `using var`, na blok, `MemoryStream`-using, geen scope) |

Scan op SampleShop: `AdvancedAssertionTests.cs` r83 `fa.assertion-scope` Manual; r85 `fa.should-be`, r86 `fa.have-count`, r87 `fa.be-greater-less` Manual (waren Fixed).

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Scope | Taak noemde subklassen (`InheritsFrom`) | `AssertionScope` is `sealed` in FA 7.2.2; check is exacte type-vergelijking | None |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | 296/296 groen |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 groen |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |

## Zie ook

- [fa-assertion-scope-plan.md](fa-assertion-scope-plan.md)
- [fa-assertion-scope-modellen.md](fa-assertion-scope-modellen.md)
