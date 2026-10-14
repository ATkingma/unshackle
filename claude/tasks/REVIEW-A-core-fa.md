# REVIEW-A: Core + FluentAssertions fix

Read-only review on `main` 3bd36cf. Scope: `src/Unshackle.Core/` (Fixer, UsingDirectives, ProjectPackages, Abstractions, Models) and `src/Unshackle.Core/Fixes/FluentAssertions/`.
Not done: running build/tests, walking through the proofs per pattern, verifying Shouldly overloads per pattern against the real assembly.

## High

### H1. BeGreaterLess produces non-compiling code for byte/short/sbyte/ushort

`Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs:135-141`

- **Problem:** `byte b; b.Should().BeGreaterThan(1)` becomes `b.ShouldBeGreaterThan(1)`. Shouldly's `ShouldBeGreaterThan<T>(this T, T)` infers `T = int`, and the receiver may not undergo a numeric conversion, so that gives CS1929. The same applies to `BeInRange`/`NotBeInRange`. FluentAssertions converted the literal to the subject's type. The result is Fixed, while the build breaks.
- **Why:** `ShouldBePattern.ConvertExpected` solves exactly this (a cast to the subject type) and `NotBePattern` already uses it. This pattern forgot it.
- **Fix:** pass every value in `values` through `ConvertExpected(value.Expression, type, model, link.SpanStart)`. On `null` return Manual. Add a proof with `byte` and `short`.

### H2. AndChain loses a link's `FixedResult.Warning`

`Fixes/FluentAssertions/Patterns/AndChainPattern.cs:108-119`, `140`

- **Problem:** `BeEquivalentToObjectPattern:101` sets a `Warning` (behavior difference with derived types). If that link comes in via an `.And` chain, `RewriteLink` still keeps it (`fixedResult with {...}`), but `Rewrite` builds a new `PatternResult.Fixed(statements, ...)` and throws the warning away. The report then shows a silent Fixed while there is a semantic difference.
- **Fix:** collect the warnings of the links and put them on the result: `(FixedResult)PatternResult.Fixed(...) with { Warning = string.Join(" ", warnings) }` (or `null` if the list is empty).

### H3. A Fixer crash outside the try/catch aborts the whole run

`Fixer.cs:107-108`

- **Problem:** `DescendantTrivia().First(t => DisabledTextTrivia)` runs on the root _before_ the usings update, while `KeepsRemovedUsing` is evaluated on the root _after_ the update. Patterns that reparse text (`AllSatisfyPattern` does `ParseStatement` with lambda body text) can move or create disabled trivia. Then `First` throws an `InvalidOperationException` that nobody catches, and the whole `FixAsync` is gone instead of a single Manual line.
- **Fix:** use `updatedRoot.DescendantTrivia().FirstOrDefault(...)` with a fallback to line 0. Use the same root as the one the condition runs on.

## Medium

### M1. The same preamble in ~20 patterns (Template Method missing)

All `Patterns/*.cs` (e.g. `ShouldBePattern.cs:20-47`, `HaveCountPattern.cs:20-50`, `BeNullPattern.cs:20-40`, `BeEmptyPattern.cs:20-40`, `BeTrueFalsePattern.cs:20-40`, ...)

- **Problem:** always the same sequence: `IsInsideAssertionScope` → Manual, cast to `InvocationExpressionSyntax`, `GetSubject` → Manual "not in the form x.Should()…", `IsResultDiscarded` → Manual, `TryGetArguments` → Manual "becauseArgs or named arguments". Only the texts differ, and not quite consistently (see M5).
- **Fix:** an abstract base `FluentAssertionPattern : IPattern` (Template Method, GoF) with a sealed-free `Rewrite` that performs the checks and calls `RewriteAssertion(AssertionContext ctx)`. The context contains `Assertion`, `Subject`, `Values`, `Because`, `Model` and `Name`. Patterns with different nodes (statement instead of invocation: AllSatisfy, OnlyContain, BeGreaterLess, ContainSingle/BeOfType with `.Which`) override a hook `GetAssertion(node)`. This removes about 300 lines.

