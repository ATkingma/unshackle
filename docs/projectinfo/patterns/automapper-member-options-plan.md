---
kingmadoc: 1
feature: "automapper-member-options"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberOptions.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/Core/CreateMapPatternTests.cs
---

# Feature: automapper.member-options: Condition, PreCondition and ForPath → conditional assignment and nested object creation

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-255.

## One-sentence summary

automapper.member-options: `ForMember(d => d.X, o => o.Condition(s => cond))` (or `PreCondition`, alone or in a block with `o.MapFrom(s => expr)`) becomes `X = cond ? value : default` when a new destination holds the default value in `X`, and `ForPath(d => d.A.B, o => o.MapFrom(s => expr))` becomes `A = new() { B = expr }`; every form that this cannot reproduce exactly stays Manual with a precise reason.

## Context

Row 13 of the AutoMapper queue. The corpus survey counts `Condition` 26 times in 4 projects (letterbook, cats, skoruba), `ForPath` 22 times in 3 (cats, jhipster, personalasst); `PreCondition`, `NullSubstitute` and `MapFrom("Name")` had no hits. Most `Condition` hits are `opt.Condition(src => src.X != null)` or `!= Guid.Empty` on a member that is otherwise mapped by name; most `ForPath` hits have one member (`dest => dest.Template`), personalasst writes `x => x.Ingredient.Name`.

## Measured behaviour (AutoMapper 14.0.0)

See the Evidence table in `claude/tasks/archief/TAAK-255-automapper-member-options.md`. In short: a false `Condition` leaves the member at the value the destination has (initializer or default for a new object, the existing value for `Map(a, b)`); `Condition` is evaluated after the value is resolved (a `MapFrom` still runs), `PreCondition` before; a condition that dereferences null throws; `Condition((s, d) => ...)` sees the destination before mapping. `ForPath(d => d.A.B, ...)` creates `A` with its parameterless constructor (initializers run) even when the value is null, reuses an existing `A` on `Map(a, b)`, and its `MapFrom` is null-safe like a `ForMember` one.

## Requirements

| REQ   | Requirement |
| ----- | ----------- |
| REQ-1 | `ForMember(d => d.X, o => o.Condition(s => cond))` on a member the map writes becomes `X = cond ? value : default`, where value is what the map writes without the condition (copy, conversion, nested map). |
| REQ-2 | `o => { o.Condition(s => cond); o.MapFrom(s => expr); }` and the same with `PreCondition` become `X = cond ? expr : default`; the condition follows the `MapFrom` rules (no `this`, no profile members, no dereference of a maybe-null value). |
| REQ-3 | The fallback is `default!` for a non-nullable reference type, `default` otherwise; a conditional or lambda value is put in parentheses. |
| REQ-4 | Manual when the destination member has an initializer, a constructor is declared on the destination or a base type, the destination is created through a constructor, or the member is not written (Ignore, no setter): "when the Condition is false AutoMapper keeps that value". |
| REQ-5 | `ForPath(d => d.A, o => o.MapFrom(...))` is a `MapFrom` for `A`; `ForPath(d => d.A.B, o => o.MapFrom(s => expr))` (one or more members of the same `A`) becomes `A = new() { B = expr, ... }` when `A` is settable, its type a class with a public parameterless constructor, and AutoMapper does not map `A` itself (no source member of that name or prefix, no `ForMember` on it). |
| REQ-6 | Manual with a precise reason: a `ForPath` more than one object deep, a `Condition` that reads the destination or the member values, `NullSubstitute`, `UseDestinationValue` and other member options. |
| REQ-7 | A map with a `Condition` or `ForPath` that also needs `MapInto` is Manual (a `Condition` or `ForPath` on the reverse map of a `ReverseMap` that needs `MapInto` too). |
| REQ-8 | `Map<B>(a)` on such a map becomes `a.ToB()` as before. |
| REQ-9 | `ReverseMap().ForMember(d => d.X, o => o.Condition(...))` (or `PreCondition`, with or without `MapFrom`) and `ReverseMap().ForPath(...)` (one member, or `d.A.B`) configure the reverse map `ToA` like a forward map; the rules of REQ-1 to REQ-5 apply with the reverse destination (TAAK-264). A `ForPath` into a member the reversed `MapFrom` also fills is Manual. |
| REQ-10 | A `Condition` on the forward map next to a plain `ReverseMap()` is rewritten for `ToB` only: AutoMapper does not carry a `Condition` over to the reverse map (measured, TAAK-264). |
| REQ-11 | A `ForPath` on the forward map next to `ReverseMap()` is Manual: AutoMapper reverses it (`ForPath(d => d.A.B, MapFrom(s => s.T))` gives `T = source.A.B` in reverse; measured, TAAK-264). |

## Design

- No new pattern class: like the constructor and nested maps, this is a capability of the shared profile rewrite. A new helper `MemberOptions` parses the options and holds conditions and paths; `ProfileSyntax.TryParseOptions` hands it the `ForMember`/`ForPath` calls the old parser does not know; `Problem` writes paths through new `PathProblem` and wraps conditional members in new `ConditionProblem`.
- `Condition` and `PreCondition` give the same code: the value expressions the rewrite moves are side-effect free (the `MapFrom` rules), so it does not matter that AutoMapper resolves the value before a `Condition`.
- Keeping an initializer value is not possible inside an object initializer without repeating it; the rewrite only accepts members whose value before mapping is known to be `default`.

## Out of scope

- `NullSubstitute`, `UseDestinationValue` (0 hits), `Condition` reading the destination, `ForPath` deeper than one object, `MapInto` with these options, and a forward `ForPath` with `ReverseMap`.

## Models

[automapper-member-options-modellen.md](automapper-member-options-modellen.md)

## See also

- [automapper-member-options-modellen.md](automapper-member-options-modellen.md)
- [automapper-member-options-verify.md](automapper-member-options-verify.md)
- `claude/tasks/archief/TAAK-255-automapper-member-options.md`
- [automapper-for-member-map-from-plan.md](automapper-for-member-map-from-plan.md)
