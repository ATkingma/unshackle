---
kingmadoc: 1
feature: "automapper-value-resolver"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ValueResolverPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ValueResolvers.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberMapFromPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/ValueResolver/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/Core/ValueResolverPatternTests.cs
---

# Feature: automapper.value-resolver: MapFrom with a resolver class or a resolver lambda → X = new TResolver().Resolve(source, ...) or X = expr

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-254 (row 20 of the AutoMapper queue).

## One-sentence summary

automapper.value-resolver: a profile whose `ForMember` uses `MapFrom<TResolver>()`, `MapFrom(new TResolver())`, `MapFrom<TResolver, TSourceMember>(s => s.Y)`, `MapFrom((s, d) => expr)` or `MapFrom((s, d, m, ctx) => expr)` gets the same `ToB` method as the other profile patterns, with the resolver called by hand or the lambda body moved, when the resolver only reads the source.

## Scope (in / out)

**In scope**

- `IValueResolver<TSource, TDestination, TDestMember>` through `MapFrom<TResolver>()` or `MapFrom(new TResolver())` → `X = new TResolver().Resolve(source, default!, default!, default!)`.
- `IMemberValueResolver<TSource, TDestination, TSourceMember, TDestMember>` through `MapFrom<TResolver, TSourceMember>(s => expr)` → `X = new TResolver().Resolve(source, default!, expr, default!, default!)`.
- Lambdas `(s, d) => expr` and `(s, d, m, ctx) => expr` whose body only uses `s` → `X = expr` (with `source` for `s`), including the built-in conversions of `MemberValues` for the result.
- Together with `Ignore()`, `MapFrom(s => expr)` and plain members in the same profile.

**Out of scope (Manual)**

- A resolver with constructor parameters (DI), a constructor that does work, a base class, instance fields, generic or abstract; an explicitly implemented or overloaded `Resolve`; a resolver whose source is not in the project.
- A `Resolve` or lambda that uses the destination, the member's current value or the `ResolutionContext` (`context.Items`, `context.Mapper`).
- `ReverseMap()` on a map with a resolver form.
- `MapFrom(variable)`, `MapFrom(new R(arg))`, `MapFrom("Name")` (other forms stay with `ForMemberMapFromPattern`, Manual).

## Design

`ValueResolverPattern` is a `CreateMapPattern` subclass like `ForMemberIgnorePattern` and `ForMemberMapFromPattern`: it only claims profiles (`ValueResolvers.UsesResolver`), and `ForMemberMapFromPattern` stops claiming them. `CreateMapPattern.TryParseOptions` collects resolver forms in a separate dictionary (member → `MapFrom` call); `Problem` asks a `ValueResolvers` instance for those members before the `Ignore`/`MapFrom` options. `ValueResolvers` checks the resolver class (symbol and its `Resolve` syntax) or the lambda and hands the value to `MemberValues.Problem` for conversions and nested maps.

The resolver class itself is not changed: it still implements AutoMapper's interface and takes a `ResolutionContext`, which is fine as long as the package stays (the emptied profiles keep it too). Inlining the `Resolve` body was rejected: it can hold statements and several returns, and the class can be shared by several maps.

## Requirements

- **REQ-1**: WHEN `MapFrom<TResolver>()` or `MapFrom(new TResolver())` names an `IValueResolver` that is safe (REQ-6) THE SYSTEM SHALL assign `new TResolver().Resolve(source, default!, default!, default!)` (proof `ValueResolver/resolver-class`, unit test `Rewrite_PlainResolver_CallsResolve`).
- **REQ-2**: WHEN `MapFrom<TResolver, TSourceMember>(s => expr)` names a safe `IMemberValueResolver` THE SYSTEM SHALL pass `expr` (with `source` for `s`) as the source member (proof `ValueResolver/member-resolver`).
- **REQ-3**: WHEN a `(s, d) => expr` or `(s, d, m, ctx) => expr` lambda only uses `s` THE SYSTEM SHALL assign `expr` with AutoMapper's conversion of the result (proof `ValueResolver/lambda`: `int` → `string`, `int` → `long`, a discard parameter).
- **REQ-4**: WHEN a `Map<B>(a)` call uses such a map THE SYSTEM SHALL rewrite it to `a.ToB()` (proof `_Combined/value-resolver-map-call`).
- **REQ-5**: IF the resolver has constructor parameters THEN THE SYSTEM SHALL report Manual "takes constructor parameters (AutoMapper gets it from the DI container)" (proof `ValueResolver/di-resolver`).
- **REQ-6**: IF the resolver or lambda uses the destination, the member's current value or the context THEN THE SYSTEM SHALL report Manual naming the parameter (proofs `ValueResolver/context-items`, `ValueResolver/lambda-destination`, unit tests `Rewrite_UnsafeResolver_ReportsManual`).
- **REQ-7**: IF the resolver keeps state in fields, has a constructor that does work, or implements `Resolve` explicitly THEN THE SYSTEM SHALL report Manual (unit tests `Rewrite_UnsafeResolver_ReportsManual`).
- **REQ-8**: IF the map also has `ReverseMap()` THEN THE SYSTEM SHALL report Manual (unit test `Rewrite_ResolverWithReverseMap_ReportsManual`); `ForMemberMapFromPattern` no longer claims resolver profiles (unit test `Find_ResolverForm_IsNotClaimedByMapFromPattern`).

## Assumptions

Measured with AutoMapper 14.0.0 (TAAK-254 **Evidence**):

- A resolver gets the destination object being built (its initializers ran, members configured earlier are set; with `Map(a, existing)` the existing object), the member's current value and a non-null `ResolutionContext`. So only a `Resolve` that ignores those three is equal to a call with `default!`.
- `MapFrom<TResolver>()` creates a new resolver per `Map` call; `MapFrom(instance)` reuses the instance. With no fields and an empty constructor `new TResolver()` per call gives the same result.
- Nothing is null-safe: a `NullReferenceException` in a `(s, d)` lambda, in a resolver or in the source member expression of `MapFrom<TResolver, TSourceMember>` is wrapped in an `AutoMapperMappingException`. The hand-written code throws the inner exception unwrapped (same as for other exceptions); no dereference rule is needed.
- `TDestMember` must be the member type itself (a different type does not compile), so a resolver result needs no conversion; a lambda result gets the built-in conversions (`int` → `string` gave `"1"`).

## See also

- [`automapper-value-resolver-modellen.md`](automapper-value-resolver-modellen.md), [`automapper-value-resolver-verify.md`](automapper-value-resolver-verify.md)
- [`automapper-for-member-map-from-plan.md`](automapper-for-member-map-from-plan.md) — the `s => expr` form
- `claude/tasks/archief/TAAK-254-automapper-value-resolver.md`
