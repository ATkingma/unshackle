# TAAK-275 — FluentAssertions: custom assertion classes → Shouldly

**Status**: step 1/5 — not started (waits for TAAK-271)
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
- [ ] 2. Probe: construct inventory + FA behaviour
- [ ] 3. Rewrite + proofs (incl. `_Combined` with call sites and project file) + unit tests
- [ ] 4. Corpus: FA repos with custom assertions, `after` 0 regressions; SampleShop Manual count before/after
- [ ] 5. Verify, models, commit

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-fa.md`
