---
kingmadoc: 1
feature: "fa-should-be"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ShouldBePattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/
---
# Feature: fa.should-be: x.Should().Be(y) becomes x.ShouldBe(y), keeping the because argument.

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-10T23:33+02:00 by KingmaDoc 0.3.0.dev9 |

> Drawn up afterwards from TAAK-001, after the build. This plan describes what was built and why; it is not an up-front design.
> Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.should-be: x.Should().Be(y) becomes x.ShouldBe(y), keeping the because argument.

## Scope (in / out)

**In scope**

- `x.Should().Be(y[, because])` as a standalone assertion (statement or body of a void lambda) to `x.ShouldBe(y[, because])`.
- Type-dependent adjustment of the expected argument: a cast when a constant only converts implicitly to the subject type (`byte`, enum).
- `Manual(reason)` for every case that cannot be rewritten safely.
- Only the pattern itself; the `using` changes (`FluentAssertions` out, `Shouldly` in) are done by the `Fixer`.

**Out of scope**

- `NotBe` and other `Be*` methods (own patterns).
- Splitting `.And`/`.Which` chains.
- Csproj changes (package out, Shouldly in).
- `Be(expected, comparer)`: the comparer is not passed to `ShouldBe`.

## Requirements

- **REQ-1**: WHEN `x.Should().Be(y)` is a standalone assertion THE SYSTEM SHALL rewrite it to `x.ShouldBe(y)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/simple.*.cs`, with `int` and `string`).
- **REQ-2**: WHEN `Be` gets a `because` string without `becauseArgs` THE SYSTEM SHALL take it over as the second argument of `ShouldBe` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/because.*.cs`).
- **REQ-3**: WHEN `Be` is the body of a void lambda (e.g. `Array.ForEach(totals, t => t.Should().Be(20m))`) THE SYSTEM SHALL rewrite that too (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/lambda.*.cs`).
- **REQ-4**: WHEN the expected argument is a constant that only converts implicitly to the subject type (`byte`, enum) THE SYSTEM SHALL add a cast to the subject type, and no cast for `long` with `1` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/constant-cast.*.cs`).
- **REQ-5**: IF the result of `Be` is used (`.And`, `.Which`, value) THEN THE SYSTEM SHALL give a Manual finding and leave the code unchanged (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/chain.*`).
- **REQ-6**: IF `Be` has `becauseArgs` or named arguments THEN THE SYSTEM SHALL give Manual because Shouldly does not format (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/because-args.*`).
- **REQ-7**: IF `Be` is given a comparer THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/comparer.*`).
- **REQ-8**: IF the expected type cannot safely be brought to the subject type (e.g. `int` against `int?`) THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/nullable-expected.*`).
- **REQ-9**: IF `Be` comes from `FluentAssertions.Xml` or `FluentAssertions.Types` THEN THE SYSTEM SHALL give Manual because the equality is different (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/xml.*`).
- **REQ-10**: IF the type of the subject is unknown (error type) or `Be` is not in the form `x.Should().Be(...)` THEN THE SYSTEM SHALL give Manual. _No proof: guarded by code only._
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `x.ShouldBe(y, string.Format(CultureInfo.InvariantCulture, because, args))` (proof `because-args.*`, now `.after.cs`); named arguments, a non-constant or non-formattable `because` stay Manual (proofs `edge-named-because.*`, `because-args-variable.*`, `because-args-bad-format.*`). Replaces REQ-6.
- **Extension TAAK-100**: IF the assertion is inside an `AssertionScope` that is not converted by `fa.assertion-scope` THEN THE SYSTEM SHALL give Manual "Inside an AssertionScope" (proofs `assertion-scope-nested.*`, `assertion-scope-using-block.*`, `assertion-scope-using-var.*`).
- **Extension TAAK-169**: WHEN the expected argument only reaches the subject type via a user-defined implicit conversion (`BsonValue b; b.Should().Be(42)`) THE SYSTEM SHALL add a cast to the subject type: `b.ShouldBe((BsonValue)42)`. Type inference of `ShouldBe<T>` does not use such conversions (CS1929/CS0411). Measured (FA 7.2.2 vs Shouldly 4.3.0): with the cast both compare with `Equals`, same outcome for equal, unequal and a class without an `Equals` override (reference equality, fails in both) (proof `implicit-conversion.*`).
- **Extension TAAK-199**: IF a rewritten `ShouldBe` (or other Shouldly extension) after the rewrite does not bind to Shouldly but to an own method (e.g. an own `ShouldBe<T>` in the enclosing namespace, which without ambiguity wins over `using Shouldly;`, such as `Akka.TestKit.AkkaSpecExtensions.ShouldBe`) THEN THE SYSTEM SHALL leave the file unchanged and give Manual "binds to <type>.<method> instead of Shouldly" (proof `own-shouldbe-in-namespace.*`; check `UsingDirectives.MisboundAddedCallAsync` in the `Fixer`, next to the CS0121 check of TAAK-174).
- **Extension TAAK-198**: the fallback of TAAK-174/199 no longer applies per file but per call: a `ShouldBe` that would clash or bind wrongly stays FA (Manual with the same reason), the rest of the file is migrated with targeted `using static` imports (see `fa-be-true-false-plan.md` and `architectuur/core.md`). Only if those imports break something else does the whole file stay as it is.

