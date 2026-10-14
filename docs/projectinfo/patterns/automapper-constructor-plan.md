---
kingmadoc: 1
feature: "automapper-constructor"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
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

> Drawn up from TAAK-251.

## One-sentence summary

automapper.constructor: a `CreateMap<A, B>()` whose destination has no public parameterless constructor but exactly one public constructor (a positional record, a class with one constructor) gets `B ToB(this A source) => new(source.X, ...) { Y = source.Y, ... }`, reproducing AutoMapper 14's measured constructor mapping; every other constructor form stays Manual with a precise reason.

## Context

Before this feature every such destination made its profile Manual ("has no public parameterless constructor"). The corpus survey counts 63 such pairs in 8 projects (records in altinnauth, bitwarden and others), 56 `ConstructUsing` in 5 projects and 211 `ForCtorParam` in 3 (197 in xipona test code).

## Measured behaviour (AutoMapper 14.0.0)

See the Evidence table in `claude/tasks/archief/TAAK-251-automapper-constructor.md`. In short: AutoMapper takes the constructor with the most parameters it can resolve (the first declared on a tie, a public one over a private or protected parameterless one), fills each parameter from the source member with the same name in any case (with nested maps and conversions), gives an unresolvable optional parameter its default value, then maps the remaining settable members (`set` and `init`) but no member a parameter already filled (the source is read once). `ConstructUsing` runs before member mapping, which overwrites settable members. `ForCtorParam` matches the parameter name in any case.

## Requirements

| REQ   | Requirement |
| ----- | ----------- |
| REQ-1 | A destination class without a public parameterless constructor and with exactly one constructor (the implicit record copy constructor excluded), which is public, is created with `new(arguments)`; each argument is the value of the single readable source member whose name equals the parameter name ignoring case. |
| REQ-2 | Arguments use the same value rules as members (`MemberValues`): plain copy of the same type, a nested map (`source.X?.ToY()`), a collection copy. |
| REQ-3 | Destination members whose name equals a constructor parameter name ignoring case are not assigned again; other settable members follow the existing member rules (object initializer after the arguments; `Ignore()` and `MapFrom` on them work as before). |
| REQ-4 | Manual when the destination has several constructors, a non-public one, or none public: "has several constructors or none that is public". |
| REQ-5 | Manual per parameter: no source member (AutoMapper flattens, uses the default or throws), several members differing in case, a ref/out/params parameter, a type AutoMapper converts, a `ForMember` on the member the parameter fills. |
| REQ-6 | `ConstructUsing` and `ForCtorParam` stay Manual (the chain is not a `CreateMap<A, B>()` without options). |
| REQ-7 | A map that `Map(source, destination)` needs as `MapInto` and that uses a constructor is Manual; `Map<B>(a)` becomes `a.ToB()` as before. |

## Design

- No new pattern class: like nested maps and member lists, this is a capability of the shared profile rewrite. `CreateMapPattern.Problem` returns the creation expression (`new()` or `new(...)`) next to the members; new `ConstructorProblem` builds the arguments; `Extensions` writes the creation expression; `Rewrite` threads it through the map tuple and guards `MapInto`.
- Target-typed `new(...)` keeps the output close to `new() { ... }` and needs no type name.
- Only a single constructor is accepted: AutoMapper's choice between several depends on which parameters resolve and on declaration order, which a hand-written method would freeze silently.

## Out of scope

- `ConstructUsing` (needs the overwrite-after-construction semantics, measured) and `ForCtorParam` (mostly test code): a follow-up can reuse `MapFromProblem` for the latter.
- Optional parameters without a source member (AutoMapper passes the default value).
- Struct destinations with constructors (unchanged: a struct always has `new()`).

## See also

- [automapper-constructor-modellen.md](automapper-constructor-modellen.md)
- [automapper-constructor-verify.md](automapper-constructor-verify.md)
- `claude/tasks/archief/TAAK-251-automapper-constructor.md`
- [automapper-nested-maps-plan.md](automapper-nested-maps-plan.md)
