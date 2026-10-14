---
kingmadoc: 1
feature: "fa-be-same-as"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeSameAsPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/BeSameAs/
---

# Feature: fa.be-same-as

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-093 (action from `claude/tasks/ANALYSE-fa-100.md`).

## One-sentence summary

`x.Should().BeSameAs(y)` becomes `x.ShouldBeSameAs(y)`; `NotBeSameAs` becomes `ShouldNotBeSameAs`.

## Scope (in / out)

**In scope**

- `BeSameAs`/`NotBeSameAs` on any FA subject (ReferenceTypeAssertions, including strings and collections).
- `because` as customMessage; `.And` link via `fa.and-chain`.

**Out of scope**

- becauseArgs, result used (`.And`/`.Which`), AssertionScope: Manual.

## Requirements

- **REQ-1**: WHEN `BeSameAs(y)` is present THE SYSTEM SHALL produce `x.ShouldBeSameAs(y)` (proof `simple, nullable`).
- **REQ-2**: WHEN `NotBeSameAs(y)` is present THE SYSTEM SHALL produce `x.ShouldNotBeSameAs(y)` (proof `not`).
- **REQ-3**: WHEN `because` is present without becauseArgs THE SYSTEM SHALL put it last as customMessage (proof `because`).
- **REQ-4**: IF becauseArgs, a used result or an AssertionScope THEN THE SYSTEM SHALL produce Manual (proof `because-args, result-used, assertion-scope`).
- **REQ-5**: WHEN `BeSameAs` is a link after `.And` THE SYSTEM SHALL produce a separate statement via `fa.and-chain` (proof `AndChain/be-same-as`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL produce `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (proof `because-args`); named arguments or a non-constant `because` remain Manual. Supersedes that part of REQ-4.

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0): both use ReferenceEquals; same verdict for same, different, both null, one null and boxed values.
- Shouldly 4.3.0 (reflection): `ShouldBeSameAs(object?, object?, string?)` and `ShouldNotBeSameAs(object?, object?, string?)`.

## Risks

- None: identical semantics; only the failure message differs.

## Models

Class diagram, sequence diagram and decision tree: [fa-be-same-as-modellen.md](fa-be-same-as-modellen.md).

## See also

- [fa-be-same-as-verify.md](fa-be-same-as-verify.md)
- `claude/tasks/archief/TAAK-093-fa-be-same-as.md`
- [../README.md](../README.md)
