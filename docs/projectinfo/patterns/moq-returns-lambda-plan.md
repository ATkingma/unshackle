---
kingmadoc: 1
feature: "moq-returns-lambda"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
  - src/Unshackle.Core/Fixes/Moq/Patterns/ReturnsLambdaPattern.cs
  - src/Unshackle.Core/Fixes/Moq/CallLambda.cs
  - src/Unshackle.Core/Fixes/Moq/SetupReturnsSyntax.cs
  - src/Unshackle.Tests/Proofs/Moq/ReturnsLambda/
---
# Feature: moq.returns-lambda: `Returns((a, b) => ..)` becomes `Returns(ci => .. ci.ArgAt<A>(0) ..)`

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton) |

> Drawn up from `claude/tasks/moq-queue.md` row 7; council rounds 8 (general rule) and 9 (Q2 run-time proofs).

## One-sentence summary

A Setup whose `Returns`/`ReturnsAsync` gets a lambda becomes an NSubstitute `Returns(ci => ..)`. Each lambda parameter becomes
`ci.ArgAt<T>(i)`; NSubstitute and Moq both call the function per call with the call's arguments.

## Scope (in / out)

**In scope**

- `Returns(() => ..)`, `Returns((T1 a, ..) => ..)` with as many parameters as the mocked method, expression or block body.
- `ReturnsAsync(..)` with an expression body: `Returns(ci => Task.FromResult<R>(body'))` / `new ValueTask<R>(body')`.

**Out of scope**

- Method groups and anonymous methods, async lambdas, a lambda that changes its parameter, a block-bodied `ReturnsAsync` function, a
  member that returns a delegate: Manual `ReturnsLambda`.

## Requirements

- **REQ-1**: WHEN the lambda has parameters THE SYSTEM SHALL replace each with `ci.ArgAt<T>(i)` (proofs `parameter`, `two-parameters`).
- **REQ-2**: WHEN the lambda has no parameters THE SYSTEM SHALL write `ci => body` (proof `no-parameters`).
- **REQ-3**: WHEN `ReturnsAsync(lambda)` has an expression body THE SYSTEM SHALL wrap it in `Task.FromResult<R>(..)` (proof `returns-async`).
- **REQ-4**: WHEN the body already uses the name `ci` THE SYSTEM SHALL pick `callInfo` (run-time `returns-lambda-block`).
- **REQ-5**: IF a method group, a changed parameter or a block-bodied async function THEN Manual `ReturnsLambda` (proofs `SetupReturns/returns-method-group`, `changes-parameter`, `async-block`).
- **REQ-6**: The whole fix SHALL compute the same values per call, including captured state (run-time `returns-lambda-pass`, `returns-lambda-block`; `returns-lambda-must-fail` throws under both).

## Assumptions

- `ci.ArgAt<T>(i)` gives the i-th argument of the call cast to `T`, as Moq passes it to a typed parameter.

## Risks

- None known.

## Models

[moq-returns-lambda-modellen.md](moq-returns-lambda-modellen.md).

## Open questions

- None.
