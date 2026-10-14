# TAAK-002 — Pattern fa.have-count

**Status**: ✅ Done
**Created**: 2026-10-10
**Goal**: `x.Should().HaveCount(n)` is rewritten to Shouldly depending on type, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 1 in `claude/tasks/patroon-queue.md`. Waits on row 0 (TAAK-001): `IPattern`, `Fixer`, `FluentAssertionsFix`, `ProofTests` did not exist yet at creation (only empty folders in `src/Unshackle.Core/`).
- When resuming: first check the actual shape of `IPattern` (Find/Rewrite, how `using` additions can be made) and adjust **Approach** accordingly; do not change the contracts.
- Sample occurrences: `samples/SampleShop/SampleShop.Tests/OrderTests.cs:33` (List), `:42` (IEnumerable via `Where`), `OrderHandlerTests.cs:68` (`List<OrderDto>`), `RepositoryTests.cs:16` (dictionary → Manual).

## Analysis

### Current situation

FA 7.2.2: `GenericCollectionAssertions<TCollection,T,TAssertions>` has
`HaveCount(int expected, string because = "", params object[] becauseArgs)` and
`HaveCount(Expression<Func<int,bool>> countPredicate, string because = "", params object[] becauseArgs)`.
Related methods (other patterns, not here): `NotHaveCount`, `HaveCountGreaterThan(OrEqualTo)`, `HaveCountLessThan(OrEqualTo)`.
`Should()` exists for `IEnumerable<T>` (also dictionaries, arrays, lists), non-generic `IEnumerable`, and `string` (no HaveCount).
The result is `AndConstraint<..>`: chains with `.And`/`.Which` are possible.

### Shouldly 4.3.0

There is **no** `ShouldHaveCount`. The equivalent is asserting the counter itself: `x.Count.ShouldBe(n)` with
`ShouldBe<T>(this T actual, T expected, string? customMessage = null)` (also a `Func<string>` variant).
`customMessage` is a plain string: no `{0}` formatting with args like FA's `becauseArgs`.
Behavior: on failure Shouldly reports "x.Count should be 2 but was 3" (the counter, not the contents); FA shows the whole collection. Acceptable loss.

### Problem

The right counter depends on the type (`Count` property, `Length`, LINQ `Count()`), and not every case can be translated safely.

### New situation

Pattern `fa.have-count` (`HaveCountPattern`) rewrites the safe cases and returns `Manual(reason)` for the rest.

### Improvement

Consistent, and the user only has to handle truly ambiguous cases (dictionary, predicate) themselves.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | One pattern `HaveCountPattern` (ID `fa.have-count`) with an internal type switch | The queue row is one pattern; 3 outcomes + Manual stay small. Split (as ARCHITECTURE.md allows) only if it grows too large |
| 2 | Type determination via `SemanticModel` on the receiver of `.Should()`; error type → `Manual("type onbekend")` | Prevents silent errors with missing references |
| 3 | Dictionary (`IDictionary<,>`, `IReadOnlyDictionary<,>`, or a type implementing them, incl. `Dictionary<,>`) → `Manual("Dictionary: controleer of Count of Keys bedoeld is")` | Recorded in queue/ARCHITECTURE.md (report example). `Count` would technically be correct, but the intent is often "number of keys" |
| 4 | Array (1 dimension) → `x.Length.ShouldBe(n)` | Array has a public `Length`; `Count` only via explicit `ICollection` (does not compile) |
| 5 | Type with a public `int Count` property (`List<T>`, `HashSet<T>`, `ICollection<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>` via interface, `Collection<T>`) → `x.Count.ShouldBe(n)` | No enumeration, O(1), free of side effects. `IList<T>` inherits `Count` from `ICollection<T>`: include it in the search for the property (also via `AllInterfaces`) |
| 6 | `ImmutableArray<T>` (has `Length`, no `Count`) → `.Length` if the type has a public int `Length` and no `Count` | Same rule as array, stated generically: first `Count`, then `Length` |
| 7 | Other `IEnumerable<T>` (LINQ results, `IQueryable`, iterators) → `x.Count().ShouldBe(n)` and add `using System.Linq;` if missing (also if a global/implicit using already provides it: add nothing) | Spec from the queue. `Count()` enumerates, and so does FA; same behavior |
| 8 | Non-generic `IEnumerable` → `Manual("Non-generic IEnumerable: gebruik Cast<T>().Count()")` | No `Count()` without `Cast`; element type unknown |
| 9 | `HaveCount(predicate)` (Expression lambda) → `Manual("Predicate: herschrijf naar Count.ShouldBeGreaterThan/LessThan/...")` | Shouldly has no count predicate; converting the lambda requires semantics per operator. Possibly later separate patterns for `HaveCountGreaterThan` etc. |
| 10 | `because` without `becauseArgs` (string literal or string expression) → second argument of `ShouldBe` (`customMessage`) | Direct 1:1 |
| 11 | `because` with `becauseArgs` (`{0}` placeholders) → `Manual("because met format-args")` | Shouldly does not format; silently dropping it or building `string.Format` is guessing |
| 12 | Only rewrite if the `HaveCount` call is a standalone expression statement (`x.Should().HaveCount(2);`, also in an `await`-free context). If `.And`/`.Which` follows, or the result is used (lambda body, return, assignment) → `Manual("keten/waarde gebruikt")` | Shouldly asserts return (almost) nothing useful; splitting chains is the job of another pattern (row 10) |
| 13 | A receiver with lower precedence (`a ?? b`, `c ? a : b`, cast, await, binary operator) gets parentheses; conditional access (`a?.b.Should()`) → Manual | Preserves semantics for `.Count` access |
| 14 | `Should()` must be the FA extension (symbol in namespace `FluentAssertions`); otherwise skip (no Finding) | Prevents false hits on a custom `Should()` |
| 15 | Argument `n`: taken over unchanged (expression of type `int`) | No recalculation needed |
| 16 | `NotHaveCount`, `HaveCountGreaterThan` etc. out of scope | Other patterns; only exact `HaveCount` here |