### M2. Four versions of "safe to evaluate twice"

`AndChainPattern.cs:156-167` (`IsFreeOfSideEffects`, symbol-based), `AllSatisfyPattern.cs:105-111`, `BeGreaterLessPattern.cs:150-156`, `OnlyContainPattern.cs:78-84` (3× identical `IsSimple`, syntax only)

- **Problem:** copied code, and with slightly different semantics too: AndChain for example rejects indexers or method groups via symbols, `IsSimple` does not look at symbols. The same subject can become Fixed in one pattern and Manual in another.
- **Fix:** one `FluentAssertionsSyntax.IsSafeToEvaluateTwice(ExpressionSyntax, SemanticModel)` based on AndChain's symbol version. Put the `shortcut: property getters are assumed pure` comment there once.

### M3. Cross-pattern helpers ad hoc `internal` on concrete patterns

`HaveCountPattern.cs:91` (`IsSafeReceiver`, used by BeApproximately, HaveLength), `ShouldBePattern.cs:82` (`ConvertExpected`, by NotBe), `ThrowPattern.cs:20`, `NotThrowPattern.cs:19`, `ThrowAsyncPattern.cs:21` (`internal static Find(..., owns)`), `InvokingPattern.cs:25/41/51`, `ThrowExactlyPattern.cs:21`

- **Problem:** patterns depend on each other via statics on other patterns. `HaveCountPattern.IsSafeReceiver` has nothing to do with count. Throw, ThrowExactly, Invoking and NotThrow together form a web of mutual `owns` filters and `IsInvoking` checks.
- **Fix:** move `IsSafeReceiver` and `ConvertExpected` to `FluentAssertionsSyntax`. Bundle `IsExactly`, `IsInvoking` and `TryGetAction` in a `ThrowSyntax` helper (next to `ThrowMessage`, or merge them into it), so patterns no longer call other patterns.

### M4. Chain splitting exists twice; the BeGreaterLess chain path is dead in practice and produces duplicate findings

`BeGreaterLessPattern.cs:35-48, 57-104` versus `AndChainPattern.cs`

- **Problem:** `AndChainPattern` runs first and also splits `BeGreaterThan(..).And.BeLessThan(..)`. BeGreaterLess's own `.And` path is only reached when AndChain gave Manual, and then BeGreaterLess reports *another* Manual on the same statement. More generally: on a Manual chain each link pattern also reports its own link as Manual ("not in the form … after .And" or "result is used"). One chain thus yields 2-4 report lines.
- **Fix:** remove the chain logic from BeGreaterLess (Find then becomes `FindInvocations(...)`, `links` is always 1). Let link patterns skip invocations in `Find` whose `GetSubject` is null and that are in an `.And` chain. AndChain owns those, just as Throw already uses `owns`.

### M5. Null policy for the subject differs per pattern

`ContainSinglePattern.cs:58` (always `NotNull`), `HaveCountPattern.cs:58` / `HaveLengthPattern.cs:53` (only with a safe receiver, otherwise parentheses without `NotNull`), `ShouldBePattern.cs:74`, `OnlyContainPattern.cs:65`, `EqualSequencePattern` (never), `BeApproximatelyPattern.cs:67` (Manual)

- **Problem:** the same situation (a nullable subject in a nullable-enabled context) becomes Fixed with `ShouldNotBeNull()`, Fixed without it (risking CS8602/CS8604, which breaks the build in user code with `TreatWarningsAsErrors`) or Manual. With HaveCount a complex subject yields the form `(expr).Count.ShouldBe(n)` without a null check, while a simple subject does get one.
- **Fix:** choose one rule in `FluentAssertionsSyntax.NotNull` and apply it to every Shouldly receiver that is not nullable (`NotNull` can add parentheses itself for unsafe receivers). Record in a comment per pattern where it deliberately deviates, as NotBe already does.

### M6. AndChain: every link runs all 23 `Find`s over the whole file on a new compilation

`AndChainPattern.cs:96, 123-128`

