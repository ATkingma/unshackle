---
kingmadoc: 1
feature: "moq-class-mocks"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/Moq/Patterns/ClassMocksPattern.cs
    - src/Unshackle.Core/Fixes/Moq/MoqSyntax.cs
    - src/Unshackle.Tests/Proofs/Moq/ClassMocks/
---

# Feature: moq.class-mocks: `new Mock<C>(args) { CallBase = true }` becomes `Substitute.ForPartsOf<C>(args)`

|               |                                    |
| ------------- | ---------------------------------- |
| **Project**   | Unshackle                          |
| **Status**    | Approved                           |
| **Generated** | 2026-10-22 by KingmaDoc (agent L2) |

> From `claude/tasks/moq-queue.md` row 9; council rounds 8 (general rule) and 9 (Q2 run-time proofs). Measured with a probe program
> against Moq 4.18.4 and NSubstitute 6.2.0 before any code.

## One-sentence summary

A class mock with `CallBase = true` whose every use is `.Object` becomes a partial substitute with the same constructor arguments;
every other class or delegate mock stays Manual with a measured reason.

## Measurements (Moq 4.18.4 vs NSubstitute 6.2.0)

| Case                                                                                                                                                               | Moq                 | NSubstitute                                   | Verdict         |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------- | --------------------------------------------- | --------------- |
| `CallBase = true`: constructor args, virtual, non-virtual, property get/set, protected virtual, `ToString`/`Equals`/`GetHashCode`, virtual call in the constructor | real code           | real code                                     | same            |
| class mock without `CallBase`: virtual `string` member                                                                                                             | `null`              | `""` (`For<T>`) / real code (`ForPartsOf<T>`) | differs: Manual |
| `Setup(..).Returns(..)` on a `CallBase` mock                                                                                                                       | real member not run | real member runs once while configuring       | differs: Manual |
| abstract class, abstract `string` member                                                                                                                           | `null`              | `""`                                          | differs: Manual |
| wrong constructor arguments                                                                                                                                        | `ArgumentException` | `ArgumentException`                           | same            |

## Scope (in / out)

**In scope**: `new Mock<C>(args) { CallBase = true }` and `new Mock<C>(MockBehavior.Loose|Default, args) { CallBase = true }` as a local,
a private field, an assignment or inline `.Object`, with `.Object` as the only use.

**Out of scope** (Manual): no `CallBase = true` (`ClassMockNoCallBase`), abstract class (`ClassMockAbstract`), any use beside `.Object`
(`ClassMockSetup`), other initializer options (`MockOptions`), `MockBehavior.Strict` (`StrictBehaviour`), `Mock.Of<C>()` and delegate mocks (`ClassMock`).

## Requirements

- **REQ-1**: WHEN a CallBase class mock is a local THE SYSTEM SHALL write `Substitute.ForPartsOf<C>(args)` and drop `.Object` (proof `callbase`, `callbase-behavior`).
- **REQ-2**: WHEN it is inline or a private field THE SYSTEM SHALL do the same (proofs `callbase-inline`, `callbase-field`).
- **REQ-3**: IF the class mock has no `CallBase = true` THEN Manual `ClassMockNoCallBase`; IF the class is abstract THEN `ClassMockAbstract` (proofs `no-callbase`, `abstract`).
- **REQ-4**: IF the mock is used beside `.Object` THEN Manual `ClassMockSetup` (proof `setup`).
- **REQ-5**: IF other options, `Mock.Of<C>()` or a delegate THEN Manual (proofs `other-options`, `mock-of`, `delegate`).
- **REQ-6**: The rewrite SHALL behave as Moq at run time (`_Runtime/class-callbase-pass`; `class-callbase-ctor-must-fail` fails under both).

## Assumptions

- Findings carry the id `moq.class-mocks` (own pattern class deriving from `CreatePattern`; `moq.create` no longer owns class or delegate mocks).

## Risks

- A class whose members behave differently per proxy (sealed members, internal virtuals) is not probed; the harness covers public and protected virtuals.

## Models

[moq-class-mocks-modellen.md](moq-class-mocks-modellen.md).

## Open questions

- None.
