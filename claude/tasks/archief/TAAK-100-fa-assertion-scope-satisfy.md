# TAAK-100 — AssertionScope → `ShouldSatisfyAllConditions`

**Status**: ✅ Done
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md` (actions 5–10)

## Choices

`AssertionScopePattern` now runs first. Safe form: `using (new AssertionScope()) { … }` or `using var s = new AssertionScope();` (one scope in the block, variable not used otherwise), without arguments, and every statement is an FA assertion statement without `await` and without ref/out variables. Then `x.ShouldSatisfyAllConditions(() => { a; }, () => { b; });` (receiver = first safe subject; Shouldly only prints it) and the assertion patterns rewrite the lambdas. Block bodies because Shouldly is not yet referenced during the fix (expression lambdas then have no delegate type). `FluentAssertions.Execution` is now in `RemovedNamespaces` (only removed if unused).
Stays Manual: context argument, discard/scope variable, other statements, await.

## Evidence

Probe: both collect all assertion failures (2 failures → 2 messages), including a non-assertion exception and a failing Throw; a null receiver passes.
Probe code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflection on `Shouldly.dll`).

## Proofs

_Combined/scope-using-block, scope-using-var (Fixed); AssertionScope/context, other-statement, discard, await (Manual)

## Result

`dotnet build`, `dotnet test` (621) and the SampleShop tests (38) green. Scan: `ANALYSE-fa-100.md` section "Na acties 5–10".

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
