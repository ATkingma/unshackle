# TAAK-098 — Subject with side effects → `var subject = …;`

**Status**: ✅ Done
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md` (actions 5–10)

## Choices

New helper `FluentAssertionsSyntax.Hoist`: if `IsSafeToEvaluateTwice` refuses and the statement sits directly in a block, `var subject = <expr>;` is inserted before the statements (FA also evaluates the subject once, before the assertions). Used in `fa.and-chain`, `fa.only-contain`, `fa.all-satisfy`. The name is unique within the member; multiple hoists in one pass get subject, subject2, … (k, k+n, …).
Not done: the Invoking receiver (`fa.invoking`, its own ThrowSyntax path) stays Manual.

## Evidence

No semantic difference: one evaluation, same order (subject before arguments).
Probe code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflection on `Shouldly.dll`).

## Proofs

AndChain/subject-call, subject-indexer, new subject-call-twice; AllSatisfy/subject-call; OnlyContain/subject-call (Manual → Fixed)

## Result

`dotnet build`, `dotnet test` (621) and the SampleShop tests (38) green. Scan: `ANALYSE-fa-100.md` section "Na acties 5–10".

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
