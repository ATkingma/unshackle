---
kingmadoc: 1
feature: "fa-have-count"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14, REQ-15, REQ-16]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/
---
# Feature: fa.have-count: x.Should().HaveCount(n) becomes, depending on the type, x.Count.ShouldBe(n), x.Length.ShouldBe(n) or x.Count().ShouldBe(n).

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-10T23:33+02:00 by KingmaDoc 0.3.0.dev9 |

> Drawn up afterwards from TAAK-002, after the build. This plan describes what was built and why; it is not an up-front design.
> Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.have-count: x.Should().HaveCount(n) becomes, depending on the type, x.Count.ShouldBe(n), x.Length.ShouldBe(n) or x.Count().ShouldBe(n).

**Full description:** fa.have-count: x.Should().HaveCount(n) becomes, depending on the type, x.Count.ShouldBe(n), x.Length.ShouldBe(n) or x.Count().ShouldBe(n).

## Scope (in / out)

**In scope**

- `x.Should().HaveCount(n[, because])` as a standalone assertion, rewritten based on the type of `x`: `Count` property, `Length` (arrays) or LINQ `Count()` with `using System.Linq`.
- `because` without `becauseArgs` becomes the `customMessage` of `ShouldBe`.
- Parentheses around a receiver with lower precedence (`(a ?? b).Count`).
- `Manual(reason)` for dictionaries, predicates, `becauseArgs`, chains, `?.`, non-generic `IEnumerable` and unknown types.

**Out of scope**

- `NotHaveCount`, `HaveCountGreaterThan(OrEqualTo)`, `HaveCountLessThan(OrEqualTo)` (other patterns).
- Predicate rewriting (`c => c > 1`) to `ShouldBeGreaterThan` etc.
- Splitting chains with `.And`/`.Which`.
- The `using` handling itself: the `Fixer` adds `System.Linq` if it is missing.

## Requirements

