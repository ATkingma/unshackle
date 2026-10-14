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

> Drafted from TAAK-099 (action from `claude/tasks/ANALYSE-fa-100.md`).

## One-sentence summary

`s.Should().MatchRegex(r)` becomes `s.ShouldMatch(r)`, `NotMatchRegex` → `ShouldNotMatch`; wildcard `Match`/`NotMatch` with a constant becomes the regex `(?s)\A…$`.

## Scope (in / out)

**In scope**: the forms from the requirements; `because` (also with becauseArgs, centrally) as customMessage; `.And` link via `fa.and-chain`.

**Out of scope**: everything the requirements list as Manual.

## Requirements

- **REQ-1**: WHEN `MatchRegex`/`NotMatchRegex` receives a non-empty string constant THE SYSTEM SHALL produce `ShouldMatch`/`ShouldNotMatch` with the same pattern (proof `regex, not-regex, because`).
- **REQ-2**: WHEN `Match`/`NotMatch` receives a non-empty wildcard constant THE SYSTEM SHALL produce `(?s)\A` + `Regex.Escape` (`*`→`.*`, `?`→`.`) + `$` (proof `wildcard, not-wildcard`).
- **REQ-3**: WHEN the subject is nullable THE SYSTEM SHALL prepend `ShouldNotBeNull()` (proof `nullable`).
- **REQ-4**: IF the pattern is a `Regex`, a variable, empty, or the subject is not a string THEN THE SYSTEM SHALL give Manual (proof `regex-object, variable, empty, predicate`).

## Assumptions

- Probe: 22 wildcard cases (capitals, line endings, `\r\n`, special characters, trailing newline): 0 differences with `(?s)\A…$`. With `\z`, `"SO-1\n" ~ "SO-1"` failed.
- Probe: `MatchRegex("")` fails in FA, `ShouldMatch("")` passes → only non-empty constants.
- Reflection Shouldly 4.3.0: `ShouldMatch(string, string, string?)`, `ShouldNotMatch(string, string, string?)`.

## Risks

- Only the failure message differs; where the verdict could differ it is Manual.

## Models

Classes, order and decision tree: [fa-match-modellen.md](fa-match-modellen.md).

## See also

- [fa-match-verify.md](fa-match-verify.md)
- `claude/tasks/archief/TAAK-099-fa-match.md`
- [../README.md](../README.md)
