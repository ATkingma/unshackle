---
kingmadoc: 1
feature: "automapper-configuration-test"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ConfigurationTestPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ConfigurationSyntax.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/ConfigurationTest/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/Core/ConfigurationTestPatternTests.cs
---

# Feature: automapper.configuration-test: AssertConfigurationIsValid and test mappers built from profiles are removed

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-250.

## One-sentence summary

automapper.configuration-test: once the profiles are hand-written `ToB` methods, `config.AssertConfigurationIsValid()`, the test methods that only did that, and the `new MapperConfiguration(cfg => cfg.AddProfile<P>())` / `CreateMapper()` variables that are only checked or mapped with are removed; a configuration with its own maps or settings, a check with side effects, and a check while a map outside a profile constructor remains are Manual.

## Scope (in / out)

**In scope**

- `x.AssertConfigurationIsValid()` (and its overloads) as a statement, receiver and arguments without side effects (jason, neozhu, personalasst, letterbook, nop).
- Test methods and setups (xUnit, NUnit, MSTest attributes) and a test class's only parameterless constructor that are left with nothing.
- Locals and private fields typed as an AutoMapper type whose values are built here from a profile-only configuration (or its `CreateMapper()`) and that are only checked, passed through `CreateMapper()` to another such variable, or mapped with (ombi, jhipster).
- Manual: `new MapperConfiguration(...)` that does more than `AddProfile`/`AddProfiles`/`AddMaps` (settings, inline `CreateMap`, a method group, a delegate) or adds a profile from a referenced assembly (gnomeshade, iotsharp, dasblog, xipona); a check with side effects or not a statement of its own (abp, SampleShop's `Action act = () => ...`).

**Out of scope**

- A profile-only configuration whose value escapes (passed on, returned, a public or static property: kavita, skoruba, personalasst's fixture, nop's engine): not reported, it keeps working with the emptied profiles; it goes with the IMapper clean-up and `AutoMapperFix.Package`.
- Injected `IMapper` fields (the Map patterns leave them, TAAK-157).
- Unused usings left behind (`System.Reflection`), and the blank line in front of a member that follows removed ones.

## Design

`ConfigurationTestPattern` (`ISolutionPattern`) runs last in `AutoMapperFix.Patterns`, after `MapCallPattern`/`MapIntoPattern` have rewritten the Map calls in the same file. `ConfigurationSyntax.Find` computes the removable variables (`RemovableSymbols`: candidates, their values and reads, then a fixpoint over the variables a value comes from or flows into), the removable statements, and per test member or lone constructor whether every statement goes (then the member is reported instead of its statements); it adds the non-profile-only configurations and the checks that cannot go. `Rewrite` returns `Manual` for those two node kinds and `Fixed([])` (removal) for the rest, after `UnmigratedMapAsync` found no `CreateMap`/`CreateProjection` outside a profile constructor and no `[AutoMap]` in the project and its dependencies (only for removals that contain a check). The decision per form is in TAAK-250 **Choices**.

## Requirements

- **REQ-1**: WHEN a test class builds a configuration and a mapper in its only constructor and a test only checks the configuration THE SYSTEM SHALL remove the fields, the constructor and the test (proof `ConfigurationTest/validity-test`).
- **REQ-2**: WHEN a test method only holds a local configuration or mapper built from profiles and its check, or its expression body is the check THE SYSTEM SHALL remove the test method; a private static field used only by such a check goes too (proof `ConfigurationTest/local-configuration`).
- **REQ-3**: WHEN a `[SetUp]` builds a private mapper field that only a removed check uses THE SYSTEM SHALL remove the field, the setup and the test (proof `ConfigurationTest/setup-mapper`).
- **REQ-4**: WHEN the check is one statement among others THE SYSTEM SHALL remove only that statement and leave a configuration that is passed on (proof `ConfigurationTest/assert-statement`).
- **REQ-5**: WHEN the checked value comes from elsewhere (an xUnit class fixture) THE SYSTEM SHALL remove the test and leave the fixture and the field (proof `ConfigurationTest/class-fixture`).
- **REQ-6**: WHEN a test fixture builds a mapper, maps with it and checks the configuration THE SYSTEM SHALL write the profile's methods, call them, and remove the mapper, the configuration and the check (proof `_Combined/configuration-test-fixture`).
- **REQ-7**: IF the configuration does more than add profiles (inline `CreateMap`, settings, a method group) or adds a profile from a referenced assembly THEN THE SYSTEM SHALL report Manual (proofs `ConfigurationTest/inline-create-map`, `settings`, `method-group`, unit test `Rewrite_ProfileFromReferencedAssembly_ReportsManual`).
- **REQ-8**: IF a `CreateMap` outside a profile constructor (or an `[AutoMap]`) remains in the project or a project it depends on THEN THE SYSTEM SHALL report a removal that contains a check as Manual (proof `ConfigurationTest/unmigrated-map`, unit test `FixAsync_MapInReferencedProject_ChecksBeforeRemoving`).
- **REQ-9**: IF the check has side effects or is not a statement of its own THEN THE SYSTEM SHALL report Manual (proof `ConfigurationTest/side-effect-receiver`, unit test `Rewrite_AssertAsExpressionBody_ReportsManual`).
- **REQ-10**: WHEN a profile-only configuration or its mapper is passed on, or an `IMapper` is injected THE SYSTEM SHALL report nothing (unit test `Find_MapperPassedOn_FindsNothing`).

## Assumptions

- All-or-nothing: a run that rewrites anything has rewritten every profile constructor, so the emptied profiles have nothing left to check, and every Map call on a removed mapper has been rewritten.
- Test methods are not called by other code; a test class's only public parameterless constructor equals the implicit one.
- `AddProfile` arguments other than `typeof`/`new P()`/type arguments (e.g. `instance.GetType()`) cannot be checked statically; their profiles are assumed to be the solution's.

## Risks

- A check that validated maps of a non-profile source this scan does not see (a profile found at run time from a package) is removed. Mitigated by the source check for named profile types; rare in the corpus.
- A removed `[SetUp]`/constructor in a base class used by derived fixtures; the rule only removes members that end up empty, so derived classes lose nothing they used.

## Models

Class diagram, sequence and decision tree: [automapper-configuration-test-modellen.md](automapper-configuration-test-modellen.md).

## Open questions

- None for this row. `AutoMapperFix.Package` (removing the package) is the next step once escaping configurations and injected `IMapper`s are handled.
