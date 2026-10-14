# TAAK-003 — Pattern fa.be-true-false

**Status**: ✅ Done
**Created**: 2026-10-10
**Goal**: `x.Should().BeTrue()` / `BeFalse()` on `bool` and `bool?` is rewritten to Shouldly, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 2 in `claude/tasks/patroon-queue.md`. Waits on row 0 (TAAK-001, still `bezig` at creation).
- Already existing at creation (half done): `IPattern`/`IFix`/`IFixer`, `FluentAssertionsFix`, `FluentAssertionsSyntax` (`FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`) and `ShouldBePattern`. Follow that example; when resuming, check the actual shape and do not change the contracts.
- Assumption (from the requester): the foundation adds `using Shouldly;` and removes `using FluentAssertions;` via `IFix.AddedNamespaces`/`RemovedNamespaces` and `PatternResult.Fixed(node, namespaces)`. This pattern does not need to manage usings itself.
- Sample occurrence: `samples/SampleShop/SampleShop.Tests/OrderTests.cs:62` `(order.Status == OrderStatus.Cancelled).Should().BeTrue();` (`bool`). No `BeFalse` or `bool?` in the samples.

## Analysis

### Current situation

FA 7.2.2: `BooleanAssertions<TAssertions>` has `BeTrue(string because = "", params object[] becauseArgs)` and `BeFalse(...)`. `Should()` on `bool` returns `BooleanAssertions`, on `bool?` `NullableBooleanAssertions` (inherits from `BooleanAssertions`, so the same `BeTrue`/`BeFalse`). The result is `AndConstraint<..>`: `.And` chains are possible. On `bool?`, `BeTrue` also fails on `null`. `NotBeTrue`/`NotBeFalse` exist only on `NullableBooleanAssertions` (out of scope).

### Shouldly 4.3.0 (checked via reflection on `Shouldly.dll`)

`ShouldBeBooleanExtensions.ShouldBeTrue(bool actual, string customMessage)` and `ShouldBeFalse(bool actual, string customMessage)`. There is **no** overload for `bool?`: `nullable.ShouldBeTrue()` does not compile (no implicit `bool?` → `bool`). For `bool?` the equivalent is `x.ShouldBe(true[, msg])` (`ShouldBe<T>(T actual, T expected, string? customMessage)` with `T = bool?`); it also fails on `null`, so equal to FA. `customMessage` is a plain string, without `{0}` formatting.

### Problem

- Naively mapping `BeTrue` → `ShouldBeTrue` breaks `bool?` (does not compile).
- FA's `because` supports format args, Shouldly does not.
- FA returns an `AndConstraint`, Shouldly returns nothing (void).

### New situation

`BeTrueFalsePattern` (ID `fa.be-true-false`) rewrites the safe cases and otherwise returns `Manual(reason)`.

### Improvement

In Shouldly, error messages name the expression ("`order.IsPaid` should be `True` but was `False`"); FA writes "Expected ... to be true". Consistent, and only truly ambiguous cases are left for manual work.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | One pattern `BeTrueFalsePattern`, ID `fa.be-true-false`; `Find` returns both `BeTrue` and `BeFalse` (two `FindInvocations` calls merged) | The queue row is one pattern; the logic is identical apart from the name |
| 2 | Only rewrite if the FA method `BeTrue`/`BeFalse` comes from `FluentAssertions.Primitives` (symbol check via `FindInvocations`) | Prevents false hits on a custom `BeTrue` |
| 3 | Subject of type `bool` → `x.ShouldBeTrue([msg])` / `x.ShouldBeFalse([msg])` | Direct 1:1 |
| 4 | Subject of type `bool?` (`Nullable<bool>`) → `x.ShouldBe(true[, msg])` / `x.ShouldBe(false[, msg])` | Shouldly has no `bool?` overload (see above); `ShouldBe(true)` also fails on `null`, equal to FA |
| 5 | Subject type unknown/error type, or not `bool`/`bool?` → `Manual("type onbekend")` | Prevents silent errors with missing references |
| 6 | `because` without `becauseArgs` → unchanged as `customMessage` | Direct 1:1 via `TryGetArguments`; FA's "because" prefix is dropped, Shouldly prepends "Additional Info:": documented difference |
| 7 | `because` with `becauseArgs`, or named arguments → `Manual("because met format-args")` | Shouldly does not format; dropping it or building `string.Format` is guessing |
| 8 | Only rewrite if the result is discarded (`IsResultDiscarded`); `.And`/`.Which` continuation, value used, or not in the form `x.Should().BeTrue()` (`GetSubject` null) → `Manual` | ShouldBeTrue returns nothing; splitting chains is row 10. Same rule as `ShouldBePattern` |
| 9 | Take over the subject expression unchanged (like `ShouldBePattern`): `(a == b).Should().BeTrue()` → `(a == b).ShouldBeTrue()` | The parentheses are already in the source because `.Should()` requires them; no extra precedence work |
| 10 | `NotBeTrue`/`NotBeFalse` (only `bool?`) and `Be(true)` out of scope | Different semantics (`null` passes) and a different pattern (`fa.should-be`), respectively |
| 11 | No own `using` management | Foundation assumption: `Shouldly` added, `FluentAssertions` removed via the Fix |

