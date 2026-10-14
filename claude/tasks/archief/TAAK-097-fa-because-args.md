# TAAK-097 — becauseArgs centrally → `string.Format`

**Status**: ✅ Done
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md` (actions 5–10)

## Choices

In `FluentAssertionsSyntax.TryGetArguments` (so every pattern that uses the base class or that helper): becauseArgs become `string.Format(System.Globalization.CultureInfo.InvariantCulture, because, args)` as customMessage. Patterns that reused `assertion.ArgumentList` (ShouldBe, Contain, ContainKey, HaveCount, HaveLength) now build the list from `Values` + `Because`.
Stays Manual: named arguments, a `because` that is not a constant, or a format that does not fit (FA swallows that FormatException, `string.Format` would throw), and AllSatisfy with because (cannot go inside the loop).

## Evidence

Probe under nl-NL: FA formats `{0}` with 1.5 as "1.5" → InvariantCulture. Bad format `{1}` with one arg: FA passes/reports WARNING; hence trying `string.Format` on the constant up front.
Probe code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflection on `Shouldly.dll`).

## Proofs

29 because-args proofs Manual → Fixed; new ShouldBe/because-args-bad-format and because-args-variable (Manual); AndChain/link-manual renamed to link-because-args

## Result

`dotnet build`, `dotnet test` (621) and the SampleShop tests (38) green. Scan: `ANALYSE-fa-100.md` section "Na acties 5–10".

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
