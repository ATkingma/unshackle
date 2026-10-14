# TAAK-013 — Pattern fa.contain-key

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `dict.Should().ContainKey(k)` / `NotContainKey(k)` becomes `ShouldContainKey` / `ShouldNotContainKey`, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 11 in `claude/tasks/patroon-queue.md`. Example: TAAK-007 (`ContainPattern` leaves dictionaries Manual). Sample: `RepositoryTests.cs:17` (`IReadOnlyDictionary`).

## Analysis

### Current situation

FA 7.2.2 `GenericDictionaryAssertions`: `ContainKey(key, because, becauseArgs)` → `AndWhichConstraint` (`.WhoseValue`), `NotContainKey`, `ContainKeys(params)`, `ContainValue(s)`, `NotContainValue(s)`, `Contain(key, value)`.

### Problem

Shouldly 4.3.0 only has `ShouldContainKey`, `ShouldNotContainKey`, `ShouldContainKeyAndValue`, `ShouldNotContainValueForKey`: no `ShouldContainValue`, no plural, no result.

### New situation

`ContainKeyPattern` (ID `fa.contain-key`): standalone `ContainKey`/`NotContainKey` with one key (+ `because`) → Fixed; all other forms Manual.

### Improvement

The sample `repository.All.Should().ContainKey(id)` is migrated automatically, also for `IReadOnlyDictionary`.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | One pattern for `ContainKey` and `NotContainKey` | Identical logic |
| 2 | No type check on the subject; the compile check proves `Dictionary`, `IDictionary`, `IReadOnlyDictionary` | FA only offers these methods on dictionaries |
| 3 | `ContainKeys`/`NotContainKeys` Manual ("several keys") | Multiple statements = different number of statements; out of scope for an expression rewrite |
| 4 | `ContainValue(s)`/`NotContainValue(s)` Manual ("has no Shouldly equivalent") | Shouldly has no value assertion independent of a key |
| 5 | `Contain(key, value)` stays in `ContainPattern` (Manual) | Do not touch; `ShouldContainKeyAndValue` is its own pattern |
| 6 | `.WhoseValue`/`.And`/variable (result used) Manual | Shouldly returns nothing |
| 7 | `becauseArgs`/named args Manual; `because` → `customMessage` | Like other patterns |
| 8 | Manual reasons: "is used", "is not in the form", "becauseArgs", "several keys", "has no Shouldly equivalent" | `ProofTests` requires one reason per proof |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan
- [x] 4. `ContainKeyPattern` + registration
- [x] 5. Proofs (`ContainKey/`)
- [x] 6. Build/test green, verify, archive, row `klaar`

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-007-fa-contain.md`
