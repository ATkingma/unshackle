---
kingmadoc: 1
feature: "moq-create"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12]
files_expected:
  - src/Unshackle.Core/Fixes/Moq/MoqFix.cs
  - src/Unshackle.Core/Fixes/Moq/MoqSyntax.cs
  - src/Unshackle.Core/Fixes/Moq/MockDefaults.cs
  - src/Unshackle.Core/Fixes/Moq/Patterns/CreatePattern.cs
  - src/Unshackle.Tests/Proofs/Moq/Create/
---
# Feature: moq.create: a mock used only as `.Object` becomes `Substitute.For<T>()`

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton) |

> Drawn up from TAAK-307 step 3, the corpus survey `docs/projectinfo/architectuur/corpus-moq.md` (queue row 1) and council
> round 8 (general rule, M1).

## One-sentence summary

The `moq` fix swaps the Moq package and usings for NSubstitute; its first pattern `moq.create` turns `new Mock<T>()` whose
every use is `.Object`, `new Mock<T>().Object` and `Mock.Of<T>()` into `Substitute.For<T>()`, only where an unconfigured
mock and substitute provably answer the same.

## Scope (in / out)

**In scope**

- `MoqFix` (id `moq`): package `Moq` → `NSubstitute` 6.2.0, usings `Moq`, `Moq.Language`, `Moq.Language.Flow`, `Moq.Protected` → `NSubstitute`.
- Locals and private fields of type `Mock<T>` (declared `var`, `Mock<T>`, target-typed `new()`), assigned only `new Mock<T>()` or
  `new Mock<T>(MockBehavior.Loose/Default)`, used only as `x.Object` / `this.x.Object`.
- `new Mock<T>().Object` inline; `Mock.Of<T>()` and `Mock.Of<T>(MockBehavior.Loose)`.
- Manual with a reason code for every other `Mock<T>` declaration, creation, parameter, property, `Mock.Get` and `Mock.Of(predicate)`.

**Out of scope**

- `Setup`/`Verify`/`Returns`/... (queue rows 2-8); a mock with such a use stays Manual (`MockUsedBeyondObject`) until those rows land and run before this pattern.
- Class and delegate mocks, constructor arguments, `CallBase`/`DefaultValue` initializers (row 9).
- `MockBehavior.Strict` (council M1: Manual `StrictBehaviour`).

## Requirements

- **REQ-1**: WHEN a local `var m = new Mock<T>()` is only used as `m.Object` THE SYSTEM SHALL rewrite it to `var m = Substitute.For<T>()` and each `m.Object` to `m` (proof `local-object`).
- **REQ-2**: WHEN the local is declared `Mock<T> m = new()` THE SYSTEM SHALL declare it as `T m = Substitute.For<T>()` (proof `typed-local`).
- **REQ-3**: WHEN a private field is initialised or assigned with `new Mock<T>()` and only used as `.Object` (also `this._m.Object`) THE SYSTEM SHALL do the same for the field (proofs `field`, `field-assigned`).
- **REQ-4**: WHEN `new Mock<T>().Object` is written inline THE SYSTEM SHALL rewrite it to `Substitute.For<T>()` (proof `inline-object`).
- **REQ-5**: WHEN `Mock.Of<T>()` has no arguments THE SYSTEM SHALL rewrite it to `Substitute.For<T>()` (proof `mock-of`).
- **REQ-6**: WHEN the behaviour is a literal `MockBehavior.Loose`/`Default` THE SYSTEM SHALL drop it (proof `explicit-loose`); IF it is `Strict` THEN Manual `StrictBehaviour` (proof `strict`).
- **REQ-7**: IF the mock is used other than as `.Object` THEN THE SYSTEM SHALL give Manual `MockUsedBeyondObject` and leave every `.Object` of it alone (proof `setup-used`).
- **REQ-8**: IF an unconfigured mock and substitute of `T` answer differently in a way that has no known pin (a type parameter, a pure-virtual class; for `Mock.Of<T>()` an interface or non-sealed class; a settable property for `new Mock<T>()`) THEN THE SYSTEM SHALL give Manual `DefaultValueDiffers` naming the member (proof `setter`).
- **REQ-10** (council round 9, Q1): WHEN a member's Moq default is a known null (string, interface, delegate, also inside `Task<T>`/`ValueTask<T>`) THE SYSTEM SHALL pin it right after the creation statement with `m.M(Arg.Any<A>()...).ReturnsNull();` / `m.P.ReturnsNull();` and an Info line; members set up for every argument get no pin and do not count; more than `MockDefaults.MaxPins` (10) → Manual `TooManyDefaultPins`; a creation that is not a statement (field initializer, inline, `Mock.Of`) cannot take pins → Manual `DefaultValueDiffers` (proof `string-return`, run-time `create-pinned-local`, `create-pinned-field`).
- **REQ-11** (Q4): WHEN a mock is never used THE SYSTEM SHALL still rewrite it and add the Info line "mock never used" (run-time `create-unused`).
- **REQ-12** (Q2): Every Fixed shape SHALL have a run-time proof in `src/Unshackle.Tests/Proofs/Moq/_Runtime/` (`MoqRuntimeProofTests`): before on Moq, after on NSubstitute, same outcome; `create-must-fail` fails under both.
- **REQ-9**: IF the mock is a parameter, property, non-private field, several variables in one declaration, `Mock.Get(x)`, `Mock.Of(predicate)`, a class mock or has constructor arguments/initializer THEN Manual (`MockPassedAround`, `MockDeclaration`, `MockGet`, `MockOfSetup`, `ClassMock`, `ConstructorArguments`, `MockOptions`; unit tests in `CreatePatternTests`). Council round 13, M8: `MockOfSetup` reports as `moq.setup-returns`, the mock-use codes of row 13 (`StrictBehaviour`, `ProtectedMember`, `MockAs`, `MockInvocations`, `MockReset`, `MockRaise`) as `moq.unsupported` (`SubPatternIdTests`).

## Assumptions

- Defaults (Moq 4.18 `EmptyDefaultValueProvider`/`MockDefaultValueProvider` versus NSubstitute's auto-value providers): both give empty arrays, empty `IEnumerable`/`IQueryable`, completed `Task`, default value types. They differ for `string` (null vs ""), interfaces/delegates (null vs substitute, `new Mock<T>()` only), non-sealed classes (mock vs null, `Mock.Of` only) and property setters (`new Mock<T>()` forgets, NSubstitute remembers). `MockDefaults` encodes exactly this table.
- A `.Object`-only mock has no setups, so the only observable behaviour is these defaults (general rule of council round 8).

## Risks

- The defaults table is checked by the run-time harness (council round 9, Q2) for the shapes in `_Runtime/`.
- `TargetFrameworkUnsupported` (Q3): a project row through `IProjectFileMigration.ProjectBlockers` plus Manual in every pattern (from the preprocessor symbols); the project row does not yet block the `Package` swap (engine).
- The strict defaults check makes many corpus mocks Manual until the setup rows land; that is intended.

## Models

Class diagram, sequence and decision tree: [moq-create-modellen.md](moq-create-modellen.md).

## Open questions

- Answered by council round 9 (Q1-Q4).
