---
kingmadoc: 1
feature: "automapper-for-member-map-from"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberMapFromPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/
---

# Feature: automapper.for-member-map-from: ForMember(d => d.X, o => o.MapFrom(s => expr)) → X = expr in the ToB method

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by KingmaDoc |

> Drawn up from TAAK-156.

## One-sentence summary

automapper.for-member-map-from: a `CreateMap<A, B>().ForMember(d => d.X, o => o.MapFrom(s => expr))` in a `Profile` becomes, just like `automapper.create-map`, a `ToB(this A source)` extension method with `X = expr`, in which the lambda parameter `s` is replaced by `source`.

## Scope (in / out)

**In scope**

- `.ForMember(d => d.X, o => o.MapFrom(s => expr))` directly on `CreateMap<A, B>()`, also together with `Ignore()` and other MapFrom links in the same chain or the same profile.
- `expr` without conversion: type equal to the type of X, or implicitly numeric/nullable.
- `expr` that cannot throw: receivers of `.member` or `[ ]` are `s`, a type, or not null according to the nullable flow.

**Out of scope (Manual)**

- `MapFrom` with a resolver lambda of two or three parameters, a string name, a block lambda or a generic `MapFrom<T>`.
- Expressions that use something from the profile (`this`, instance fields, statics of the profile) or a name `source` that is already something else.
- Expressions that dereference a possibly-null receiver, or where AutoMapper converts (the type differs), or a collection (AutoMapper copies).
- X that B does not have as publicly writable; X configured twice.

## Design

`ForMemberMapFromPattern` inherits from `CreateMapPattern` (like `ForMemberIgnorePattern`) and shares `Rewrite`: a profile is replaced as a whole by `[profile, <Profile>Extensions]`, so Ignore and MapFrom in one profile must share one rewrite. The `Find` split is exclusive: a profile with a MapFrom call (of any form) goes to this pattern, otherwise with `Ignore()` to `ForMemberIgnorePattern`, otherwise to `CreateMapPattern`. The member list of the rewrite now holds `(name, value)`: `source.X` or the rewritten expression, in the order of B's members.

AutoMapper executes a `MapFrom` expression null-safely (a `NullReferenceException` gives the default value) and converts types; a plain assignment does neither. Therefore only an expression that cannot throw and needs no conversion is moved; Roslyn's nullable flow (`TypeInfo.Nullability`) decides whether a receiver is non-null. In code without a nullable context every dereference is therefore Manual.

## Requirements

- **REQ-1**: WHEN a `CreateMap<A, B>()` has a `ForMember(d => d.X, o => o.MapFrom(s => expr))` THE SYSTEM SHALL have the ToB method assign `X = expr` with `s` replaced by `source` (proofs `simple`, `ignore-and-nullable`, unit test `Rewrite_MapFromWithRenamedParameter_UsesSourceInTheExpression`).
- **REQ-2**: WHEN a profile combines Ignore and MapFrom THE SYSTEM SHALL process both in the same ToB method and have the profile found by exactly one pattern (proof `ignore-and-nullable`, unit test `Find_ProfileWithOrWithoutOptions_IsClaimedByOneOfThePatterns`).
- **REQ-3**: IF the expression dereferences a receiver that can be null THEN THE SYSTEM SHALL report Manual (proof `null-deref`, unit test `Rewrite_NullableAnnotatedReceiver_ReportsManual`).
- **REQ-4**: WHEN a receiver is not null according to the nullable flow (non-nullable member, or tested for null first) THE SYSTEM SHALL move the expression (proof `ignore-and-nullable`, unit test `Rewrite_ReceiverCheckedForNull_IsMoved`).
- **REQ-5**: IF the type of the expression is not equal to that of X (apart from implicitly numeric or nullable) THEN THE SYSTEM SHALL report Manual (proof `conversion`).
- **REQ-6**: IF X is a collection THEN THE SYSTEM SHALL report Manual (proof `collection`).
- **REQ-7**: IF the expression uses something that does not exist outside the profile (instance field, `this`) THEN THE SYSTEM SHALL report Manual (proof `outside-profile`).
- **REQ-8**: IF the MapFrom form is not supported (resolver lambda, X twice, X without a public setter) THEN THE SYSTEM SHALL report Manual (proof `resolver-form`, unit tests `Rewrite_MapFromConfiguredTwice_ReportsManual`, `Rewrite_MapFromOnReadOnlyMember_ReportsManual`).

## Assumptions

- `MapFrom(s => expr)` with one parameter is AutoMapper's `Expression<Func<TSource, TMember>>` overload; expression trees do not allow `?.`, so it does not occur.
- A non-nullable reference is not null according to the nullable contract; whoever does not guard that contract gets a too optimistic rewrite here.

## Risks

- Corpus code without a nullable context gets Manual more often (every dereference of a reference type), deliberately: a silent behaviour change is worse.
- An empty source (`s` itself null) gives a NullReferenceException in `ToB`; cf. row 1.

## Models

Class diagram, sequence and decision tree: [automapper-for-member-map-from-modellen.md](automapper-for-member-map-from-modellen.md).

## Open questions

- None.
