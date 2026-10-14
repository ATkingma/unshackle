# TAAK-001 — Foundation: contracts, Fixer, proof runner + `fa.should-be`

**Status**: ✅ Done
**Created**: 2026-10-10
**Goal**: Core contracts, `Fixer`, `FluentAssertionsFix`, proof runner and the first pattern `fa.should-be`, with a green build and tests.

## Context for the next agent

- Row 0 in `claude/tasks/patroon-queue.md`; all pattern agents wait on this and must not change the contracts.
- Code standards: `claude/docs/code-standaarden.md`. Architecture: `ARCHITECTURE.md` section "Binnen Core".
- Pattern agents: use `FluentAssertionsSyntax` (see Choices) and look at `ShouldBePattern` as an example.

## Analysis

### Current situation

Only empty folders with `.gitkeep`; `src/Unshackle.Interface/Program.cs` returns `0`. No tests (exit code 8).

### Problem

Without contracts and a proof runner, no pattern agent can start or prove its work.

### New situation

`IPattern`/`IFix`/`IFixer`, models, `Fixer`, `FluentAssertionsFix` with `ShouldBePattern`, test helpers, `ProofTests` and `FixerTests`.

### Improvement

A new pattern = one class + one line in `Patterns` + proof files. Proofs check that before compiles with FA and after with Shouldly.

## Approach

### What / Where

- `src/Unshackle.Core/Abstractions/` `IPattern.cs`, `IFix.cs`, `IFixer.cs`
- `src/Unshackle.Core/Models/` `Finding.cs`, `PatternResult.cs`, `FixedResult.cs`, `ManualResult.cs`, `FixRequest.cs`, `FixReport.cs`
- `src/Unshackle.Core/Fixer.cs`, `src/Unshackle.Core/UsingDirectives.cs`
- `src/Unshackle.Core/Fixes/FluentAssertions/` `FluentAssertionsFix.cs`, `FluentAssertionsSyntax.cs`, `Patterns/ShouldBePattern.cs`
- `src/Unshackle.Tests/Helpers/` `TestReferences.cs`, `TestSolution.cs`; `Core/FixerTests.cs`; `ProofTests.cs` + `ProofFix.cs` (project root: `Proofs/**` is not compiled); `Proofs/FluentAssertions/ShouldBe/*`

### Why this approach

See **Choices**.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | `IPattern { Id; Description; IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model); PatternResult Rewrite(SyntaxNode node, SemanticModel model); }`, synchronous, without CancellationToken | Roslyn work on one tree is CPU-bound; the Fixer checks the token between patterns. `Find` also returns nodes that can only be Manual (that way they end up in the report) |
| 2 | A pattern chooses itself which node it replaces (expression, `.WithMessage` chain, or whole statement) | `Throw` (statement form changes) and chains: Find returns the outermost invocation or the statement |
| 3 | `FixedResult(IReadOnlyList<SyntaxNode> NewNodes, IReadOnlyList<string> RequiredNamespaces)`; factories `Fixed(node, params namespaces)` and `Fixed(IEnumerable<SyntaxNode>, params namespaces)` | Multiple nodes = splitting a statement (`.And` chains). Namespaces: requirement from TAAK-002 (`Count()` → `System.Linq`). Deviates from the example in code standards §2, deliberately |
| 4 | Rewriting in the Solution: per document, per pattern in order, each time with a fresh SemanticModel. All `Rewrite`s on the original tree, then `TrackNodes` + `ReplaceNode` in document order (outermost first); overlap → Manual "run the fix again". `Solution.WithDocumentSyntaxRoot` | Immutable Roslyn: the input solution stays intact (dry run for free). Patterns see each other's result; no conflicts between patterns |
| 5 | The Fixer sets trivia: one node → `WithTriviaFrom(original)`; multiple → the first gets leading trivia, the rest only indentation, all get the trailing trivia | Patterns build bare nodes; consistent formatting |
| 6 | Usings at document level in the Fixer (`UsingDirectives`): `IFix.AddedNamespaces` (`Shouldly`) + `RequiredNamespaces` of the results are added if they are not there (not even as `global using` in the compilation); `IFix.RemovedNamespaces` (`FluentAssertions`) is removed if no simple name in the document still binds to that namespace. When removing + adding, the using is converted in the same place | Once per document instead of per pattern; never remove FA while FA code (e.g. Manual) is still present. Global usings and usings in namespace blocks are left alone (shortcut) |
| 7 | `Finding { FilePath, Line (1-based, 0 = whole file), FixId, PatternId, Result }`; `FixReport { Solution, Findings }`; `FixRequest { FixId }` | Covers the CSV columns from ARCHITECTURE.md; `Result` provides Status + Reason |
| 8 | Exceptions in `Find`, `Rewrite` and during replacement → `Manual("Internal error in <id>: …")` | Code standards §8 |
| 9 | Helper class `FluentAssertionsSyntax`: `FindInvocations(root, model, names)` (FA symbols only), `GetSubject` (x in `x.Should().M()`), `IsResultDiscarded` (statement or void lambda), `TryGetArguments` (values + `because`; false for `becauseArgs`/named args, via `IInvocationOperation`) | 14 FA patterns all need this; pattern agents may only change their own file |
| 10 | `fa.should-be`: `x.ShouldBe(y[, because])` with the original argument list. Manual for: not `x.Should().Be`, result used (`.And`), `becauseArgs`/named args, comparer, `FluentAssertions.Xml`/`.Types` (different equality), type difference without a safe form (e.g. `int` vs `int?`, Guid vs string) | `ShouldBe<T>` infers T from both sides and the receiver allows no numeric/nullable conversion; FA converted the argument |
| 11 | Type-dependent: a constant that only implicitly converts to the subject type gets a cast (`byte`: `(byte)1`, enum: `(Status)0`); `long` with `1` stays without a cast | Otherwise `b.ShouldBe(1)` does not compile (T = int) |
| 12 | Proofs: `ProofTests` runs per proof only the pattern from the folder name (`<Map>Pattern`) via an NSubstitute `IFix` with the namespaces of the real fix. `before` compiles against FA, `after` exactly equal and compiles against Shouldly only; `manual.txt` = all findings Manual, reason contains the text, document unchanged | Isolation between parallel pattern agents. A proof with remaining FA code next to Shouldly therefore does not fit: `FixerTests` covers that |
| 13 | Test names `Method_State_Expected` (**provisional**); CA1707 off for `src/Unshackle.Tests/**` in `.editorconfig` | ARCHITECTURE.md: test names still to be decided |
| 14 | `TestSolution` uses one static `AdhocWorkspace` | Solution is immutable; no dispose hassle |

