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
# Feature: fa.not-throw: act.Should().NotThrow() wordt Should.NotThrow(act)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-011. Bouwt voort op `fa.throw` (TAAK-008): zelfde subject-regels.

## One-sentence summary

fa.not-throw: `act.Should().NotThrow()` op een `Action` of `Func<T>` wordt `Should.NotThrow(act)`.

## Scope (in / out)

**In scope**

- Losse `NotThrow()` op een parameterloze `Action` of `Func<T>` (ook waardetypen), met optionele `because` zonder `becauseArgs`.
- `Manual(reden)` voor `NotThrow<T>`, `NotThrowAfter`, gebruikt resultaat, `Invoking(...)`, `becauseArgs`.

**Out of scope**

- `NotThrowAsync`/`NotThrowAfterAsync` (async, hoort bij de async-patronen).
- Gebruik van `.Which` (de waarde van `Func<T>`): Shouldly geeft die terug, maar de vorm verschilt; blijft Manual.

## Requirements

- **REQ-1**: WHEN `act.Should().NotThrow()` als statement op een `Action` staat THE SYSTEM SHALL het vervangen door `Should.NotThrow(act)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/simple.*.cs`).
- **REQ-2**: WHEN het subject een `Func<T>` met T een referentietype is THE SYSTEM SHALL `Should.NotThrow(func)` geven (proof `.../NotThrow/func.*.cs`).
- **REQ-3**: WHEN het subject een `Func<T>` met T een waardetype is THE SYSTEM SHALL `Should.NotThrow(func)` geven, want `NotThrow<T>(Func<T>)` is generiek (proof `.../NotThrow/value-func.*.cs`).
- **REQ-4**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als tweede argument (`customMessage`) overnemen (proof `.../NotThrow/because.*.cs`).
- **REQ-5**: IF `becauseArgs` worden gebruikt THEN THE SYSTEM SHALL Manual geven (proof `because-args.*`).
- **REQ-6**: IF `NotThrow<T>()` of `NotThrowAfter(...)` wordt gebruikt THEN THE SYSTEM SHALL Manual geven, omdat Shouldly geen equivalent heeft (proofs `generic.*`, `after.*`).
- **REQ-7**: IF het resultaat wordt gebruikt of geketend (`.Which`, `.And`, variabele) THEN THE SYSTEM SHALL Manual geven (proofs `value-used.*`, `result-used.*`).
- **REQ-8**: IF het subject met `Invoking(...)` is gebouwd THEN THE SYSTEM SHALL Manual geven (proof `invoking.*`).

## Assumptions

- Shouldly 4.3.0 heeft `Should.NotThrow(Action[, string])` en `Should.NotThrow<T>(Func<T>[, string])`.
- `Func<Task>.Should()` heeft in FA 7.2.2 geen synchrone `NotThrow` (compileerfout), dus daar hoeft geen guard voor.

## Risks

- Foutmelding krijgt "Additional Info:" in plaats van FA's voorvoegsel: gedocumenteerd verschil.
- `NotThrowAsync` blijft onaangeroerd.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-not-throw-modellen.md](fa-not-throw-modellen.md).

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
