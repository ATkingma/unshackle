# TAAK-031 — Do not silently rewrite FA assertions inside an `AssertionScope`

**Status**: ✅ completed (2026-10-11)
**Created**: 2026-10-11
**Goal**: an assertion inside `using (new AssertionScope())` is never silently `Fixed` again; the scope semantics (collect all failures) are kept or reported as `Manual`.

## Context for the next agent

- Found in TAAK-030: `samples/SampleShop/SampleShop.Advanced.Tests/AdvancedAssertionTests.cs` lines 83-88.
- Scan 2026-10-11: line 85 (`fa.should-be`), 86 (`fa.have-count`), 87 (`fa.be-greater-less`, `BePositive`) become `Fixed`; the `using (new AssertionScope())` itself is not seen.
- Shared helpers are in `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` (`GetSubject`, `IsResultDiscarded`, `TryGetArguments`). Do not change contracts (`Abstractions/`, `Models/`, `Fixer`).

## Analysis

### Current situation

Every pattern only looks at its own invocation. An `AssertionScope` around it (also `using var scope = new AssertionScope();` or `new AssertionScope("context")`) is ignored.

### Problem

- After the fix the test stops at the first failing `ShouldBe`; FA reported all failures at once. Behaviour changes silently, which breaks the promise "Fixed = equivalent".
- The `using FluentAssertions.Execution;` and the scope stay, so FA stays in the project without the user knowing why.

### New situation

Every FA assertion with an ancestor (statement block) under an `AssertionScope` using gets `Manual("Inside an AssertionScope: ...")`. A separate pattern `fa.assertion-scope` reports the scope itself as `Manual` with the advice `ShouldSatisfyAllConditions`.

### Improvement

No more silent behaviour change; the user sees in the report exactly which scopes need manual work.

## Approach

### What

1. Helper `FluentAssertionsSyntax.IsInsideAssertionScope(SyntaxNode, SemanticModel)`: walks the ancestors to a `UsingStatementSyntax` with `new AssertionScope(...)` and to a `LocalDeclarationStatementSyntax` with `using` (valid until the end of the block, so only statements after the declaration). Type via the semantic model (`FluentAssertions.Execution.AssertionScope`), not by name.
2. All patterns in `Patterns/` call the helper as the first check → `Manual`.
3. New pattern `AssertionScopePattern` (`fa.assertion-scope`): finds `new AssertionScope(...)` and always returns `Manual` with the suggestion.

### Where

- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs`
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/*.cs` (all 14 patterns + `ThrowMessage.cs` if needed)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs` (new) + a line in `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AssertionScope/` + KingmaDoc `docs/projectinfo/patterns/fa-assertion-scope-{plan,verify,modellen}.md` + README row

### How

One shared check instead of per-pattern logic (DRY, one place for the `using var` edge cases). Proofs: `using (...)` block, `using var`, scope with a context string, assertion before `using var` (must stay Fixed).

### Why this approach

Option B, rewriting automatically to `x.ShouldSatisfyAllConditions(() => ..., () => ...)`, is rejected for now: Shouldly has that API (`ShouldSatisfyAllConditions` in 4.3.0, confirmed in `Shouldly.dll`), but it expects one subject or separate lambdas, and scopes often also contain non-assertion statements, nested scopes and `scope.Discard()`/`AddReportable`. Manual is safe; a rewrite can come later as its own pattern for the simple case (a block with only assertion statements).

## Steps

- [x] 1. Helper + unit tests (`using` block, `using var`, nested scope, assertion outside the scope)
- [x] 2. Have all patterns ask; existing proofs stay green
- [x] 3. `AssertionScopePattern` + proofs + KingmaDoc
- [x] 4. Scan on SampleShop: lines 85-87 `Manual`; `dotnet build`, `dotnet test`, sample tests green

## Risks & edge cases

- `using var scope` applies until the end of the block: only statements after the declaration in the same block and deeper.
- Lambdas inside the scope (e.g. `act` that is executed later) fall syntactically inside the scope; Manual is then too cautious, but safe.
- Custom scope subclasses: check with `InheritsFrom`/`SymbolEqualityComparer` on `AssertionScope`.

## Choices (execution)

- Subclasses are moot: `AssertionScope` is `sealed` in FA 7.2.2 (CS0509); `IsAssertionScope` compares the exact type.
- "Assertion before `using var` stays Fixed" as a unit test, not as a proof: the after would still contain `AssertionScope` and would not compile against Shouldly alone.
- SampleShop integration test unchanged: the assertions are loose (`>= 20` Fixed) and stay green.

## See also

- `claude/tasks/archief/TAAK-030-sampleshop-uitbreiden.md` (finding)
- `claude/tasks/patroon-queue.md`
- `docs/projectinfo/architectuur/core.md`
