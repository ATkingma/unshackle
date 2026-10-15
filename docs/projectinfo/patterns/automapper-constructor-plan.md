---
kingmadoc: 1
feature: "automapper-constructor"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/Core/CreateMapPatternTests.cs
---

# Feature: automapper.constructor: a destination with one public constructor → new(source.X, ...) { Y = source.Y }

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-251; extended with several constructors, `ConstructUsing` and `MaxDepth` (TAAK-266, REQ-4, REQ-6 and REQ-8 to REQ-11).

## One-sentence summary

automapper.constructor: a `CreateMap<A, B>()` whose destination has no public parameterless constructor but exactly one public constructor (a positional record, a class with one constructor) gets `B ToB(this A source) => new(source.X, ...) { Y = source.Y, ... }`, reproducing AutoMapper 14's measured constructor mapping; every other constructor form stays Manual with a precise reason.

## Context

Before this feature every such destination made its profile Manual ("has no public parameterless constructor"). The corpus survey counts 63 such pairs in 8 projects (records in altinnauth, bitwarden and others), 56 `ConstructUsing` in 5 projects and 211 `ForCtorParam` in 3 (197 in xipona test code).

## Measured behaviour (AutoMapper 14.0.0)

See the Evidence table in `claude/tasks/archief/TAAK-251-automapper-constructor.md`. In short: AutoMapper takes the constructor with the most parameters it can resolve (the first declared on a tie; also over a parameterless one and whatever the accessibility, TAAK-266), fills each parameter from the source member with the same name in any case (with nested maps and conversions), gives an unresolvable optional parameter its default value, then maps the remaining settable members (`set` and `init`) but no member a parameter already filled (the source is read once). `ConstructUsing` runs before member mapping, which overwrites settable members. `ForCtorParam` matches the parameter name in any case.

## Requirements

| REQ   | Requirement |
| ----- | ----------- |
| REQ-1 | A destination class without a public parameterless constructor and with exactly one constructor (the implicit record copy constructor excluded), which is public, is created with `new(arguments)`; each argument is the value of the single readable source member whose name equals the parameter name ignoring case. |
| REQ-2 | Arguments use the same value rules as members (`MemberValues`): plain copy of the same type, a nested map (`source.X?.ToY()`), a collection copy. |
| REQ-3 | Destination members whose name equals a constructor parameter name ignoring case are not assigned again; other settable members follow the existing member rules (object initializer after the arguments; `Ignore()` and `MapFrom` on them work as before). |
| REQ-4 | Manual when the destination has no public constructor, or its only constructor is not public (TAAK-251); several constructors: see REQ-8. |
| REQ-5 | Manual per parameter: no source member (AutoMapper flattens, uses the default or throws), several members differing in case, a ref/out/params parameter, a type AutoMapper converts without a built-in conversion (`MemberConversions`), a `ForMember` on the member the parameter fills. |
| REQ-6 | `ForCtorParam` other than REQ-12 stays Manual (the chain is not a `CreateMap<A, B>()` without options); `ConstructUsing` see REQ-9. |
| REQ-7 | A map that `Map(source, destination)` needs as `MapInto` and that uses a constructor (or `ConstructUsing`) is Manual; `Map<B>(a)` becomes `a.ToB()` as before. |
| REQ-8 | Several constructors (TAAK-266): the constructor AutoMapper 14 takes is the one with the most parameters it can resolve (a source member of the same name in any case, or a default value), over a parameterless one, any accessibility, the first declared on a tie; a public or internal choice gives `new(args)`, a parameterless one `new()`. Manual when no constructor resolves, when a candidate parameter may be filled by flattening or a Get method, matches several members, is configured by a `ForMember`, when the choice is private or protected, or when tied constructors are declared in different files. |
| REQ-9 | `ConstructUsing(s => expression)` (a one-parameter lambda with an expression body that uses only the parameter, static members and types) makes the destination through the expression, then all members are mapped as without it: `new T(args) { ... }` when the expression is a plain `new` without initializer or no member remains, else a block body `T destination = expression; destination.X = ...; return destination;`. Manual for a `ResolutionContext` lambda, a block body, a method group or generic form, a profile constructor parameter, field or private member, `ConstructUsing` after `ReverseMap`, and with a Condition or ForPath. |
| REQ-10 | `MaxDepth(n)` with a constant n of 2 or more is dropped on a map that copies nested objects or collections when no cycle exists (cycles stay Manual in `MemberValues`) and no `ProjectTo` uses the map; `MaxDepth(1)`, a non-constant argument, a `ReverseMap` or `PreserveReferences` stay Manual. |
| REQ-11 | A profile with several constructors stays Manual, with a reason that names the constructor holding the maps and the parameters or fields its lambdas read (a service the caller hands in, which a static `ToB` cannot reach). |
| REQ-12 | `ForCtorParam("p", o => o.MapFrom(s => expr))` (TAAK-270) fills the argument of parameter `p` (any case) with `expr` by the `MapFrom` rules (null-safe chain, conversions; measured on AutoMapper 14.0.0: null-safe like a member's `MapFrom`). Manual when `p` is no parameter of the chosen constructor, when the map is a `ReverseMap` (not measured), has `ConstructUsing` or a parameterless destination, a `ForMember` configures `p` too, or (several constructors) `p` has no single same-named source member, since a `ForCtorParam` may change which constructor AutoMapper picks. |

## Design

- No new pattern class: like nested maps and member lists, this is a capability of the shared profile rewrite. `CreateMapPattern.Problem` returns the creation expression (`new()` or `new(...)`) next to the members; new `ConstructorProblem` builds the arguments; `Extensions` writes the creation expression; `Rewrite` threads it through the map tuple and guards `MapInto`.
- Target-typed `new(...)` keeps the output close to `new() { ... }` and needs no type name.
- TAAK-266: `ConstructorChoice` reproduces AutoMapper's measured choice between several constructors where it is statically decidable; `ConstructUsingProblem` turns the lambda into the creation (the parameter becomes `source`; `BlockMarker` asks `Extensions` for a block body); `UnsupportedOptions.MaxDepthLimit` lets `Rewrite` drop a harmless `MaxDepth`.

## Out of scope

- `ForCtorParam` with another option than `MapFrom`, or next to `ReverseMap`. `ConstructUsing` on the reverse map, with a `ResolutionContext`, or reading the profile's services.
- Optional parameters without a source member (AutoMapper passes the default value; they count as resolvable for the choice but the rewrite stays Manual).
- Struct destinations with constructors (unchanged: a struct always has `new()`).

## See also

- [automapper-constructor-modellen.md](automapper-constructor-modellen.md)
- [automapper-constructor-verify.md](automapper-constructor-verify.md)
- `claude/tasks/archief/TAAK-251-automapper-constructor.md`
- `claude/tasks/archief/TAAK-266-automapper-several-constructors.md`
- [automapper-nested-maps-plan.md](automapper-nested-maps-plan.md)
