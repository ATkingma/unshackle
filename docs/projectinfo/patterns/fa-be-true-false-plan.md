---
kingmadoc: 1
feature: "fa-be-true-false"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/
---
# Feature: fa.be-true-false: x.Should().BeTrue()/BeFalse() becomes x.ShouldBeTrue()/ShouldBeFalse() (bool) or…

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-10T23:36+02:00 by KingmaDoc 0.3.0.dev9 |

> Drafted from TAAK-003 (prior research, adjusted to the real contract during implementation).
> Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.be-true-false: x.Should().BeTrue()/BeFalse() becomes x.ShouldBeTrue()/ShouldBeFalse() (bool) or…

**Full description:** fa.be-true-false: x.Should().BeTrue()/BeFalse() becomes x.ShouldBeTrue()/ShouldBeFalse() (bool) or x.ShouldBe(true/false) (bool?).

## Scope (in / out)

**In scope**

- `x.Should().BeTrue()` / `BeFalse()` (with an optional `because` without `becauseArgs`) as a standalone assertion on `bool` and `bool?`.
- `bool` becomes `ShouldBeTrue`/`ShouldBeFalse`; `bool?` becomes `ShouldBe(true)`/`ShouldBe(false)`, because Shouldly has no `bool?` overload.
- `Manual(reden)` for `becauseArgs`, chains, used results and unknown types.

**Out of scope**

- `NotBeTrue`/`NotBeFalse` (only `bool?`, different semantics for `null`) and `Be(true)` (pattern `fa.should-be`).
- Splitting chains with `.And`/`.Which`.
- Managing `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `x.Should().BeTrue()` is used on a `bool` THE SYSTEM SHALL rewrite it to `x.ShouldBeTrue()` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/be-true.*.cs`).
- **REQ-2**: WHEN `x.Should().BeFalse()` is used on a `bool`, including on a parenthesized subject such as `(a == b)`, THE SYSTEM SHALL rewrite it to `x.ShouldBeFalse()` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/be-false.*.cs`).
- **REQ-3**: WHEN `BeTrue`/`BeFalse` has a `because` without `becauseArgs` THE SYSTEM SHALL carry it over as `customMessage` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/because.*.cs`).
- **REQ-4**: WHEN the subject is a `bool?` THE SYSTEM SHALL build `x.ShouldBe(true)` or `x.ShouldBe(false)` respectively, with `because` as the second argument (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/nullable.*.cs`).
- **REQ-5**: IF `becauseArgs` or named arguments are used THEN THE SYSTEM SHALL produce Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/because-args.*`).
- **REQ-6**: IF the assertion is part of a chain (`.BeTrue().And...`) THEN THE SYSTEM SHALL produce Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/chain.*`).
- **REQ-7**: IF the result of `BeTrue`/`BeFalse` is used as a value (`var c = ...`) THEN THE SYSTEM SHALL produce Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/value-used.*`).
- **REQ-8**: IF the subject's type is unknown or not `bool`/`bool?` THEN THE SYSTEM SHALL produce Manual. _No proof: before would not compile; guarded in code only._
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL produce `string.Format(CultureInfo.InvariantCulture, because, args)` as `customMessage` (proof `.../BeTrueFalse/because-args.after.cs`); named arguments or a non-constant `because` remain Manual. Supersedes REQ-5 for this case.
- **Extension TAAK-100**: IF the assertion is inside an `AssertionScope` that `fa.assertion-scope` does not convert THEN THE SYSTEM SHALL produce Manual ("Inside an AssertionScope").
- **Extension TAAK-198**: IF the file has an own extension with a Shouldly name in scope (Akka.TestKit's `ShouldBe`/`ShouldBeTrue`) THEN THE SYSTEM SHALL, instead of `using Shouldly;`, import only the non-clashing Shouldly classes (`using static Shouldly.ShouldBeBooleanExtensions;`, alias `using Should = Shouldly.Should;`) and leave only the calls that resolve to a clashing name Manual (proof `.../BeTrueFalse/own-shouldbe-in-scope.*`; `Fixer.FixWithTargetedImportsAsync`). Applies to all FA patterns.

## Assumptions

- _(inferred)_ Built on the existing stack: C#.
- Shouldly 4.3.0 has `ShouldBeTrue(bool, string)`/`ShouldBeFalse(bool, string)` but no `bool?` overload (checked via reflection).
- `ShouldBe(true)` on `bool?` also fails on `null`, same as FA `BeTrue()`.
- `customMessage` is a plain string without `{0}` formatting.
- The `Fixer` adds `using Shouldly;` and removes `using FluentAssertions;` via `IFix`; the pattern does not manage usings.

## Risks

- The failure message gets "Additional Info:" instead of FA's "because" prefix: documented difference, not Manual.
- An own `BeTrue` outside FluentAssertions is not rewritten: `FindInvocations` checks the symbol's namespace.
- The subject expression is taken over unchanged (the parentheses are already in the source before `.Should()`).

## Models

Class diagram, sequence diagram and decision tree: [fa-be-true-false-modellen.md](fa-be-true-false-modellen.md).

## Open questions

- No open questions. `NotBeTrue`/`NotBeFalse` on `bool?` may become a pattern of their own later.

**Answered while planning**

- [x] What does this solve and for whom? Developers who want to get rid of FluentAssertions; sample: `OrderTests.cs:62`.
- [x] Who or what triggers it? The `Fixer` runs the pattern per document.
- [x] External systems? Roslyn (SemanticModel); no data store.
- [x] Which modules change? `Unshackle.Core` (pattern and registration) and `Unshackle.Tests` (proofs).
- [x] How do we know it works? `ProofTests` runs all 7 proofs.

## Appendix: codebase context _(inferred)_

- **Tech stack:** C#
- **Entry points:** `src/Unshackle.Interface/Program.cs`
- **Config files:** _none found_
- **Test directories:** _none found_

| Language | Files |
|---|---|
| csharp | 88 |
| markdown | 28 |
| json | 2 |
| yaml | 2 |
| shell | 1 |

<details>
<summary>File tree (161 files)</summary>

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
│   └── settings.json
├── .github/
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
│       └── TAAK-003-fa-be-true-false.md
├── docs/
│   └── projectinfo/
│       ├── architectuur/
│       │   └── …
│       ├── patterns/
│       │   └── …
│       ├── beslissingen.md
│       ├── project.md
│       └── README.md
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