- **REQ-1**: WHEN `x.Should().HaveCount(n)` is on a type with a public `int Count` (e.g. `List<T>`) THE SYSTEM SHALL rewrite it to `x.Count.ShouldBe(n)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/list.*.cs`).
- **REQ-2**: WHEN `HaveCount` has a `because` without `becauseArgs` THE SYSTEM SHALL take it over as the second argument of `ShouldBe` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/list-because.*.cs`).
- **REQ-3**: WHEN the subject is `ICollection<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `IList<T>` or `HashSet<T>` THE SYSTEM SHALL use `.Count`, also when `Count` comes from a base interface (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/collection-interfaces.*.cs`).
- **REQ-4**: WHEN the subject is an array, or a type with `Length` but without `Count` (e.g. `ImmutableArray<T>`), THE SYSTEM SHALL use `.Length.ShouldBe(n)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/array.*.cs`; `ImmutableArray<T>` has no proof).
- **REQ-5**: WHEN the subject is a generic `IEnumerable<T>` without `Count`/`Length` THE SYSTEM SHALL build `x.Count().ShouldBe(n)` and have `System.Linq` added (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/enumerable.*.cs`).
- **REQ-6**: WHEN the document already has `using System.Linq;` THE SYSTEM SHALL not add it again (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/enumerable-existing-using.*.cs`).
- **REQ-7**: WHEN a generic `IEnumerable<T>` has a `because` THE SYSTEM SHALL take it over as the second argument of `ShouldBe` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/enumerable-because.*.cs`).
- **REQ-8**: WHEN the subject is an expression with lower precedence (e.g. `a ?? b`) THE SYSTEM SHALL put parentheses around the receiver (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/receiver-parens.*.cs`).
- **REQ-9**: IF the subject is a dictionary (`IDictionary<,>`, `IReadOnlyDictionary<,>` or an implementation) THEN THE SYSTEM SHALL give Manual because Count or Keys may be meant (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/dictionary.*`).
- **REQ-10**: IF `HaveCount` gets a predicate (`c => c > 1`) THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/predicate.*`).
- **REQ-11**: IF `HaveCount` has `becauseArgs` or named arguments THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/because-args.*`). _For becauseArgs replaced by Extension TAAK-097._
- **REQ-12**: IF the result of `HaveCount` is used (`.And`, `.Which`, value) THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/chain.*`).
- **REQ-13**: IF the subject is a non-generic `IEnumerable` THEN THE SYSTEM SHALL give Manual with the advice `Cast<T>().Count()`. _No proof: FA 7.2.2 has no `HaveCount` on non-generic `IEnumerable`, so before does not compile._
- **REQ-14**: IF the subject is a conditional access (`a?.b`) THEN THE SYSTEM SHALL give Manual. _No proof: guarded by code only._
- **REQ-15**: IF the type of the subject is unknown (error type) THEN THE SYSTEM SHALL give Manual. _No proof: guarded by code only._
- **REQ-16**: IF the subject may be null in a nullable-enabled context (flow state MaybeNull) THEN THE SYSTEM SHALL write `x.ShouldNotBeNull().Count.ShouldBe(n)` (TAAK-021: FluentAssertions accepts a nullable subject, Shouldly's receiver does not; without this CS8602/CS8604 in projects with warnings-as-errors. `ShouldNotBeNull()` keeps FA's failure on null) (proof `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/nullable.*`).
- **Extension TAAK-095**: WHEN `HaveCountGreaterThan`/`…OrEqualTo`/`HaveCountLessThan`/`…OrEqualTo` (also the `…GreaterOrEqualTo`/`…LessOrEqualTo` aliases) is used THE SYSTEM SHALL give the same count receiver with `ShouldBeGreaterThan`/`…OrEqualTo`/`ShouldBeLessThan`/`…OrEqualTo` (proofs `count-comparisons`, `count-comparison-because-args`). Probe: boundary n-1, n, n+1 equal.
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (Fixed, proofs `because-args`, `count-comparison-because-args`); named arguments or a non-constant `because` stay Manual. Since the Template Method, the shared checks (AssertionScope, shape, result, becauseArgs) live in `FluentAssertionPattern.Rewrite`; `HaveCountPattern` implements `RewriteAssertion` and uses `FluentAssertionsSyntax.NotNullReceiver` (parentheses and `ShouldNotBeNull()`) instead of its own `IsSafeReceiver`.

## Assumptions

- _(inferred)_ Built on the existing stack: C#, Roslyn, Shouldly 4.3.0 as target, FluentAssertions 7.2.2 as source.
- Shouldly has no `ShouldHaveCount`; asserting the counter itself (`x.Count.ShouldBe(n)`) is the equivalent.
- Shouldly's `customMessage` is a bare string without `{0}` formatting, so `becauseArgs` cannot come along.
- The `Fixer` can have a using added via `PatternResult.Fixed(node, "System.Linq")` if it is missing.
- `Count()` and FA both enumerate once: behaviour with lazy sequences is the same.

## Risks

- A `Count` property that is not an `int` on an own type: only a public `int` property is accepted, otherwise another branch or Manual follows.
- The error message gets shorter (Shouldly shows "x.Count should be 2 but was 3" instead of the whole collection): a documented difference, no Manual.
- Dictionary intent (number of keys or entries) cannot be inferred: deliberately Manual.
- Multidimensional arrays fall under `IArrayTypeSymbol` and get `Length` (total number of elements), just as FA counts. No proof.

## Models

Class diagram, sequence diagram and decision tree: [fa-have-count-modellen.md](fa-have-count-modellen.md).

## Open questions

- **Should a pattern be able to add a using?** Solved via `PatternResult.Fixed(node, "System.Linq")` (TAAK-002, adjustments during realisation).
- **Should `HaveCount` with a predicate become automatic later?** Still open: possibly separate patterns for `HaveCountGreaterThan` etc.

**Answered while planning**

- [x] What does this solve and for whom? Developers who want to get rid of FluentAssertions; samples: `OrderTests.cs:33,42`, `OrderHandlerTests.cs:68`, `RepositoryTests.cs:16` (dictionary, Manual).
- [x] Who or what triggers it? The `Fixer` runs the pattern per document.
- [x] External systems? Roslyn (SemanticModel); no data store.
- [x] Which modules change? `Unshackle.Core` (pattern and registration in `FluentAssertionsFix`) and `Unshackle.Tests` (proofs).
- [x] How do we know it works? `ProofTests` runs all 12 proofs (before with FA, after with Shouldly, manual.txt with reason).

## Appendix: codebase context _(inferred)_

- **Tech stack:** C#
- **Entry points:** `src/Unshackle.Interface/Program.cs`
- **Config files:** _none found_
- **Test directories:** _none found_

| Language | Files |
|---|---|
| csharp | 76 |
| markdown | 25 |
| json | 2 |
| yaml | 2 |
| shell | 1 |

<details>
<summary>File tree (143 files)</summary>

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
