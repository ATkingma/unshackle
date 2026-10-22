---
kingmadoc: 1
feature: "moq-properties"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/Moq/PropertySyntax.cs
  - src/Unshackle.Core/Fixes/Moq/MockUseContext.cs
  - src/Unshackle.Core/Fixes/Moq/MockDefaults.cs
  - src/Unshackle.Core/Fixes/Moq/MockCall.cs
  - src/Unshackle.Core/Fixes/Moq/VerifySyntax.cs
  - src/Unshackle.Core/Fixes/Moq/Patterns/CreatePattern.cs
---
# Feature: moq.properties: SetupGet, SetupProperty, SetupAllProperties, VerifyGet, VerifySet

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton) |

> Drawn up from `claude/tasks/moq-queue.md` row 5; council rounds 8 (general rule), 9 (Q1 settable property → Manual, Q2 run-time proofs).

## One-sentence summary

Row 5 reports as `moq.properties` (council round 12, M7), reusing the existing rewrites:

- `SetupGet` is rewritten as by `moq.setup-returns` (`m.P.Returns(v)`), reported by `PropertiesPattern`.
- `VerifyGet`/`VerifySet` are rewritten as by `moq.verify` (`_ = m.Received(n).P;`, `m.Received(n).P = v;`), reported by `PropertiesPattern`.
- `SetupProperty`/`SetupAllProperties` are handled by `moq.create`. These are the only ways a Moq property remembers a set value, as NSubstitute's always does, so with them a settable property is no longer a `DefaultValueDiffers` blocker.

## Scope (in / out)

**In scope**

- `m.SetupGet(x => x.P).Returns(v)[.Verifiable()];`
- `m.VerifyGet(x => x.P[, Times]);`, `m.VerifySet(x => x.P = value | It.IsAny<T>() | It.Is<T>(..)[, Times]);`
- `m.SetupAllProperties();`, `m.SetupProperty(x => x.P[, initial]);` before any other setup or verify of the mock.

**Out of scope**

- `SetupSet` (Manual `PropertySetup`), indexers, `SetupProperty` after other setups (Manual `PropertyOrder`).

## Requirements

- **REQ-1**: WHEN `SetupGet(x => x.P).Returns(v)` THE SYSTEM SHALL write `m.Object.P.Returns(v);` (proof `Properties/setup-get`).
- **REQ-2**: WHEN `VerifyGet(x => x.P, Times)` THE SYSTEM SHALL write `_ = m.Object.Received(n).P;` (proof `Properties/verify-get`).
- **REQ-3**: WHEN `VerifySet(x => x.P = v, Times)` THE SYSTEM SHALL write `m.Object.Received(n).P = v';` with matchers rewritten (proof `Properties/verify-set`).
- **REQ-4**: WHEN `SetupAllProperties()` THE SYSTEM SHALL remove it in the same pass as the declaration, treat every settable property as remembering, and pin Moq's null default for string/interface properties (proof `Create/setup-all-properties`).
- **REQ-5**: WHEN `SetupProperty(x => x.P[, initial])` THE SYSTEM SHALL remove it, or write `m.P.Returns(initial);`, which is a configuration and not a set call that `Received` would count (proof `Create/setup-property-initial`).
- **REQ-6**: IF `SetupSet` THEN Manual `PropertySetup` (proof `Create/setup-set`); IF a tracking statement comes after another setup or verify THEN Manual `PropertyOrder` (proof `SetupReturns/property-order`); a settable property without tracking stays Manual `DefaultValueDiffers` (proof `Create/setter`).
- **REQ-7**: The whole fix SHALL give the same values and Verify outcomes as Moq (run-time `props-all-properties`, `props-setup-property`, `props-setup-property-default`; `props-must-fail` fails under both).
- **REQ-8**: Tracking statements are rewritten by `moq.create`, not by a pattern before it, so `moq.create`'s own analysis still sees them (consistency of one mock across patterns).

## Assumptions

- NSubstitute: setting a property after `P.Returns(..)`/`P.ReturnsNull()` makes the getter return the set value (last configuration wins); proven at run time.

## Risks

- A test that relies on Moq resetting property setups by a late `SetupAllProperties` stays Manual (`PropertyOrder`).

## Models

[moq-properties-modellen.md](moq-properties-modellen.md).

## Open questions

- None.
