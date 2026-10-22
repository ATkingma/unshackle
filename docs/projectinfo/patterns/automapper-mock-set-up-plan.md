---
kingmadoc: 1
feature: "automapper-mock-set-up"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MockSetUpPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MockedMappers.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MockedMapper.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapStub.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Core/AutoMapper/MockSetUpPatternTests.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/mocked-stub-removed.before.cs
---

# Feature: automapper.mock-set-up: remove a stub of a mocked IMapper that provably returns the generated mapping

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-311 (ANALYSE-manual-round-2 rank 1) and council round 8, decision A1.

## One-sentence summary

automapper.mock-set-up: a stub `mock.Setup(m => m.Map<B>(a)).Returns(b);` (Moq) or `mock.Map<B>(a).Returns(b);` (NSubstitute) is removed,
and the class under test migrates to the generated `a.ToB()`, only when the mock serves just maps that are all migrated and `b` provably
equals what the generated method returns for `a`; every other stub stays and its Manual row names the test (file:line) and why.

## Scope (in / out)

**In scope**

- A mock of `IMapper` held in a local, field or property of a test file (`new Mock<IMapper>()`, `new()`, `Substitute.For<IMapper>()`,
  `A.Fake<IMapper>()`), whose every use is its creation, a constructor argument of a class of the solution (`mock` or `mock.Object`), or a
  stub statement of the shape above.
- A stub whose source and value are locals of the same member created with `new A { ... }` / `new B { ... }` (no constructor arguments, an
  object initializer of simple assignments), or a value created in place.

**Out of scope (stub stays, Manual names the reason)**

- Argument matchers (`It.IsAny`, `Arg.Any`, `It.Is`), `Verify`/`Received`/`Verifiable`, `SetupSequence`, `ReturnsAsync`, a lambda in
  `Returns`, a mock that is handed on, assigned another value or created in place (`new C(Substitute.For<IMapper>())`).
- A mock that stubs nothing (a loose mock answers `Map` with null, the generated method does not).
- Maps with options, profiles with settings, members of a non-simple type (collections, nested objects), flattening, a destination with a
  constructor with parameters or public fields.
- A test that uses the stubbed object itself (reference comparison, another stub argument) unless the destination is a record.

## Design

`MockedMappers.Find` lists the mocks of a file with their classes, stubs and the first use that is neither (`Problem`, with file:line).
`MockedMappers.Disproof` returns why a stub's value may differ from the generated mapping, or null: both objects are compared member by
member as values (`c:<constant>` for a constant or a property's constant default, `s:<member>` for the value the source object holds), for
every settable member of `B`, which must be a same-named, same-typed, simple member of `A`. The source and the value must not change after
their creation in the member.

`MapOwners.BuildAsync` (the solution plan) treats a mock without problem and with only provable stubs as a candidate: its classes do not
become classes under test, and its stubs are neither shared nor kept. Once the plan is stable, `Undischarged` checks each candidate: its
classes are not under test through another mock and do not hand their `IMapper` on, every stubbed pair is migrated, and every Map call of its
classes is migrated and stubbed by this mock (an unstubbed map ran on the loose mock and returned null). A candidate that fails is vetoed
with the reason and the plan is built again (the vetoed set only grows, so this ends). Plain pairs come from profiles whose single
constructor holds only CreateMap statements, from a statement `CreateMap<A, B>()` (optionally `.ReverseMap()`).

`MockSetUpPattern` (an `ISolutionFinder`, run after the call patterns and before `UnusedMapperPattern`) finds the stub statements of the
discharged mocks and removes them, each with a warning. `MapCallPattern.FindAsync` leaves out the Map calls inside those statements.
`UnusedMapperPattern` then removes the class's `IMapper` parameter and the test's argument as usual. The mock declaration itself stays.

## Requirements

- **REQ-1**: WHEN every stub of a mock provably returns the generated mapping of its source and every map its classes call is stubbed and migrated THE SYSTEM SHALL remove the stubs and migrate the classes (proof `_Combined/mocked-stub-removed`, Moq and NSubstitute).
- **REQ-2**: WHEN a stub is removed THE SYSTEM SHALL warn with the stub's file:line, the class that now calls the generated method, and the condition the test relies on.
- **REQ-3**: WHEN a stub's value differs, or cannot be shown to equal, the generated mapping (matcher, other constant, non-constant initializer, changed object) THE SYSTEM SHALL keep it and name the reason and file:line in the Manual row.
- **REQ-4**: WHEN the mock is used another way (Verify, Received, handed on) THE SYSTEM SHALL keep its stubs and name that use with file:line.
- **REQ-5**: WHEN the test uses the stubbed object itself and the destination is not a record THE SYSTEM SHALL keep the stub; a record compared as a whole is allowed.
- **REQ-6**: WHEN the stubbed map has options or its profile has settings THE SYSTEM SHALL keep the stub; options on another map of the profile do not count.
- **REQ-7**: WHEN a class of the mock calls a map the mock does not stub, or a call that stays on IMapper THE SYSTEM SHALL keep the stubs and the class under test.
- **REQ-8**: WHEN a class stays under test THE SYSTEM SHALL name the test (file:line) and why its mock does not qualify in the Manual rows of its calls.
- **REQ-9**: WHEN no mock qualifies THE SYSTEM SHALL produce the same rewrites as before (only the Manual reasons gain the test and the reason).

## See also

- [verify](automapper-mock-set-up-verify.md), [modellen](automapper-mock-set-up-modellen.md)
- [automapper-map-call plan](automapper-map-call-plan.md), [automapper-unused-imapper plan](automapper-unused-imapper-plan.md)
- `claude/docs/council.md` round 8, decision A1; `claude/tasks/TAAK-311-am-mocked-imapper.md`
