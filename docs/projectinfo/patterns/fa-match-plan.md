---
kingmadoc: 1
feature: "fa-match"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/MatchPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/Match/
---

# Feature: fa.match

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-099 (actie uit `claude/tasks/ANALYSE-fa-100.md`).

## One-sentence summary

`s.Should().MatchRegex(r)` wordt `s.ShouldMatch(r)`, `NotMatchRegex` → `ShouldNotMatch`; wildcard-`Match`/`NotMatch` met een constante wordt de regex `(?s)\A…$`.

## Scope (in / out)

**In scope**: de vormen uit de requirements; `because` (ook met becauseArgs, centraal) als customMessage; `.And`-schakel via `fa.and-chain`.

**Out of scope**: alles wat de requirements als Manual noemen.

## Requirements

- **REQ-1**: WHEN `MatchRegex`/`NotMatchRegex` een niet-lege stringconstante krijgt THE SYSTEM SHALL `ShouldMatch`/`ShouldNotMatch` met hetzelfde patroon geven (proof `regex, not-regex, because`).
- **REQ-2**: WHEN `Match`/`NotMatch` een niet-lege wildcardconstante krijgt THE SYSTEM SHALL `(?s)\A` + `Regex.Escape` (`*`→`.*`, `?`→`.`) + `$` geven (proof `wildcard, not-wildcard`).
- **REQ-3**: WHEN het subject nullable is THE SYSTEM SHALL `ShouldNotBeNull()` ervoor zetten (proof `nullable`).
- **REQ-4**: IF het patroon een `Regex`, een variabele, leeg, of het subject geen string is THEN THE SYSTEM SHALL Manual geven (proof `regex-object, variable, empty, predicate`).

## Assumptions

- Probe: 22 wildcardgevallen (hoofdletters, regeleinden, `\r\n`, speciale tekens, trailing newline): 0 verschillen met `(?s)\A…$`. Met `\z` faalde `"SO-1\n" ~ "SO-1"`.
- Probe: `MatchRegex("")` faalt in FA, `ShouldMatch("")` slaagt → alleen niet-lege constanten.
- Reflectie Shouldly 4.3.0: `ShouldMatch(string, string, string?)`, `ShouldNotMatch(string, string, string?)`.

## Risks

- Alleen de foutmelding verschilt; waar het oordeel kon verschillen is het Manual.

## Modellen

Klassen, volgorde en beslisboom: [fa-match-modellen.md](fa-match-modellen.md).

## Zie ook

- [fa-match-verify.md](fa-match-verify.md)
- `claude/tasks/archief/TAAK-099-fa-match.md`
- [../README.md](../README.md)
