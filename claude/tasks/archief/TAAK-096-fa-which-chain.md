# TAAK-096 — `.Which`/`.Subject` after value-returning assertions

**Status**: ✅ Done
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md` (actions 5–10)

## Choices

No new pattern: `FluentAssertionsSyntax.WithWhich` now accepts `.Which`/`.Subject` everywhere (not only `var y = ….Which;`), so `fa.be-of-type` (BeOfType/BeAssignableTo) and `fa.contain-single-which` replace the whole `….Which` with the Shouldly call that returns the same value (`ShouldBeOfType<T>()`, `ShouldHaveSingleItem()`, `Where(p).ShouldHaveSingleItem()`). `fa.throw` (also ThrowExactly/Invoking) does the same for `.Which` → `Should.Throw<T>(act)`; not `.Subject` there (those are all exceptions).
Stays Manual: `.And.Which`/`.And` (AndConstraint has no value in Shouldly), `.WithMessage(..).Which`, `ThrowAsync(..).Which` (await form), `ContainKey(..).WhoseValue` (`dict[k]` throws KeyNotFoundException instead of an assertion).

## Evidence

Probe: `BeOfType<D>().Which.A` = `ShouldBeOfType<D>().A` (pass/fail equal), also `BeAssignableTo`, `ContainSingle().Which/.Subject`, `ContainSingle(p).Which`, `Throw<T>().Which.Message`. Reflection: `ShouldBeOfType<T>`/`ShouldBeAssignableTo<T>`/`ShouldHaveSingleItem<T>` return `T`, `Should.Throw<T>` returns `TException`.
Probe code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflection on `Shouldly.dll`).

## Proofs

BeOfType/which-chain, which-multiple, ContainSingle/which-chain, Throw/edge-which-message, Throw/value-used, ThrowExactly/value-used (Manual → Fixed)

## Result

`dotnet build`, `dotnet test` (621) and the SampleShop tests (38) green. Scan: `ANALYSE-fa-100.md` section "Na acties 5–10".

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