## Approach

### What / Where

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs` (Id `fa.have-count`, Description `x.Should().HaveCount(n) → x.Count.ShouldBe(n)`).
- One line in the `Patterns` list of `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`.
- Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/`.

### How

1. `Find`: invocations named `HaveCount` whose symbol is in `FluentAssertions.Collections.*`.
2. `Rewrite`: Manual checks (choices 2, 3, 8, 9, 11, 12), then the type switch (4–7), build `Receiver.Count.ShouldBe(n[, because])`.
3. `using System.Linq` only for choice 7; the mechanism depends on the foundation contracts (open point below).

### Open point

Can a pattern add a `using` via the contracts? If not: falling back to `System.Linq.Enumerable.Count(x)` is ugly; then rather mark the step `[!]` and ask the user/foundation. Decide when resuming.

## Proofs (to be made)

Folder `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/`; `before` compiles against FA, `after` against Shouldly.

| File | Contents |
|---|---|
| `list.before.cs` / `list.after.cs` | `List<int>`: `x.Should().HaveCount(2)` → `x.Count.ShouldBe(2)` |
| `list-because.before.cs` / `.after.cs` | `HaveCount(2, "two items expected")` → `x.Count.ShouldBe(2, "two items expected")` |
| `collection-interfaces.before.cs` / `.after.cs` | `ICollection<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `HashSet<T>` → `.Count` |
| `array.before.cs` / `array.after.cs` | `int[]` → `.Length.ShouldBe(n)` |
| `enumerable.before.cs` / `enumerable.after.cs` | `IEnumerable<T>` / `list.Where(...)` → `.Count().ShouldBe(n)` + `using System.Linq;` (variant with and without an existing using) |
| `enumerable-because.before.cs` / `.after.cs` | same, with `because` |
| `receiver-parens.before.cs` / `.after.cs` | `(a ?? b).Should().HaveCount(1)` → `(a ?? b).Count.ShouldBe(1)` |
| `dictionary.before.cs` / `dictionary.manual.txt` | `Dictionary<,>` → Manual (reason from choice 3) |
| `predicate.before.cs` / `predicate.manual.txt` | `HaveCount(c => c > 1)` → Manual |
| `because-args.before.cs` / `because-args.manual.txt` | `HaveCount(2, "x {0}", y)` → Manual |
| `chain.before.cs` / `chain.manual.txt` | `x.Should().HaveCount(2).And.Contain(1)` → Manual |
| `non-generic.before.cs` / `non-generic.manual.txt` | non-generic `IEnumerable` → Manual |

## Steps

- [x] 1. Claim (row 1) and create task
- [x] 2. Research FA 7.2.2 / Shouldly 4.3.0 / sample occurrences, choices recorded
- [x] 3. Row 0 done; contract checked (`PatternResult.Fixed(node, namespaces)` adds usings)
- [x] 4. `HaveCountPattern` + registration in `FluentAssertionsFix`
- [x] 5. Proofs (see table)
- [x] 6. `dotnet build` and `dotnet test` green; archive task, row `klaar`

## Risks & edge cases

| Risk | Mitigation |
|---|---|
| Foundation contract cannot add a `using` | Open point; decide when resuming |
| `Count` on a custom type with a `Count` that is not an `int` | Only accept an `int` property, otherwise Manual |
| Lazy `IEnumerable` with side effects | FA and `Count()` both enumerate once: same behavior |
| Error message becomes shorter (counter only) | Documented difference, no Manual needed |

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (+1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/` (12 proofs)

## Adjustments during implementation (compared to research)

- Open point `using System.Linq` resolved: `PatternResult.Fixed(node, "System.Linq")`; the Fixer only adds it if it is missing (proofs `enumerable` and `enumerable-existing-using`).
- Choice 8 (non-generic `IEnumerable`) is implemented (Manual) but has **no proof**: FA 7.2.2 has no `HaveCount` on the `Should()` of non-generic `IEnumerable` (resolves to `ObjectAssertions`, does not compile).
- Predicate recognized via the first parameter of the resolved method (`int` or not), not via the argument shape. Conditional access (`a?.b`) is Manual (choice 13).
- The `enumerable` proof uses its own iterator method instead of `Where`, because `before` must compile without `using System.Linq`.
- Proofs: 12 (see the list in the table above; `enumerable-existing-using` added, `non-generic` dropped).

## See also

- `claude/tasks/patroon-queue.md`
- `claude/docs/patroon-loop.md`
- `ARCHITECTURE.md` section "Testen"
- `samples/SampleShop/SampleShop.Tests/OrderTests.cs`
