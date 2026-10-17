# TAAK-275 — FluentAssertions: custom assertion classes → Shouldly

**Status**: step 4/5 — in progress (agent)
**Branch**: worktree-agent-a8433c56bf106c4f6 (worktree `.claude/worktrees/agent-a8433c56bf106c4f6`)
**Created**: 2026-10-17
**Goal**: user-written FA assertion classes (`ReferenceTypeAssertions<T, TSelf>` subclasses with `Execute.Assertion.ForCondition(...).FailWith(...)`, plus their `Should()` extension) are rewritten to Shouldly-style extension methods with the same pass/fail behaviour, so their call sites and the project file can migrate too.

## Context for the next agent

- SampleShop: `samples/SampleShop/SampleShop.Advanced.Tests/OrderAssertions.cs` causes 10 of the 19 Manual findings (8 unmatched uses + 2 project files that keep FA). Today the report says "rewrite this FluentAssertions extension to Shouldly once" (`fa.custom-assertion`).
- Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Approach

1. Probe: list the FA 7.2.2 constructs used in custom assertions in the corpus (TAAK-271 table + FA corpus `claude/tasks/CORPUSRUN-fa.md`): `Execute.Assertion`, `BecauseOf`, `ForCondition`, `FailWith` with `{context}`/`{reason}` placeholders, `Given`/`Then`, `AndConstraint<T>`.
2. Rewrite target: `public static void ShouldBeCancelled(this Order order, string? customMessage = null)` that throws `ShouldAssertException` with an equivalent message; call sites `x.Should().BeCancelled()` → `x.ShouldBeCancelled()`; `.And` chains split as `fa.and-chain`.
3. Only constructs whose pass/fail behaviour is provably the same are Fixed (the message text may differ, as with all Shouldly rewrites); anything else (custom `Subject` state, `Given` pipelines with side effects) stays Manual with a precise reason.

## Scope from TAAK-271 (measured)

- Correction to the context: the 19 SampleShop FA Manual findings split into **11** caused by `OrderAssertions` (7 `unmatched-use` on `ReferenceTypeAssertions`, `AndConstraint`, `Execute`, `AssertionScope.BecauseOf/ForCondition/FailWith`; 1 `fa.custom-assertion`; 1 `.And` chain containing `BeCancelled`; 2 project-file/usings leftovers) and **8** unrelated ones. Only the 11 belong here; the 8 go to TAAK-277.
- The blind corpus has no custom assertion class among its Manual causes of the 8 blocked repos (those are MediatR/AutoMapper); FA repos in `CORPUSRUN-blind.md` were not part of TAAK-271. Look for custom assertions in the FA corpus (`CORPUSRUN-fa.md`) in step 2 of this task before investing.

## Steps

- [ ] 1. KingmaDoc plan (pattern `fa.custom-assertion`) after reading TAAK-271
- [x] 2. Probe: construct inventory + FA behaviour (probe program outside the repo; results below)
- [x] 3. Rewrite + proofs (incl. `_Combined` with call sites and project file) + unit tests
- [ ] 4. Corpus: FA repos with custom assertions, `after` 0 regressions; SampleShop Manual count before/after
- [ ] 5. Verify, models, commit

## Progress (WIP)

Probe results (FA 7.2.2): ForCondition(true) passes, false makes FailWith throw; FailWith args are evaluated even on pass (a throwing arg throws); `because` shows only via `{reason}`; first failure throws outside an AssertionScope (inside it collects); `{context}` default is "object"; unknown placeholders are dropped, `{{` is a literal brace; Given/Then and `.Then` chains are lazy; null Subject throws NRE in both.

Design: `CustomAssertionPattern` becomes ISolutionFinder/ISolutionPattern placed before `AndChainPattern`; per class one cached `CustomAssertionPlan` (shape analysis + solution-wide usage scan via SymbolFinder) so class, `Should()` extension and calls all convert or none do. Chains with `.And` are split per link into statements (custom link -> `x.ShouldFoo(...)`, FA link stays `x.Should().Bar()` for the other patterns).

Done (step 3): pattern is ISolutionFinder/ISolutionPattern, first in FluentAssertionsFix.Patterns (plans are computed on the unchanged baseline; cache per Solution snapshot via ConditionalWeakTable). Class → static extension class, holder of `Should()` removed (or only the method), calls rewritten, `.And` chains split (FA links stay for the later patterns). Extra plan check: a method name in inactive `#if` code blocks the class. `FluentAssertions.Primitives` added to RemovedNamespaces. Proofs: CustomAssertion/reference-type (after), given-then + class-in-assertion-scope (manual), _Combined/custom-assertion-class. All 1517 tests green.
Next (old list): `CustomAssertionPlans` (cache + usage scan), pattern changes, `FluentAssertionsFix` (order, add `FluentAssertions.Primitives` to RemovedNamespaces), build, proofs, unit tests, SampleShop before/after, corpus (`/mnt/dikkeschijf/unshackle-corpus`, FA repos with custom assertions: fresults, powerapps, steeltoe, bearded, evo), KingmaDoc plan/models/verify, archive.

## Changed files

- src/Unshackle.Core/Fixes/FluentAssertions/CustomAssertion{Shape,Analyzer,Chain,Plan,Plans}.cs (new)
- src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs, FluentAssertionsFix.cs
- src/Unshackle.Tests/Proofs/FluentAssertions/CustomAssertion/{reference-type,given-then,class-in-assertion-scope}.*, _Combined/custom-assertion-class.*
- src/Unshackle.Tests/CombinedProofTests.cs (pattern order), docs/patterns.md

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-fa.md`