## Steps

- [x] 1. Research FA 7.2.2 `Be` / Shouldly 4.3.0 `ShouldBe` and samples
- [x] 2. Contracts and models
- [x] 3. Fixer + usings
- [x] 4. `FluentAssertionsFix`, helper, `ShouldBePattern`
- [x] 5. Test helpers, `ProofTests`, proofs, `FixerTests`
- [x] 6. Build + test green (17 tests), wrap up

## Risks & edge cases

| Risk                                                           | Mitigation                                                                    |
| -------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| Overlapping findings from one pattern                          | Fixer applies in document order; overlap → `Manual` ("run the fix again")     |
| Removing `using FluentAssertions` while FA code is still there | Only remove if no symbol in the document still binds to that namespace        |

## Changed files

- Core: `Abstractions/{IPattern,IFix,IFixer}.cs`, `Models/{Finding,PatternResult,FixedResult,ManualResult,FixRequest,FixReport}.cs`, `Fixer.cs`, `UsingDirectives.cs`, `Fixes/FluentAssertions/{FluentAssertionsFix,FluentAssertionsSyntax}.cs`, `Fixes/FluentAssertions/Patterns/ShouldBePattern.cs`
- Tests: `Helpers/{TestReferences,TestSolution}.cs`, `Core/FixerTests.cs`, `ProofTests.cs`, `ProofFix.cs`, `Proofs/FluentAssertions/ShouldBe/` (simple, because, constant-cast, lambda; manual: chain, because-args, nullable-expected, comparer, xml)
- `.editorconfig` (CA1707 off in tests), `.gitkeep`s removed from filled folders

### Open points

- FA research `Be`: FA 7.2.2 has `Be(expected, because, params becauseArgs)` on 20 assertion types; `ObjectAssertions` also with `IEqualityComparer`. Samples: `OrderTests.cs:24,61`, `OrderHandlerTests.cs:26,27,69` (lambda in `AllSatisfy`).
- Csproj work (FA package out, Shouldly in, `<Using Include="FluentAssertions" />`) is outside Roslyn: later in DataAccess/`ISolutionStore`.
- `ISolutionStore`/`IReportWriter` and the CLI wiring have not been built yet (outside row 0).

## See also

- `claude/docs/INDEX.md`
- `ARCHITECTURE.md`
- `claude/docs/patroon-loop.md`