- **Problem:** per chain there is a `ReplaceSyntaxTree` (a compilation fork) and a new semantic model. Then `RewriteLink` does `pattern.Find(root, model)` per link over the **whole** split root for each link pattern, and each `FindInvocations` binds symbols on the fresh model. That costs O(chains × links × patterns × invocations in file), and for test files with many chains it adds up quickly.
- **Fix:** pass `linkStatement` as root to `pattern.Find(linkStatement, model)`. The patterns only use `DescendantNodes` on root and the model, so that works. Adjust the `found == statement` check accordingly (the statement itself is not in `DescendantNodes`, so `DescendantNodesAndSelf` is needed for AllSatisfy and OnlyContain). Optional: split all chains of a file in one tree and one fork.

### M7. The whole solution is fully bound three times for using analysis

`UsingDirectives.cs:156-162` (`UsedNamespaces`), called from `Fixer.cs:87` (each file), `Fixer.cs:179` (each document of a global-using project) and `ProjectPackages.cs:62-66` (each document again)

- **Problem:** `GetSymbolInfo` on *every* `SimpleNameSyntax` of every document, three times, plus `GetSemanticModelAsync` for documents that no pattern touched. This is the dominant cost on large solutions.
- **Fix:** one pass per document that collects `(namespaces, assemblies)` of bound symbols (in a `Dictionary<DocumentId, ...>` inside `FixAsync`) and reuses them. Skip files without any `FluentAssertions` token with a quick text or syntax check before binding.

### M8. Three different ways to split a statement

`OnlyContainPattern.cs:16-19` (block check in `Find`), `AllSatisfyPattern.cs:19, 74` (statement in `Find`, block check in `Rewrite`), `AndChainPattern.cs:32, 46` and `BeGreaterLessPattern.cs:96` (block check in `Rewrite`)

- **Problem:** inconsistent. For `if (c) x.Should().OnlyContain(..);` OnlyContain reports a different node (the invocation) and a different reason than AllSatisfy in the same situation.
- **Fix:** a shared helper `FluentAssertionsSyntax.TryGetSplittableStatement(InvocationExpressionSyntax, out ExpressionStatementSyntax)` (Parent is `BlockSyntax` or `SwitchSectionSyntax`), called from `Rewrite`, with one shared reason text.

## Low

### L1. The because argument is added in 5 different ways

`BeEmptyPattern.cs:49`, `BeNullPattern.cs:49`, `BeOfTypePattern.cs:76`, `BeGreaterLessPattern.cs:135`, `ContainSinglePattern.cs:59`, `NotThrowPattern.cs:80`, `EqualSequencePattern.cs:72`, `OnlyContainPattern.cs:63`, with `ThrowMessage.Arguments` as the clean variant

- **Fix:** move `ThrowMessage.Arguments(first, because)` to `FluentAssertionsSyntax.Arguments` and use it everywhere. That also fixes the varying space trivia (sometimes `WithLeadingTrivia(Space)`, sometimes not).

### L2. The "becauseArgs" reason is wrong when the number of values differs

`BeGreaterLessPattern.cs:124-127`, `ThrowPattern.cs:88-91, 110-113`, `NotThrowPattern.cs:75-78`

- **Problem:** `!TryGetArguments(...) || values.Count != n` gives "becauseArgs or named arguments" in both cases, even with an unexpected number of arguments. That is misleading in the report.
- **Fix:** split the check into two separate checks with their own reason (BeOfType already does this right, `:63-70`).

### L3. AndChain gives a misleading reason for a link with multiple statements

`AndChainPattern.cs:108-116`

- **Problem:** a link that returns Fixed with more than one node (OnlyContain, AllSatisfy) falls into `default` and then becomes "which no pattern rewrites". That is not true: there is a pattern, it just returns two statements.
- **Fix:** accept `FixedResult { NewNodes: var nodes } when nodes.All(n => n is StatementSyntax)` and add all of them. That fixes it properly instead of only adjusting the text.

### L4. `IComparable<T>` check with a redundant condition

`BeGreaterLessPattern.cs:113`

- `i.OriginalDefinition.SpecialType == SpecialType.None` adds nothing (`IComparable<T>` has no SpecialType). Rather compare with `model.Compilation.GetTypeByMetadataName("System.IComparable`1")`via`SymbolEqualityComparer`, as `ThrowMessage.MayThrowAggregate` does with AggregateException.

