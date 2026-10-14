---
kingmadoc: 1
feature: "fa-have-length"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveLengthPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/HaveLength/
---

# Feature: fa.have-length: string HaveLength(n) becomes Length.ShouldBe(n)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-039.

## One-sentence summary

`s.Should().HaveLength(n)` becomes `s.Length.ShouldBe(n)`; nullable subject `s.ShouldNotBeNull().Length.ShouldBe(n)`.

## Scope (in / out)

**In scope**

- `HaveLength(int)` on `StringAssertions`, with optional `because`.

**Out of scope**

- Stream `HaveLength(long)` (Manual).
- Subject with `?.` (Manual).

## Requirements

- **REQ-1**: WHEN `s.Should().HaveLength(n)` is a statement THE SYSTEM SHALL produce `s.Length.ShouldBe(n)` (proof `simple`).
- **REQ-2**: WHEN `because` is present without becauseArgs THE SYSTEM SHALL pass it as customMessage (proof `because`).
- **REQ-3**: WHEN the subject is MaybeNull in a nullable context THE SYSTEM SHALL prepend `ShouldNotBeNull()` (proof `nullable`).
- **REQ-4**: WHEN the subject is a compound expression THE SYSTEM SHALL wrap it in parentheses (proof `receiver-parens`).
- **REQ-5**: IF a stream, becauseArgs, an AssertionScope or `.And` THEN THE SYSTEM SHALL give Manual (proofs `stream`, `because-args`, `assertion-scope`).
- **REQ-6**: WHEN `HaveLength` is a link after `.And` THE SYSTEM SHALL produce a separate statement via `fa.and-chain` (proof `AndChain/equal-length-approximately`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL pass `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (Fixed, proof `because-args`); named arguments or a non-constant `because` remain Manual. REQ-5 therefore no longer applies to becauseArgs.

## Assumptions

- Probe: a null string fails in FA and with `ShouldNotBeNull()`; without a nullable context `s.Length` throws a NullReferenceException, so the test fails too.

## Risks

- The message on null differs (NullReferenceException instead of an assertion) when nullable is off.

## Models

Class diagram, sequence diagram and decision tree: [fa-have-length-modellen.md](fa-have-length-modellen.md).
