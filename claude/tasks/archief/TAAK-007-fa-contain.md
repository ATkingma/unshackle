# TAAK-007 — Pattern fa.contain

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `x.Should().Contain(item|predicate)` / `NotContain` becomes `ShouldContain` / `ShouldNotContain`, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 5 in `claude/tasks/patroon-queue.md`. Example: TAAK-006. Sample: `OrderTests.cs:81`, `OrderHandlerTests.cs:56-57`.

## Analysis

### Current situation

FA 7.2.2: `Contain(T item | Expression<Func<T,bool>> | IEnumerable<T>, because, becauseArgs)` on `GenericCollectionAssertions`, `Contain(string, because, becauseArgs)` (+ occurrence overload) on `StringAssertions`, `Contain(KeyValuePair/params)` and `Contain(key, value)` on dictionaries. All return an `AndConstraint`.

### Problem

Shouldly has no `becauseArgs`. More importantly: `string.ShouldContain(x)` is **case-insensitive** by default, while FA is case-sensitive (discovered with a test program).

### New situation

`ContainPattern` (ID `fa.contain`) rewrites standalone assertions; strings get `Shouldly.Case.Sensitive`; the rest `Manual(reason)`.

### Improvement

One pattern for item, predicate and substring, `Contain` and `NotContain`, without a silent change in semantics.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | `NotContain` in the same pattern | Identical logic apart from the name |
| 2 | Determine the overload via `method.OriginalDefinition.Parameters[0].Type` (type parameter / string / Expression = Fixed; `IEnumerable<T>` = Manual) | Distinguishes item from all-items even when T itself is an IEnumerable |
| 3 | String: `ShouldContain(x, Shouldly.Case.Sensitive[, because])` | Shouldly's default is case-insensitive; also applies to `ShouldNotContain`. The overload is `(string, Case, customMessage)`, so `because` comes after `Case` |
| 4 | Dictionary always Manual | Contain(KVP/params/key,value) has no unambiguous Shouldly form; `ContainKey` is row 11 |
| 5 | Occurrence constraint (2 value arguments) Manual | No equivalent |
| 6 | Non-generic `IEnumerable` subject Manual | Shouldly only has `IEnumerable<T>` |
| 7 | Manual reasons: `chain`/`value-used` → "is used"; `and-chain` → "is not in the form"; `because-args` → "becauseArgs"; `dictionary` → "Dictionary:"; `items` → "several items"; `occurrence` → "occurrence constraint" | `ProofTests` requires one reason per proof |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan (11 REQ)
- [x] 4. `ContainPattern` + registration
- [x] 5. Proofs (28 files in `Contain/`)
- [x] 6. Build/test green (71 + 15 sample), verify, archive, row `klaar`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/`
- `docs/projectinfo/patterns/fa-contain-{plan,verify}.md`, `docs/projectinfo/README.md` (row)

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-006-fa-be-empty.md`