## Approach

### What / Where

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs` (`Id => "fa.be-true-false"`, `Description => "x.Should().BeTrue() → x.ShouldBeTrue()"`).
- One line `new BeTrueFalsePattern(),` in `Patterns` of `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`.
- Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/`.

### How

1. `Find`: `FindInvocations(root, model, "BeTrue").Concat(FindInvocations(root, model, "BeFalse"))`.
2. `Rewrite`: Manual checks (choices 8, 7, 5), then the type switch: `bool` → `Should{True|False}`, `bool?` → `ShouldBe` with a `true`/`false` literal as the first argument and any `because` as the second.
3. Build `InvocationExpression(MemberAccess(subject.WithoutTrailingTrivia(), name), args)` like `ShouldBePattern`.

## Proofs (to be made)

Folder `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/`; `before` compiles against FA, `after` against Shouldly.

| File | Contents |
|---|---|
| `be-true.before.cs` / `.after.cs` | `bool`: `flag.Should().BeTrue()` → `flag.ShouldBeTrue()` |
| `be-false.before.cs` / `.after.cs` | `bool`: `BeFalse()` → `ShouldBeFalse()`; also `(a == b).Should().BeFalse()` |
| `because.before.cs` / `.after.cs` | `BeTrue("must be paid")` → `ShouldBeTrue("must be paid")` (and BeFalse) |
| `nullable.before.cs` / `.after.cs` | `bool?`: `BeTrue()` → `ShouldBe(true)`, `BeFalse("why")` → `ShouldBe(false, "why")` |
| `because-args.before.cs` / `.manual.txt` | `BeTrue("x {0}", y)` → Manual (choice 7) |
| `chain.before.cs` / `.manual.txt` | `flag.Should().BeTrue().And.Be(true)` and `.BeTrue()` as a value (`var c = ...`) → Manual (choice 8) |
| `unknown-type.before.cs` / `.manual.txt` | if an error type can be made without `before` failing the compile check: otherwise skip and cover with a unit test |

## Steps

- [x] 1. Claim (row 2) and create task
- [x] 2. Research FA 7.2.2 / Shouldly 4.3.0 / sample occurrences, choices recorded
- [x] 3. Row 0 checked: foundation done, contract (`Fixed`/`Manual`, usings via `IFix`) matches the assumption
- [x] 4. `BeTrueFalsePattern` + registration in `FluentAssertionsFix`
- [x] 5. Proofs (see table)
- [x] 6. `dotnet build` and `dotnet test` green; archive task, row `klaar`

## Risks & edge cases

| Risk | Mitigation |
|---|---|
| `bool?` with `ShouldBeTrue` does not compile | Choice 4; proof `nullable` with compile check |
| Contract for usings differs from the assumption | Check when resuming; do not change the contract |
| Error message has a different shape (Additional Info) | Documented difference, no Manual |

## Adjustments during implementation

- `unknown-type` proof dropped: `before` would not compile; type check guarded by code only.
- Extra proof `value-used` (`var c = flag.Should().BeFalse()`); `chain` uses `.And.NotBe(false)` so that only BeTrue produces a finding (ProofTests requires the reason on all findings).
- For `bool?`, `because` gets a leading space (`ShouldBe(false, "why")`), otherwise `SeparatedList` glues the arguments together.
- The contract matched the assumption; no changes to contracts.

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/` (7 proofs)
- `docs/projectinfo/patterns/fa-be-true-false-{plan,verify}.md`, `docs/projectinfo/README.md` (row)

## See also

- `claude/tasks/patroon-queue.md`
- `claude/docs/patroon-loop.md`
- `docs/projectinfo/architectuur/testen.md`
- `samples/SampleShop/SampleShop.Tests/OrderTests.cs`