## Assumptions

- _(inferred)_ Built on the existing stack: C#, Roslyn, Shouldly 4.3.0 as target, FluentAssertions 7.2.2 as source.
- The foundation from TAAK-001 (`IPattern`, `Fixer`, `ProofTests`) exists; this pattern was the first and proves the whole chain.
- `ShouldBe<T>(actual, expected)` infers T from both sides and allows no numeric or nullable conversion on the receiver.
- The `Fixer` handles the usings and the trivia; the pattern builds bare nodes.
- Equality of `Be` (`Equals`) matches `ShouldBe`, except in the Manual cases above.

## Risks

- An FA conversion of the argument that cannot be captured with a cast (e.g. `Guid` against `string`): becomes Manual instead of a guess. Limited to cases where `ClassifyConversion` finds no safe form.
- Shouldly's error message differs from FA's; no Manual needed, but a documented difference.
- Only the `because` text is kept; format args are not lost because those become Manual.
- Behaviour difference (TAAK-200, measured on Akka DData): FA `Be`/`NotBe` calls `actual.Equals(expected)`, Shouldly `expected.Equals(actual)`. With an asymmetric `Equals` (Akka `ORMultiValueDictionaryDelta`) the outcome can flip. User decision: **Fixed with a warning** in the report ("Shouldly compares expected.Equals(actual), FluentAssertions actual.Equals(expected); check symmetry of X.Equals") when the subject or expected type (or a base type) declares an `Equals(x)` outside the BCL itself, or when both sides have an unknown runtime type (`object`, type parameter, interface). No warning for BCL types (assembly `System*`/`Microsoft*`/`mscorlib`/`netstandard`) and records without their own `Equals` (generated, symmetric). Unit tests `ShouldBeEqualsOrderTests`, proof `own-equals.*`.

## Models

Class diagram, sequence diagram and decision tree: [fa-should-be-modellen.md](fa-should-be-modellen.md).

## Open questions

- **What if `Be` uses a different equality than `ShouldBe`?** Only for XML and `MethodInfoSelector` (Types); those are Manual.
- **How does the using get added?** Via the `Fixer` (`IFix.AddedNamespaces`), not via the pattern.

**Answered while planning**

- [x] What does this solve and for whom? Developers who want to get rid of FluentAssertions; `Be` is the most used assertion (samples: `OrderTests.cs:24,61`, `OrderHandlerTests.cs:26,27,69`).
- [x] Who or what triggers it? The `Fixer` runs the pattern per document.
- [x] External systems? Roslyn (SemanticModel); no data store.
- [x] Which modules change? `Unshackle.Core` (pattern and registration in `FluentAssertionsFix`) and `Unshackle.Tests` (proofs).
- [x] How do we know it works? `ProofTests` runs all proofs: before compiles against FA, after is exactly equal and compiles against Shouldly; the manual.txt reason appears in the report.

## Appendix: codebase context _(inferred)_

- **Tech stack:** C#
- **Entry points:** `src/Unshackle.Interface/Program.cs`
- **Config files:** _none found_
- **Test directories:** _none found_

| Language | Files |
|---|---|
| csharp | 76 |
| markdown | 24 |
| json | 2 |
| yaml | 2 |
| shell | 1 |

<details>
<summary>File tree (142 files)</summary>

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
