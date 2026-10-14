# Verification: automapper-value-resolver

|               |                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-value-resolver-plan.md`](automapper-value-resolver-plan.md) |
| **Status**    | Verified                                                                                           |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                                 |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ValueResolverPattern.cs`, `ValueResolvers.cs`, `CreateMapPattern.cs`, `ForMemberMapFromPattern.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/ValueResolver/`, `_Combined/value-resolver-map-call`. Unit tests: `src/Unshackle.Tests/Core/ValueResolverPatternTests.cs`.

| REQ   | Verdict | Evidence                                                                                                  |
| ----- | ------- | --------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | `ValueResolver/resolver-class` (`MapFrom<R>()` with `TDestination` `object`, `MapFrom(new R())`), `Rewrite_PlainResolver_CallsResolve` |
| REQ-2 | Met     | `ValueResolver/member-resolver` (two `IMemberValueResolver`s, renamed lambda parameter)                    |
| REQ-3 | Met     | `ValueResolver/lambda` (`(s, d)`, `(src, dest, member, ctx)`, `(s, _)`; `int` → `string` and `long`)       |
| REQ-4 | Met     | `_Combined/value-resolver-map-call`                                                                       |
| REQ-5 | Met     | `ValueResolver/di-resolver`                                                                               |
| REQ-6 | Met     | `ValueResolver/context-items`, `ValueResolver/lambda-destination`, `Rewrite_UnsafeResolver_ReportsManual` (destination, member value, `ctx`, `this`) |
| REQ-7 | Met     | `Rewrite_UnsafeResolver_ReportsManual` (fields, explicit implementation, constructor with a statement)    |
| REQ-8 | Met     | `Rewrite_ResolverWithReverseMap_ReportsManual`, `Find_ResolverForm_IsNotClaimedByMapFromPattern`          |

`dotnet build` (warnings = errors), `dotnet test` (1303 passed) and the SampleShop tests (38 passed) are green.

## Deviations

- The proof `ForMemberMapFrom/resolver-form` (a `(s, d) => "x"` lambda, Manual) is gone: that profile is now claimed by this pattern and rewritten; `ValueResolver/lambda` covers the shape.
- The resolver class keeps implementing AutoMapper's interface (see plan, Design).

## See also

- [`automapper-value-resolver-plan.md`](automapper-value-resolver-plan.md), [`automapper-value-resolver-modellen.md`](automapper-value-resolver-modellen.md)
