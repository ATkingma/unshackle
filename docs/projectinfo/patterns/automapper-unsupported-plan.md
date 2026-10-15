---
kingmadoc: 1
feature: "automapper-unsupported"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/UnsupportedOptions.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Core/UnsupportedOptionsTests.cs
---

# Feature: automapper.unsupported: options without effect are dropped, every other unsupported option gets a reason naming the API

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-257.

## One-sentence summary

automapper.unsupported: `MaxDepth`/`PreserveReferences` on a map without nested objects or collections and `ForSourceMember(..., o => o.DoNotValidate())` are dropped; `AfterMap`, `BeforeMap`, `Include*`, `ForAllMembers`, unsupported `ForMember` options, profile settings and the like stay Manual with a reason that names the API and what to write by hand.

## Scope (in / out)

**In scope**

- Options in the chain of a `CreateMap<A, B>()` statement in a profile constructor, and profile settings in that constructor.

**Out of scope**

- Rewriting `AfterMap`/`BeforeMap` into a block-bodied `ToB` (Manual with the form spelled out; see TAAK-257 Choices).
- `IMapFrom<T>` + `Mapping(Profile)` (not a profile constructor), `MapperConfiguration` with inline maps (row 18).

## Design

`UnsupportedOptions` (internal static) holds the knowledge: `IsDepthOption`, `IsDoNotValidate`, `DepthOption` and `Reason`. `CreateMapPattern.TryParseOptions` skips the droppable calls anywhere between the `ForMember` calls; `Rewrite` turns a depth option into Manual when `Problem` reports a copied nested object or collection, or the map has a `ReverseMap`; the generic Manual asks `UnsupportedOptions.Reason` first.

## Requirements

- **REQ-1**: WHEN a map without nested objects or collections carries `MaxDepth(constant)` THE SYSTEM SHALL write the same `ToB` as without it (proof `CreateMap/max-depth`, unit test `Rewrite_OptionWithoutEffect_SameAsWithout`).
- **REQ-2**: WHEN such a map carries `PreserveReferences()` THE SYSTEM SHALL write the same `ToB` as without it (proof `CreateMap/preserve-references`).
- **REQ-3**: WHEN a map carries `ForSourceMember(s => s.X, o => o.DoNotValidate())` THE SYSTEM SHALL drop it (proof `CreateMap/for-source-member-do-not-validate`).
- **REQ-4**: IF `MaxDepth`/`PreserveReferences` sits on a map that copies a nested object or collection, has a `ReverseMap`, or `MaxDepth` has a non-constant argument THEN THE SYSTEM SHALL report Manual naming the option (proof `CreateMap/max-depth-nested`, unit tests).
- **REQ-5**: IF the chain holds `AfterMap`, `BeforeMap`, `Include`, `IncludeBase`, `IncludeAllDerived`, `ForAllMembers`, `ForCtorParam` (except `MapFrom` on a constructor parameter, see automapper-constructor REQ-12), `ConstructUsing`, `ConvertUsing` or another listed API THEN THE SYSTEM SHALL report Manual naming it and what to write by hand (proofs `after-map`, `before-map`, `for-all-members`, `construct-using`, `for-ctor-param`, `reverse-map-options`).
- **REQ-6**: IF a `ForMember` uses an option the rewrite does not read (`NullSubstitute`, `UseDestinationValue`, string member name, ...) THEN THE SYSTEM SHALL report Manual naming that option (proofs `ForMemberIgnore/string-name`, `ForMemberIgnore/string-name` only (Condition, ForPath, ConvertUsing and resolver MapFrom are supported on main since TAAK-253/254/255)).
- **REQ-7**: IF the profile constructor sets a profile setting (`AllowNullCollections`, `ShouldMapProperty`, naming conventions) or calls `RecognizePrefixes` and the like THEN THE SYSTEM SHALL report Manual naming it (proof `profile-setting`, unit tests).

## Assumptions

- Measured with AutoMapper 14.0.0 (TAAK-257 **Evidence**): on flat maps `MaxDepth(0|1)` and `PreserveReferences()` change nothing; `DoNotValidate` only steers `AssertConfigurationIsValid`.

## Risks

- A statement with several unsupported APIs names only the first (innermost) one; the user sees the rest when writing the method.

## Models

Classes, sequence and decision tree: [automapper-unsupported-modellen.md](automapper-unsupported-modellen.md).

## Open questions

- Whether `AfterMap((s, d) => stmt)` deserves a rewrite of its own (18 / 4 in the corpus).

## See also

- [automapper-unsupported-verify.md](automapper-unsupported-verify.md)
- `claude/tasks/archief/TAAK-257-automapper-unsupported.md`
