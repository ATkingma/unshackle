---
kingmadoc: 1
feature: "fa-not-throw"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/
---
# Feature: fa.not-throw: act.Should().NotThrow() becomes Should.NotThrow(act)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-011. Builds on `fa.throw` (TAAK-008): same subject rules.

## One-sentence summary

fa.not-throw: `act.Should().NotThrow()` on an `Action` or `Func<T>` becomes `Should.NotThrow(act)`.

## Scope (in / out)

**In scope**

- Standalone `NotThrow()` on a parameterless `Action` or `Func<T>` (also value types), with an optional `because` without `becauseArgs`.
- `Manual(reason)` for `NotThrow<T>`, `NotThrowAfter`, a used result, `Invoking(...)`, `becauseArgs`.

**Out of scope**

- `NotThrowAsync`/`NotThrowAfterAsync` (async, belongs to the async patterns).
- Use of `.Which` (the value of `Func<T>`): Shouldly returns it, but the shape differs; stays Manual.

## Requirements

- **REQ-1**: WHEN `act.Should().NotThrow()` is a statement on an `Action` THE SYSTEM SHALL replace it with `Should.NotThrow(act)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/simple.*.cs`).
- **REQ-2**: WHEN the subject is a `Func<T>` with T a reference type THE SYSTEM SHALL give `Should.NotThrow(func)` (proof `.../NotThrow/func.*.cs`).
- **REQ-3**: WHEN the subject is a `Func<T>` with T a value type THE SYSTEM SHALL give `Should.NotThrow(func)`, because `NotThrow<T>(Func<T>)` is generic (proof `.../NotThrow/value-func.*.cs`).
- **REQ-4**: WHEN there is a `because` without `becauseArgs` THE SYSTEM SHALL take it over as the second argument (`customMessage`) (proof `.../NotThrow/because.*.cs`).
- **REQ-5**: IF `becauseArgs` are used THEN THE SYSTEM SHALL give Manual (proof `because-args.*`).
- **REQ-6**: IF `NotThrow<T>()` or `NotThrowAfter(...)` is used THEN THE SYSTEM SHALL give Manual, because Shouldly has no equivalent (proofs `generic.*`, `after.*`).
- **REQ-7**: IF the result is used or chained (`.Which`, `.And`, variable) THEN THE SYSTEM SHALL give Manual (proofs `value-used.*`, `result-used.*`).
- **REQ-8**: IF the subject is built with `Invoking(...)` THEN THE SYSTEM SHALL give Manual (proof `invoking.*`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `Should.NotThrow(act, string.Format(CultureInfo.InvariantCulture, because, args))` (proof `because-args.*`, now `.after.cs`); named arguments or a non-constant `because` stay Manual. Replaces REQ-5.
- **Extension Invoking**: `x.Invoking(s => ...)` subjects are not found by this pattern's `Find` but by `fa.invoking` (`InvokingPattern`), which calls `NotThrowPattern.Rewrite`: with a stable reference receiver and a lambda it becomes `Should.NotThrow(() => ...)` (proof `Invoking/not-throw.*`); otherwise Manual (`Invoking/side-effect-receiver`, `value-type`, `method-group`). Another subject built with FluentAssertions (e.g. `FluentActions.Invoking`) stays Manual. Replaces REQ-8; there is no `NotThrow/invoking.*` proof.
- **Extension TAAK-100**: IF the assertion is inside an `AssertionScope` that is not converted by `fa.assertion-scope` THEN THE SYSTEM SHALL give Manual "Inside an AssertionScope" (no own proof; `Invoking/assertion-scope.*` shows the same check for Throw).

## Assumptions

- Shouldly 4.3.0 has `Should.NotThrow(Action[, string])` and `Should.NotThrow<T>(Func<T>[, string])`.
- `Func<Task>.Should()` has no synchronous `NotThrow` in FA 7.2.2 (compile error), so no guard is needed for it.

## Risks

- The error message gets "Additional Info:" instead of FA's prefix: a documented difference.
- `NotThrowAsync` stays untouched.

## Models

Class diagram, sequence diagram and decision tree: [fa-not-throw-modellen.md](fa-not-throw-modellen.md).

## Open questions

- [ ] What problem does this feature solve, and for whom?
- [ ] Who or what triggers it (end user, scheduled job, external system, ...)?
- [ ] Which external systems, services, or data stores does it interact with?
- [ ] Which existing modules will change? (detected modules: `samples`, `src/Unshackle.Core`, `src/Unshackle.Interface`, `src/Unshackle.Tests`)
- [ ] How will you know it works? List the acceptance criteria.
- [ ] _TODO: anything else that must be decided before implementation._
## Appendix: codebase context _(inferred)_

- **Tech stack:** C#
- **Entry points:** `src/Unshackle.Interface/Program.cs`
- **Config files:** _none found_
- **Test directories:** _none found_

| Language | Files |
|---|---|
| csharp | 209 |
| markdown | 55 |
| yaml | 6 |
| json | 2 |
| shell | 1 |

<details>
<summary>File tree (370 files)</summary>

```text
unshackle/
├── .claude/
│   ├── hooks/
│   │   └── check-pattern-docs.sh
│   ├── skills/
│   │   ├── taak-oppakken/
│   │   │   └── …
│   │   ├── taak-specifiek/
│   │   │   └── …
│   │   └── taak-start/
│   │       └── …
│   ├── scheduled_tasks.lock
│   └── settings.json
├── .github/
│   ├── ISSUE_TEMPLATE/
│   │   ├── config.yml
│   │   ├── missing-pattern.yml
│   │   ├── new-library.yml
│   │   └── wrong-rewrite.yml
│   └── workflows/
│       └── ci.yml
├── .vibe-wise/
│   ├── profile.md
│   ├── progress.md
│   └── project-map.md
├── claude/
│   ├── docs/
│   │   ├── agent-werkwijze.md
│   │   ├── code-standaarden.md
│   │   ├── design-patterns.md
│   │   ├── INDEX.md
│   │   └── patroon-loop.md
│   └── tasks/
│       ├── archief/
│       │   └── …
│       ├── patroon-queue.md
│       ├── TAAK-005-end-to-end-keten.md
│       ├── TAAK-010-fa-be-greater-less.md
│       ├── TAAK-011-fa-not-throw.md
│       └── TAAK-012-fa-throw-async.md
├── docs/
│   ├── projectinfo/
│   │   ├── architectuur/
│   │   │   └── …
│   │   ├── features/
│   │   │   └── …
│   │   ├── img/
│   │   │   └── …
│   │   ├── modellen/
│   │   │   └── …
│   │   ├── patterns/
│   │   │   └── …
│   │   ├── beslissingen.md
│   │   ├── project.md
│   │   └── README.md
│   └── issues.md
├── samples/
│   ├── SampleShop/
│   │   ├── SampleShop.Tests/
│   │   │   └── …
│   │   ├── SampleShop/
│   │   │   └── …
│   │   └── SampleShop.slnx
│   ├── .editorconfig
│   ├── Directory.Build.props
│   └── Directory.Packages.props
├── src/
│   ├── Unshackle.Core/
│   │   ├── Abstractions/
│   │   │   └── …
│   │   ├── Fixes/
│   │   │   └── …
│   │   ├── Models/
│   │   │   └── …
│   │   ├── Fixer.cs
│   │   ├── Unshackle.Core.csproj
│   │   └── UsingDirectives.cs
│   ├── Unshackle.DataAccess/
│   │   ├── Reports/
│   │   │   └── …
│   │   ├── Solutions/
│   │   │   └── …
│   │   └── Unshackle.DataAccess.csproj
│   ├── Unshackle.Interface/
│   │   ├── Commands/
│   │   │   └── …
│   │   ├── Output/
│   │   │   └── …
│   │   ├── Program.cs
│   │   └── Unshackle.Interface.csproj
│   └── Unshackle.Tests/
│       ├── Core/
│       │   └── …
│       ├── DataAccess/
│       │   └── …
│       ├── Helpers/
│       │   └── …
│       ├── Integration/
│       │   └── …
│       ├── Interface/
│       │   └── …
│       ├── Proofs/
│       │   └── …
│       ├── ProofFix.cs
│       ├── ProofTests.cs
│       └── Unshackle.Tests.csproj
├── .editorconfig
├── .featuredoc.yml
├── .gitignore
├── CLAUDE.md
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── LICENSE
├── README.md
└── Unshackle.slnx
```

</details>
