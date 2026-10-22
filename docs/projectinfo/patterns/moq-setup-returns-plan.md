---
kingmadoc: 1
feature: "moq-setup-returns"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/Moq/Patterns/SetupReturnsPattern.cs
  - src/Unshackle.Core/Fixes/Moq/SetupReturnsSyntax.cs
  - src/Unshackle.Core/Fixes/Moq/ArgMatchers.cs
  - src/Unshackle.Core/Fixes/Moq/MockCall.cs
  - src/Unshackle.Core/Fixes/Moq/MockUsage.cs
  - src/Unshackle.Core/Fixes/Moq/MockUseRules.cs
  - src/Unshackle.Tests/Proofs/Moq/SetupReturns/
---
# Feature: moq.setup-returns: `m.Setup(x => x.M(..)).Returns(v)` becomes `m.M(..).Returns(v)`

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton) |

> Drawn up from `claude/tasks/moq-queue.md` rows 2 and 3 and council rounds 8 (general rule) and 9 (Q1 pins, Q2 run-time proofs).

## One-sentence summary

A `Setup(..).Returns(v)` / `.ReturnsAsync(v)` statement on a mock that converts as a whole becomes an NSubstitute configuration
on `m.Object` (which `moq.create` then drops), with Moq's argument matchers rewritten to `Arg` specs (row 2).

## Scope (in / out)

**In scope**

- `m.Setup(x => x.Method(args)).Returns(value);` and `m.Setup(x => x.Property).Returns(value);` as a statement, `m` a local or private field (also `this._m`).
- `.ReturnsAsync(value)` on `Task<T>` / `ValueTask<T>` → `.Returns(Task.FromResult<T>(value))` / `.Returns(new ValueTask<T>(value))`.
- Row 2 matchers (`ArgMatchers`): `It.IsAny<T>()` → `Arg.Any<T>()`, `It.Is<T>(lambda)` → `Arg.Is<T>(lambda)`, `It.IsNotNull<T>()` → `Arg.Is<T>(a => a != null)`; once one argument is a matcher, a plain value becomes `Arg.Is<T>(value)`.

**Out of scope**

- `Returns(lambda)` (row 7), `Callback` (row 8), `Throws` (row 6), `Verifiable`/sequences (rows 10-11): such a use keeps the whole mock on Moq.
- Matchers `It.IsIn`, `It.IsRegex`, `It.IsInRange`, `It.Ref`, `It.IsAnyType`, a matcher nested in an expression, ref/out/params parameters, named arguments.

## Requirements

- **REQ-1**: WHEN a property is set up THE SYSTEM SHALL write `m.Object.P.Returns(v);` (proof `property`).
- **REQ-2**: WHEN arguments are `It.IsAny<T>()` THE SYSTEM SHALL write `Arg.Any<T>()` (proof `any-argument`).
- **REQ-3**: WHEN matchers and plain values are mixed THE SYSTEM SHALL wrap the values in `Arg.Is<T>(value)` (proof `mixed-arguments`); only plain values stay plain (proof `literal-arguments`).
- **REQ-4**: WHEN `ReturnsAsync(v)` is used THE SYSTEM SHALL write `Returns(Task.FromResult<T>(v))` and add `using System.Threading.Tasks;` (proof `returns-async`).
- **REQ-5**: IF `Returns` gets a method group THEN Manual `ReturnsLambda` (proof `returns-method-group`); a lambda is `moq.returns-lambda`.
- **REQ-6**: IF the mock does not convert as a whole (strict, another use such as `Callback`, default that cannot be pinned) THEN every Setup of it is Manual with the mock's reason (proofs `strict`, `other-use-blocks`).
- **REQ-7**: The whole fix SHALL give the same run-time answers as Moq, including overriding setups, non-matching arguments (Moq's null pinned by `moq.create`) and setups in a constructor (run-time `setup-returns`, `setup-override`, `setup-field`; `setup-must-fail` throws under both).
- **REQ-8**: A member set up for every argument (`It.IsAny` only, or a property) SHALL get no pin and not count toward `MaxPins`; any other set-up member keeps its pin (council round 9, Q1).

- **REQ-9** (found in row 8): WHEN the member has another setup in the mock's scope THE SYSTEM SHALL configure through `m.Configure().M(..)` (`NSubstitute.Extensions`), because an NSubstitute configuration call otherwise runs the earlier matching configuration (its function, AndDoes or Throws) (run-time `setup-overlap-side-effects`).

## Assumptions

- NSubstitute, like Moq, lets the last matching configuration win, and compares plain arguments with `Equals`.
- `moq.create` runs after this pattern in `MoqFix.Patterns`; with `pendingUses: false` it never converts a mock whose Setup is still on Moq.

## Risks

- Without NSubstitute in the semantic model (`new MoqFix()` without references) the rewritten `Returns` does not bind before `moq.create`
  runs: the exemption of REQ-8 is then missed and an extra (harmless) pin is written. Register `MoqFix` with the NSubstitute reference.

## Models

[moq-setup-returns-modellen.md](moq-setup-returns-modellen.md).

## Open questions

- None.