### L5. Namespace check on string instead of symbol

`FluentAssertionsSyntax.cs:22-24, 134-135`, `ShouldBePattern.cs:38`, `NotBePattern.cs:37`, `BeOfTypePattern.cs:58`

- `ContainingNamespace.ToDisplayString()` creates a string per call, and `FindInvocations` calls this for every invocation with a matching name in every pattern. That is cheap but it adds up: 25 patterns × all invocations. Optional: cache the FluentAssertions `INamespaceSymbol` per compilation. This is low, only relevant together with M7.

### L6. `HashSet` is rebuilt per project

`ProjectPackages.cs:23`. Move `removed` before the `foreach`. This is a nit.

### L7. ShouldBe on an `object` subject with a collection changes semantics (uncertain)

`ShouldBePattern.cs:71-76`

- FluentAssertions' `ObjectAssertions.Be` compares with `Equals` (reference for `List<T>`). Shouldly's `ShouldBe<T>` compares, as far as I know, enumerables element by element. A test that failed may pass after the rewrite. This is not verified: check it with a proof (`object o = new List<int>{1}; o.Should().Be(new List<int>{1});`). If it holds, return Manual when the runtime or static type implements `IEnumerable` and is not a string.

### L8. `shortcut:` comments: status

- `UsingDirectives.cs:7` (usings in namespace blocks are not touched): **fix now.** File-scoped namespaces with usings inside are common in test code. Then `using FluentAssertions;` stays and `using Shouldly;` is added at the top, which compiles, but `HasUnusedRemovedUsingAsync` does not see the old using and the package may stay referenced via `ProjectPackages`. Verify with an integration test on a file in `namespace X; using FluentAssertions;` style (usings inside a `namespace {}` block).
- `UsingDirectives.cs:53`, `:121`, `ProjectPackages.cs:22`: acceptable, leave as is.
- `OnlyContain/AllSatisfy/BeGreaterLess` "property getters assumed pure": merge via M2.
- `InvokingPattern.cs:92`: leave as is.

### L9. Code standards

No `sealed`, no primary constructors, models are classes with `required init`, the `PatternResult` records are the permitted exception, `CancellationToken` is present in all async APIs and Core references no other project. **No violations found.** One edge case: `IPattern.Find`/`Rewrite` have no `CancellationToken`. They are synchronous, so the rule formally does not apply, but `AndChainPattern` does expensive work (M6) without being able to cancel. Consider a token in `IPattern` if M6 is not sufficient.

## Top 5 to fix first

1. **H1**: BeGreaterLess byte/short to `ConvertExpected`. Right now it silently becomes Fixed while the build breaks.
2. **H3**: `First` to `FirstOrDefault` in `Fixer.cs:107`. One line, prevents the whole run from crashing.
3. **H2**: Pass the Warning through in AndChain, so the report does not lie.
4. **M4 + M8**: chains have one owner (AndChain), the chain logic leaves BeGreaterLess and link patterns skip `.And` links. That cleans up dead code and duplicate report lines.
5. **M1 + M2 + M3**: base class `FluentAssertionPattern` with shared helpers in `FluentAssertionsSyntax`. This makes M5 and L1 and L2 consistent almost for free. Do this before more patterns are added.

## Status (executed on 3e7f807, uncommitted)

