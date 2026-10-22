---
kingmadoc: 1
feature: "automapper-mock-set-up"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MockSetUpPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MockedMappers.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MockedMapper.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MockReason.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MockSetUpCode.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapStub.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Core/AutoMapper/MockSetUpPatternTests.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/mocked-stub-removed.before.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/mocked-matcher-stub-removed.before.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/mocked-no-stubs.before.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/MapCall/mocked-verify.before.cs
---

# Feature: automapper.mock-set-up: remove a stub of a mocked IMapper that provably returns the generated mapping

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-311 (ANALYSE-manual-round-2 rank 1) and council round 8, decision A1; extended with council round 11, A6–A8.

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
- A7: a stub with an argument matcher (`It.IsAny<A>()`, `Arg.Any<A>()`) when the test member builds exactly one `A` itself (a local with
  a plain `new A { ... }`) and holds no other expression of type `A` outside the stub (a field, a helper call, a cast); that `A` is then the
  source, and every other rule of a stub applies to it unchanged.
- A10 (council round 12): that one `A` may also be created in place (no local), with literal or no constructor arguments. A member
  it does not set blocks when the constructor has arguments (value unknown) or the test reads a member of that name (default-constructed).
- A8: a mock without stubs, when every Map call of its classes is a plain `Map` whose value nothing reads: a statement of its own, a
  discard `_ = ...`, or the initializer of a local that is never read. An Info row on the mock says "had no stubs; Map now returns a real
  object".

**Out of scope (stub stays, Manual names the reason)**

- A6: a check of a Map call (`Verify(m => m.Map..., Times.X)` in any `Times`, `Received(..)`/`DidNotReceive()`/`...WithAnyArgs()`):
  Manual `VerifyOnMapper`, "assert the mapped result instead"; the mock's stubs stay with the same code.
- Other argument matchers (`It.Is`, `Arg.Is`), `Verifiable`, `SetupSequence`, `ReturnsAsync`, a lambda in `Returns`, a mock that is
  handed on, assigned another value or created in place (`new C(Substitute.For<IMapper>())`).
- A stub whose value is not provably the generated mapping (A1/A7): Manual `MockedReturnNotProvable`.
- A mock without stubs whose class reads, null-checks, returns, passes or stores what Map returns (the loose mock answers null, the
  generated method a real object): Manual `MockWithoutStubsNotProvable`. A member read or null check of the value inside the method is
  also a read, because on the mock it throws or branches differently (confirmed by council round 12, A9).
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

Council round 11: `MockedMappers.Find` also collects the checks of a Map call (`Checks`); one check makes the mock's `Problem` the typed
`VerifyOnMapper` reason, and `MapOwners.KeptCall` gives the checked Map call its own `VerifyOnMapper` row. Reasons are `MockReason`
(text + `MockSetUpCode`), rendered through `ManualResult.Code` (council round 10, C1). A stub with an argument matcher goes through
`MockedMappers.Matched` to find its one source. A mock without stubs is a candidate; `Undischarged` then asks `ValueUse` for every Map
call of its classes instead of "is it stubbed".

`MockSetUpPattern` (an `ISolutionFinder`, run after the call patterns and before `UnusedMapperPattern`) finds the stub statements of the
discharged mocks and removes them, each with a warning (for a matcher stub it adds "the stub matched any A; the test builds one"), and
the declarations of discharged mocks without stubs, which get the A8 Info row. `MapCallPattern.FindAsync` leaves out the Map calls inside those statements.
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
- **REQ-10** (A6): WHEN a test checks a Map call of the mock (Verify with any Times, Received, DidNotReceive) THE SYSTEM SHALL keep the mock, its stubs and its class, with Manual code `VerifyOnMapper` and the hint "assert the mapped result instead".
- **REQ-11** (A7): WHEN a stub uses `It.IsAny<A>()` / `Arg.Any<A>()`, the test member builds exactly one `A` in a local and holds no other `A`, and the stub's value provably equals the generated mapping of that `A` THE SYSTEM SHALL remove the stub with a warning naming the matcher.
- **REQ-12** (A7): WHEN a matcher stub does not meet REQ-11 (two or no `A`s, another `A` expression, other value, value used as a whole) THE SYSTEM SHALL keep it with Manual code `MockedReturnNotProvable` (also for A1 stubs that are not provable).
- **REQ-14** (A10): WHEN the one `A` of a matcher stub is created in place with literal or no arguments, and every member the test reads is set in its initializer and provably copied THE SYSTEM SHALL remove the stub; a default-constructed member the test reads, or a member left to constructor arguments, keeps it Manual `MockedReturnNotProvable`.
- **REQ-13** (A8): WHEN a mock has no stubs and no other problem, and every Map call of its classes is a plain Map whose value is never read THE SYSTEM SHALL migrate the classes and add an Info row on the mock; otherwise Manual code `MockWithoutStubsNotProvable` naming the call's file:line and the use.

## See also

- [verify](automapper-mock-set-up-verify.md), [modellen](automapper-mock-set-up-modellen.md)
- [automapper-map-call plan](automapper-map-call-plan.md), [automapper-unused-imapper plan](automapper-unused-imapper-plan.md)
- `claude/docs/council.md` round 8, decision A1, and round 11, A6–A8; `claude/tasks/TAAK-311-am-mocked-imapper.md`
