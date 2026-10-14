# TAAK-166 — Pattern fa.numeric-sign

**Status**: ✅ completed
**Created**: 2026-10-12
**Goal**: `BeNegative()`/`BePositive()` on all numeric types; `BeNegative` on double/float (Manual since TAAK-101) automatic when an exact translation exists.

## Context for the next agent

- Row 27 in `claude/tasks/patroon-queue.md`. `BePositive`, `BeNegative` and `BeInRange` were already in `BeGreaterLessPattern` (`fa.be-greater-less`); no separate pattern ID was added. Plan, verify and models of `fa-be-greater-less` are updated (REQ-16).
- Reason for the Manual from TAAK-101: NaN (see below).

## Probe (FA 7.2.2 vs Shouldly 4.3.0; scratchpad `probe166/`)

| Value | FA BeNegative | ShouldBeNegative | ShouldBeLessThan(0) | `x < 0` | BePositive FA = Shouldly |
|---|---|---|---|---|---|
| NaN (double and float) | fails | **passes** | **passes** | false | yes (both fail) |
| -0.0 / 0.0 | fails | fails | fails | false | yes |
| ±Epsilon, ±Infinity, ±1 | equal | equal | equal | equal | yes |

int, long, short and decimal (-1, 0, 1): all equal. `byte` has no Shouldly overload (stays Manual).

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | `BeNegative` on double/float → `(x < 0).ShouldBeTrue([because])` | `ShouldBeNegative` and `ShouldBeLessThan(0)` pass on NaN (CompareTo orders NaN below everything); `x < 0` is false for NaN and -0.0 and equal for the rest: pass/fail exactly equal |
| 2 | Subject in parentheses if it is not a name/member/call/index | `(a - b) < 0` and precedence |
| 3 | Literal `0` (int) | Converts implicitly; the comparison stays in the subject type |
| 4 | `BePositive` stays `ShouldBePositive()` | Identical in both libraries for all probe values |
| 5 | Drawback documented | The error message of `ShouldBeTrue` does not contain the value |
| 6 | `BeInRange` unchanged | Already covered (proof `in-range`) |

## Proofs

`BeGreaterLess/negative-double` (was `.manual.txt`, now before+after), new `negative-float` (float with because, compound subject), existing `sign`, `sign-byte`.

## Result

`dotnet build` 0 warnings; ProofTests green (653). Note: `kingmadoc` was missing on this machine, so the PNG/SVG of figure 3 were not re-rendered; only the `.d2` and the table in the models document were updated.

## See also

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs`
- `docs/projectinfo/patterns/fa-be-greater-less-plan.md`
- `claude/tasks/archief/TAAK-101-fa-direct-mappings.md`
