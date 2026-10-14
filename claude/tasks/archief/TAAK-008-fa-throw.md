# TAAK-008 — Pattern fa.throw

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `act.Should().Throw<T>()` (+ `.WithMessage`) becomes `Should.Throw<T>(act)` (+ message check), with proofs, green in `dotnet test`.

## Context for the next agent

- Row 6 in `claude/tasks/patroon-queue.md`. Sample: `OrderTests.cs:73`. Example: TAAK-007.

## Analysis

### Current situation

FA 7.2.2: `Throw<T>(because, becauseArgs)` on `ActionAssertions`/`FunctionAssertions<T>` (not on `Func<Task>`), `ThrowExactly<T>`; result `ExceptionAssertions<T>` with `WithMessage(wildcard, because, becauseArgs)` (case-insensitive, whole message), `WithInnerException`, `Where`, `.Which`/`.And`.

### Problem

The subject becomes an argument (the shape changes) and `WithMessage` has no direct Shouldly counterpart (`ShouldMatch` is regex).

### New situation

`ThrowPattern` (`fa.throw`): Find returns the `.WithMessage(...)` call if it directly follows `Throw`, otherwise the `Throw` call. One statement stays one statement.

### Improvement

The most common pattern in the sample is converted without manual work.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | Shouldly `Should.Throw<T>` accepts derived types (established with a test program); `Throw<T>` → Fixed, `ThrowExactly<T>` → Manual | Same behaviour for `Throw<T>`; `ThrowExactly` has no equivalent |
| 2 | `Throw<T>().WithMessage(p)` → `Should.Throw<T>(act).Message.ShouldX(...)` in one statement | No split needed; Shouldly's `Throw` returns the exception |
| 3 | Pattern only as a string constant; `*x*` → `ShouldContain`, `x*` → `ShouldStartWith`, `*x` → `ShouldEndWith` (all `Shouldly.Case.Insensitive`), `x` → `ShouldBe(x, StringCompareShould.IgnoreCase)`; anything else (`?`, `*` in the middle, only `*`) → Manual | FA's `WithMessage` is a case-insensitive wildcard over the whole message; Shouldly's `ShouldMatch` is regex and would silently behave differently |
| 4 | `because` of `Throw` → 2nd argument of `Should.Throw`; of `WithMessage` → `customMessage` of the check (for `ShouldBe` before `StringCompareShould`: overload `(string, string, StringCompareShould)`) | Checked against the Shouldly 4.3.0 overloads |
| 5 | Subject: `Action`, or `Func<T>` with T a reference type; `Func<ValueType>` and `Invoking(...)` → Manual | `Should.Throw` only has `Action`/`Func<object?>`; `Func<Task>` has no `Throw` in FA |
| 6 | Result used/chain (`.Which`, `.WithInnerException`, `.Where`) → Manual | No unambiguous form; `.Which` may come later |
| 7 | Manual reasons: "is used", "Invoking", "reference type", "wildcards other than", "not a string constant", "becauseArgs", "requires the exact exception type" | `ProofTests` requires one reason per proof |
| 8 | No proof for `.And.Throw` (does not compile with FA 7.2.2) | `ProofTests` requires before to compile |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan (13 REQ)
- [x] 4. `ThrowPattern` + registration
- [x] 5. Proofs (19 cases in `Throw/`)
- [x] 6. Build/test green (98 + 15 sample), verify, archive, row `klaar`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/`
- `docs/projectinfo/patterns/fa-throw-{plan,verify}.md`, `docs/projectinfo/README.md` (row)

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-007-fa-contain.md`
