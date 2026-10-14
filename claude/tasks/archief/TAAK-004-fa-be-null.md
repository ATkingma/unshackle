# TAAK-004 — Pattern fa.be-null

**Status**: ✅ Done
**Created**: 2026-10-10
**Goal**: `x.Should().BeNull()` / `NotBeNull()` is rewritten to `ShouldBeNull()` / `ShouldNotBeNull()`, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 3 in `claude/tasks/patroon-queue.md`. Foundation and examples: TAAK-001..003.
- Sample occurrences: `RepositoryTests.cs:18,26` (`Find(id).Should().NotBeNull()` / `BeNull()` on a reference type) and `OrderHandlerTests.cs:25` (`dto.Should().NotBeNull(); dto!.LineCount...`).

## Analysis

### Current situation

FA 7.2.2: `BeNull(string because = "", params object[] becauseArgs)` and `NotBeNull(...)` exist on `ReferenceTypeAssertions` (object, string, collections) and on the nullable assertion classes (`NullableNumericAssertions<T>`, `NullableBooleanAssertions`, ...). They return an `AndConstraint`; `NotBeNull` returns an `AndWhichConstraint` (`.Which`) on some types.

### Problem

- Shouldly has no `because` format args.
- FA returns a constraint (`.And`/`.Which`); `ShouldBeNull` returns nothing, `ShouldNotBeNull` returns the value itself.

### New situation

`BeNullPattern` (ID `fa.be-null`) rewrites standalone assertions; otherwise `Manual(reason)`.

### Improvement

One pattern for all subject types; no type switch needed.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | One pattern, `Find` on `BeNull` and `NotBeNull` | Identical logic apart from the name (like `BeTrueFalsePattern`) |
| 2 | No type switch: reference types and `Nullable<T>` → the same `ShouldBeNull`/`ShouldNotBeNull` | Shouldly 4.3.0 has `ShouldBeNull<T>(this T? actual, string? customMessage)`; proven by the compile check with `int?`/`bool?` (unlike `bool?` with `ShouldBeTrue`) |
| 3 | `because` without args → `customMessage`; with `becauseArgs` or named args → Manual | Shouldly does not format; same rule as other patterns |
| 4 | Only rewrite if the result is discarded and the form is `x.Should().…` | `.And`/`.Which`/value require splitting chains or `var v = x.ShouldNotBeNull()`; deliberately out of scope |
| 5 | `x!` in code after `NotBeNull()` is left alone | Harmless; Shouldly's `[NotNull]` merely makes it redundant |
| 6 | Manual proofs each have one reason: `chain` (`.NotBeNull().And.NotBeEmpty()`) and `value-used` → "is used"; `and-chain` (`.NotBeEmpty().And.NotBeNull()`) → "is not in the form"; `because-args` → "becauseArgs" | `ProofTests` requires the same reason for all findings in one proof (TAAK-003) |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan
- [x] 4. `BeNullPattern` + registration
- [x] 5. Proofs (10 proofs in `BeNull/`)
- [x] 6. Build/test green, verify, archive, row `klaar`

## Adjustments during implementation

- Proof `which` dropped: `NotBeNull().Which` does not exist on string/object assertions; replaced by `value-used`.
- Proofs lack implicit usings: `lambda` and `collection` have explicit `using System;` / `System.Collections.Generic`.

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/`
- `docs/projectinfo/patterns/fa-be-null-{plan,verify}.md`, `docs/projectinfo/README.md` (row)

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-003-fa-be-true-false.md`
