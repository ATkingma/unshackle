---
kingmadoc: 1
feature: "fa-invoking-local"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingLocalPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/InvokingLocal/
---

# Feature: fa.invoking-local: `var act = x.Invoking(s => s.M());` becomes `Action act = () => x.M();`

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-18 by KingmaDoc |

> Drafted from TAAK-287 (mihome keeps the delegate of `Invoking` in a local and asserts on it later).

## One-sentence summary

An `Invoking`/`Awaiting` call stored in a local becomes a plain delegate local, so the Throw/NotThrow patterns can rewrite `act.Should().Throw<T>()` on it.

## Scope (in / out)

**In scope**

- `var act = x.Invoking(s => ...);` (and `Awaiting`, an explicit declared type) with the same receiver rules as `fa.invoking` (a local, parameter, field or `this` of a reference type; `ThrowSyntax.TryGetAction`).

**Out of scope**

- `FluentActions.Invoking(...)` in a local, receivers with possible side effects (property such as `mock.Object`, calls): Manual with the `fa.invoking` reason.
- The rewrite of the later `act.Should().Throw<T>()` (other patterns).

## Requirements

- **REQ-1**: WHEN a local is initialized with a FluentAssertions `Invoking`/`Awaiting` call on a stable reference receiver THE SYSTEM SHALL initialize it with `() => body[s := x]` and write the delegate type FluentAssertions returned (proof `InvokingLocal/simple.*`).
- **REQ-2**: WHEN the receiver may have side effects or is a value type THE SYSTEM SHALL report Manual (proof `InvokingLocal/side-effect-receiver.*`).
- **REQ-3**: WHEN the delegate is asserted on later THE SYSTEM SHALL let the Throw patterns rewrite it (proof `_Combined/invoking-local.*`).

## Assumptions

- `Invoking` only wraps the lambda and runs nothing, so the local holds the same behavior; the receiver is read when the lambda runs instead of before, which is the same for locals, parameters and fields that are not reassigned in between (the same assumption as `fa.invoking`).

## Risks

- A local that is reassigned between the declaration and the assertion: not detected (shortcut shared with `fa.invoking`).

## Models

Models: [fa-invoking-local-modellen.md](fa-invoking-local-modellen.md).
