# TAAK-006 — Pattern fa.be-empty

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `x.Should().BeEmpty()` / `NotBeEmpty()` becomes `ShouldBeEmpty()` / `ShouldNotBeEmpty()`, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 4 in `claude/tasks/patroon-queue.md`. Example: TAAK-004 (same shape).
- Sample occurrences: `OrderTests.cs:82` (`Lines.Should().NotBeEmpty()`), `:92` (`Lines.Should().BeEmpty()`), `:93` (`CustomerName.Should().BeEmpty()`, string).

## Analysis

### Current situation

FA 7.2.2: `BeEmpty(because, becauseArgs)` / `NotBeEmpty(...)` exist on `StringAssertions`, `GenericCollectionAssertions<T>` (also dictionaries, arrays) and return an `AndConstraint`.

### Problem

Shouldly has no `becauseArgs`; FA returns a constraint, `ShouldBeEmpty` returns nothing.

### New situation

`BeEmptyPattern` (ID `fa.be-empty`); standalone assertions are rewritten, otherwise `Manual(reason)`.

### Improvement

One pattern for strings, collections and dictionaries.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | One pattern for `BeEmpty` and `NotBeEmpty` | Identical logic apart from the name (like `BeNullPattern`) |
| 2 | No type switch: Shouldly has `ShouldBeEmpty`/`ShouldNotBeEmpty` for `string` and `IEnumerable<T>`, dictionaries fall under the latter | Proven by the compile check on the `after` proofs (string, List, Dictionary, IEnumerable, array) |
| 3 | `because` without args → `customMessage`; with `becauseArgs`/named → Manual | Same rule as other patterns |
| 4 | Only rewrite when the result is ignored and the shape is `x.Should().…` | `.And` requires splitting chains; out of scope |
| 5 | `BeNullOrEmpty`/`NotBeNullOrEmpty` out of scope | Different semantics (null allowed); its own pattern, Shouldly `ShouldBeNullOrEmpty` |
| 6 | Non-generic `IEnumerable` not proven | Rare; possibly Manual later |
| 7 | Manual proofs: `chain`, `value-used` → "is used"; `and-chain` → "is not in the form"; `because-args` → "becauseArgs" | `ProofTests` requires one reason per proof |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan (9 REQ)
- [x] 4. `BeEmptyPattern` + registration
- [x] 5. Proofs (11 in `BeEmpty/`)
- [x] 6. Build/test green (57 + 15 sample), verify, archive, row `klaar`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/`
- `docs/projectinfo/patterns/fa-be-empty-{plan,verify}.md`, `docs/projectinfo/README.md` (row)

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-004-fa-be-null.md`