| Finding | Status | Explanation |
| ------- | ------ | ----------- |
| H1 | Fixed | `BeGreaterLessPattern` passes each value through `FluentAssertionsSyntax.ConvertExpected`; null → Manual. Proof `BeGreaterLess/byte-short`. |
| H2 | Fixed | `AndChainPattern` collects `Warning` from the links. Unit test `AndChainPatternTests.Rewrite_LinkWithWarning_KeepsWarning`. |
| H3 | Fixed | `Fixer.cs` (only change, lines ~106-109): `First` → `FirstOrDefault`, line 0 if the disabled trivia is only in the updated root. |
| M1 | Fixed | `FluentAssertionPattern` (Template Method) + `AssertionContext`; 19 patterns inherit from it. Deliberately not: Throw, ThrowAsync, ThrowExactly, NotThrow, Invoking (different node shape: `WithMessage`/`await` as node, own order of checks); those share `ThrowSyntax`. |
| M2 | Fixed | One `FluentAssertionsSyntax.IsSafeToEvaluateTwice` (symbol-based, from AndChain), used by AndChain, AllSatisfy, OnlyContain. BeGreaterLess no longer needs it (M4). |
| M3 | Fixed | `IsSafeReceiver`, `ConvertExpected` → `FluentAssertionsSyntax`; `IsExactly`, `IsInvoking`, `TryGetAction`, the `Find(owns)` variants and the dispatch → `ThrowSyntax`. NotBe is a subclass of ShouldBe (only the Shouldly name differs). |
| M4 | Partial | Chain logic removed from BeGreaterLess (proofs `and-chain`, `chain-lambda` → `AndChain/greater-less`, `AndChain/lambda`; `chain-other`, `chain-no-block`, `chain-subject-call`, `chain-used` dropped, covered by `AndChain` proofs and `BeGreaterLess/value-used`). Deliberately not: letting link patterns skip `.And` links. ~25 proofs per pattern pin down that Manual, and a pattern without AndChain (proof runner, or a fix without AndChain) would then silently ignore the chain. Duplicate lines only occur when AndChain gives Manual. |
| M5 | Partial | `FluentAssertionsSyntax.NotNullReceiver`: HaveCount and HaveLength now also get `ShouldNotBeNull()` with a complex subject (same rule). Deliberately not for ShouldBe/NotBe (generic `T` accepts nullable; NotBe would fail on null), EqualSequence/OnlyContain/StringStartEnd: requires verification per Shouldly overload with proofs (nullable-enabled), worth its own task. |
| M6 | Fixed | `RewriteLink` does `Find` on the parent of the link statement instead of the whole file. Deliberately not: all chains in one fork (optional). |
| M7 | Partial | `HasUnusedRemovedUsingAsync` and `MayUseRemovedNamespaceAsync` only bind when syntactically needed (files without a using of a removed namespace skip binding). Deliberately not: cache across the three passes; the compilation differs per pass (after each file, after the global usings), so reuse could be semantically wrong. `ProjectPackages` not touched (other agent, TAAK-060/063). |
| M8 | Fixed | `FluentAssertionsSyntax.TryGetSplittableStatement` + `NotSplittableReason`, used by AllSatisfy, OnlyContain and AndChain. OnlyContain now always finds the statement, just like AllSatisfy. |
| L1 | Fixed | `FluentAssertionsSyntax.Arguments`/`Invoke` (without trivia, space after comma) in the affected patterns. `ThrowMessage.Arguments` stays for the Throw family (keeps the trivia of `because`). |
| L2 | Fixed | Base splits becauseArgs; a differing number of values has its own reason (BeGreaterLess, BeApproximately, BeEquivalentToString). Throw/NotThrow: deliberately not (Throw family not migrated). |
| L3 | Fixed | AndChain accepts a link that returns multiple statements. Unit test `Rewrite_LinkWithSeveralStatements_KeepsAll`. |
| L4 | Fixed | `IComparable<T>` via `Compilation.GetTypeByMetadataName` (`System.IComparable` with arity 1). |
| L5 | Deliberately not | Optional and only relevant together with the M7 cache. |
| L6 | Deliberately not | `ProjectPackages.cs` belongs to the other agent (avoid merge conflict); nit. |
| L7 | Deliberately not | Uncertain, requires runtime verification of Shouldly's `ShouldBe` on `object` with a list; its own task. |
| L8 | Fixed | `UsingDirectives`: usings inside a namespace declaration (block and after file-scoped) are swapped/removed and count for `HasUnusedRemovedUsingAsync`/`KeepsRemovedUsing`. Proofs `ShouldBe/edge-using-in-namespace`, `ShouldBe/edge-using-after-file-scoped-namespace`. Other shortcuts left alone. |
| L9 | No action | No violations. |
