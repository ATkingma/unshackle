# Pattern queue (FluentAssertions → Shouldly)

Work list for the pattern loop (`claude/docs/patroon-loop.md`). One row per pattern, top first.
An agent claims the **first** row with status `open` (or `blocked` whose blocker is done) by changing the status
immediately. Usage examples: `samples/SampleShop/SampleShop.Tests/`.

Statuses: `open` · `in progress (TAAK-NNN)` · `blocked (TAAK-NNN, waiting for TAAK-MMM)` · `done (TAAK-NNN)` · `failed (TAAK-NNN, attempt N)`

| #   | Pattern ID                   | FluentAssertions                                  | Shouldly (expected)                                                                      | Status           |
| --- | ---------------------------- | ------------------------------------------------- | ---------------------------------------------------------------------------------------- | ---------------- |
| 0   | _foundation_                 | contracts, Fixer, proof runner + `fa.should-be`   | —                                                                                        | done (TAAK-001)  |
| 1   | `fa.have-count`              | `x.Should().HaveCount(n)`                         | List → `x.Count.ShouldBe(n)`, IEnumerable → `x.Count().ShouldBe(n)`, dictionary → Manual | done (TAAK-002)  |
| 2   | `fa.be-true-false`           | `x.Should().BeTrue()` / `BeFalse()`               | `x.ShouldBeTrue()` / `ShouldBeFalse()`                                                   | done (TAAK-003)  |
| 3   | `fa.be-null`                 | `x.Should().BeNull()` / `NotBeNull()`             | `x.ShouldBeNull()` / `ShouldNotBeNull()`                                                 | done (TAAK-004)  |
| 4   | `fa.be-empty`                | `x.Should().BeEmpty()` / `NotBeEmpty()`           | `x.ShouldBeEmpty()` / `ShouldNotBeEmpty()`                                               | done (TAAK-006)  |
| 5   | `fa.contain`                 | `x.Should().Contain(item)` / `Contain(predicate)` | `x.ShouldContain(item)` / `ShouldContain(predicate)`                                     | done (TAAK-007)  |
| 6   | `fa.throw`                   | `act.Should().Throw<T>()` (+ `.WithMessage`)      | `Should.Throw<T>(act)` (+ message check)                                                 | done (TAAK-008)  |
| 7   | `fa.throw-async`             | `await act.Should().ThrowAsync<T>()`              | `await Should.ThrowAsync<T>(act)`                                                        | done (TAAK-012)  |
| 8   | `fa.not-throw`               | `act.Should().NotThrow()`                         | `Should.NotThrow(act)`                                                                   | done (TAAK-011)  |
| 9   | `fa.be-equivalent-to-string` | `s.Should().BeEquivalentTo("x")`                  | `s.ShouldBe("x", StringCompareShould.IgnoreCase)`                                        | done (TAAK-009)  |
| 10  | `fa.be-greater-less`         | `BeGreaterThan` / `BeLessThan` (+ `.And` chains)  | `ShouldBeGreaterThan` / `ShouldBeLessThan`, split chain                                  | done (TAAK-010)  |
| 11  | `fa.contain-key`             | `dict.Should().ContainKey(k)`                     | `dict.ShouldContainKey(k)`                                                               | done (TAAK-013)  |
| 12  | `fa.only-contain`            | `x.Should().OnlyContain(pred)`                    | `x.ShouldAllBe(pred)`                                                                    | done (TAAK-015)  |
| 13  | `fa.all-satisfy`             | `x.Should().AllSatisfy(a => ...)`                 | `x.ShouldAllBe(...)` or Manual                                                           | done (TAAK-014)  |
| 14  | `fa.be-equivalent-to-object` | `obj.Should().BeEquivalentTo(other)`              | `obj.ShouldBeEquivalentTo(other)` or Manual (semantics!)                                 | done (TAAK-016)  |

### Round 2 — from SampleShop.Advanced.Tests (TAAK-030)

By priority (how often in real test code; updated 2026-10-11 from `docs/projectinfo/architectuur/corpus.md`, rows without a number come from that corpus). TAAK number on claiming, from 032 (031 = AssertionScope bug: done, pattern `fa.assertion-scope`, TAAK-031).
All Shouldly targets checked in `Shouldly.dll` 4.3.0. Prio: H = in almost every codebase, M = regularly, L = rarely.

| #   | Pattern ID                 | FluentAssertions (before)                                                                           | Shouldly (expected)                                                                                                                                                                              | Prio | Status           |
| --- | -------------------------- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---- | ---------------- |
| 15  | `fa.not-be`                | `status.Should().NotBe(Shipped, "reason")`                                                          | `status.ShouldNotBe(Shipped, "reason")`                                                                                                                                                          | H    | done (TAAK-032)  |
| 16  | `fa.and-chain`             | `x.Should().NotBeEmpty().And.HaveCount(2)` (r39, r47, r159)                                         | split into separate statements on the same subject (like `fa.be-greater-less`); subject with side effects → Manual                                                                               | H    | done (TAAK-033)  |
| 17  | `fa.be-of-type`            | `dto.Should().BeOfType<OrderDto>()` / `BeAssignableTo<T>()`                                         | `dto.ShouldBeOfType<OrderDto>()` / `ShouldBeAssignableTo<T>()` (return the cast value)                                                                                                           | H    | done (TAAK-034)  |
| 18  | `fa.contain-single-which`  | `x.Should().ContainSingle(pred).Which.P.Should().Be(5m)`                                            | `x.Where(pred).ShouldHaveSingleItem().P.ShouldBe(5m)`; without pred `x.ShouldHaveSingleItem()`                                                                                                   | H    | done (TAAK-036)  |
| 19  | `fa.which-chain`           | `.BeOfType<T>().Which…`, `ContainKey(k).WhoseValue…`, `ThrowAsync<T>().Which…`                      | use the value-returning Shouldly call inline (`ShouldBeOfType<T>()`, `dict[k]`, `(await Should.ThrowAsync<T>(act))`); rest Manual                                                               | H    | done (TAAK-096)  |
| 20  | `fa.string-start-end`      | `s.Should().StartWith("Ti")` / `EndWith` / `NotBeNullOrWhiteSpace()` / `NotBeNullOrEmpty()`         | `s.ShouldStartWith("Ti")` / `ShouldEndWith` / `ShouldNotBeNullOrWhiteSpace()` / `ShouldNotBeNullOrEmpty()` (note: Shouldly is case-insensitive by default for StartWith → pass `Case.Sensitive`) | H    | done (TAAK-035)  |
| 21 | `fa.equal-sequence` | `products.Should().Equal(Products())` | `products.ShouldBe(Products())` (order-sensitive, like FA `Equal`) | M | done (TAAK-038) |
| 22 | `fa.because-args` | `Be(25m, "two of {0}", 10m)` / `HaveCount(2, because: "…", 2)` | `ShouldBe(25m, string.Format("two of {0}", 10m))`; reorder named args. Shared helper in `TryGetArguments` | M | done (TAAK-097) |
| 23 | `fa.throw-exactly` | `await act.Should().ThrowExactlyAsync<T>()` / `ThrowExactly<T>()` | `(await Should.ThrowAsync<Exception>(act)).ShouldBeOfType<T>()` | M | done (TAAK-051) |
| 24 | `fa.be-approximately` | `d.Should().BeApproximately(25m, 0.01m)` | `d.ShouldBe(25m, 0.01m)` (double/float/decimal overload with tolerance) | M | done (TAAK-050) |
| 25 | `fa.be-close-to` | `dt.Should().BeCloseTo(other, TimeSpan.FromSeconds(2))` | `dt.ShouldBe(other, TimeSpan.FromSeconds(2))` (DateTime/DateTimeOffset/TimeSpan) | M | done (TAAK-050) |
| 26 | `fa.have-length` | `s.Should().HaveLength(5)` | `s.Length.ShouldBe(5)` (null subject: `ShouldNotBeNull()` before it, like TAAK-021) | M | done (TAAK-039) |
| 27 | `fa.numeric-sign` | `x.Should().BeNegative()` (`BePositive` already covered by `fa.be-greater-less`) | `x.ShouldBeNegative()`; also `BeInRange(a,b)` → `ShouldBeInRange(a,b)` | M | done (TAAK-166) |
| 28 | `fa.invoking` | `order.Invoking(o => o.Cancel()).Should().Throw<T>()` | `Should.Throw<T>(() => order.Cancel())`; then `fa.throw` rules (chains Manual) | M | done (TAAK-052) |
| 29 | `fa.not-contain-nulls` | `x.Should().NotContainNulls()` / `AllBe("EUR")` | `x.ShouldNotContain(i => i == null)` / `x.ShouldAllBe(i => i == "EUR")` | L | done (TAAK-161) |
| 30 | `fa.order` | `x.Should().BeInAscendingOrder(p => p.Sku)` / `BeInDescendingOrder` | `x.Select(p => p.Sku).ShouldBeInOrder(SortDirection.Ascending)`; with comparer → Manual | L | done (TAAK-162) |
| 31 | `fa.match` | `s.Should().MatchRegex("^SO-[0-9]+$")` / `Match("SO-*")` | `s.ShouldMatch("^SO-[0-9]+$")`; wildcard `Match` → regex (`^` + `Regex.Escape` with `*`→`.*`, `?`→`.` + `$`) or Manual | L | done (TAAK-099) |
| 32 | `fa.equivalent-options` | `BeEquivalentTo(x, o => o.Excluding(d => d.Id))` / `WithStrictOrdering` / `ExcludingMissingMembers` | Manual (Shouldly `ShouldBeEquivalentTo` has no options); only a better Manual reason per option | L | done (TAAK-163) |
| 33 | `fa.contain-equivalent-of` | `x.Should().ContainEquivalentOf(item)` | Manual (no Shouldly equivalent; records → `ShouldContain(item)` is possible, but only with value equality) | L | done (TAAK-164) |
| 34 | `fa.custom-assertions` | `order.Should().BeCancelled(..)` on own `ReferenceTypeAssertions` subclass | Manual with reason "custom FA assertion: rewrite the extension to Shouldly"; do not skip silently | L | done (TAAK-165)   |
| — | `fa.be-same-as` | `x.Should().BeSameAs(y)` / `NotBeSameAs(y)` (482 + 54×, 9 projects) | `x.ShouldBeSameAs(y)` / `x.ShouldNotBeSameAs(y)` | H | done (TAAK-093) |
| — | `fa.not-throw-async` | `await act.Should().NotThrowAsync()` (150×, 4 projects) | `await Should.NotThrowAsync(act)`; uses result/`.Which` → Manual | M | done (TAAK-094) |
| — | `fa.contain-in-order` | `x.Should().ContainInOrder(a, b)` (67×, 4 projects) | no Shouldly equivalent: Manual with reason, or subsequence check with `ShouldBe` if `x` is exactly `a, b` | M | done (TAAK-101: Manual with code) |
| — | `fa.not-equal` | `x.Should().NotEqual(other)` (11×, 3 projects) | `x.ShouldNotBe(other)` (order-sensitive, just like `Equal` → #21) | L | done (TAAK-160) |
| — | `fa.be-one-of` | `x.Should().BeOneOf(a, b)` (9×, 3 projects) | `x.ShouldBeOneOf(a, b)` | L | done (TAAK-101, `fa.direct-mapping`) |
| — | `fa.have-count-comparison` | `x.Should().HaveCountGreaterThan(n)` / `…OrEqualTo` / `HaveCountLessThan` (3 projects) | `x.Count().ShouldBeGreaterThan(n)` (like `fa.have-count`: List → `.Count`) | L | done (TAAK-095) |

## See also

- `docs/projectinfo/architectuur/corpus.md` — frequency per FA API in 14 open-source projects (source of the priorities)
- `claude/docs/patroon-loop.md` — how the loop and the agents work
- `docs/projectinfo/project.md` section "Examples of rewrites"


## Bugs from corpus run (2026-10-11) — first, before new patterns

| Task | Bug | Found in | Status |
| --- | --- | --- | --- |
| TAAK-070 | Assertion in lambda with `Throw<XunitException>` on it → regression | FluentResults | done (assertion + Throw Manual) |
| TAAK-110 | Unrecognized `.Should()` → `using FluentAssertions` removed anyway | CSharpFunctionalExtensions | done (using + Manual; CSFE builds) |
| TAAK-111 | Project file not modifiable → code rewritten anyway | Polly, Ardalis.Result | done (TAAK-111/134) |
| TAAK-112 | Bogus does not load | Bogus | done (Builder omitted with warning; Bogus builds) |
| TAAK-113 | `Equal([..])` collection expression | PowerApps-Tooling | done (proof; corpus not re-proven) |
| TAAK-114 | `Guid` `NotBeEmpty()` → `ShouldNotBeEmpty()` (CS1061) | Polly | done (Polly builds) |
| TAAK-115 | Added usings outside namespace / redundant (IDE0065/IDE0005) | evo1 (MediatR) | done |

## Round 3 — corpus 2026-10-12

From `claude/tasks/CORPUSRUN-fa.md` (round 3). Build errors first.

| Task | Bug / pattern | Found in | Count | Status |
| --- | --- | --- | ---: | --- |
| TAAK-174 | `using Shouldly;` clashes with own `ShouldBe` extensions (CS0121) | Akka.NET | 4 984 errors | done (TAAK-174, TAAK-198, TAAK-199) |
| TAAK-169 | `Be(x)` with own implicit conversion (CS1929/CS0411) | LiteDB | 11 lines | done (TAAK-169; corpus not re-run) |
| TAAK-168 | `ShouldContainKey` on `IReadOnlyDictionary`/headers (CS0411) | Steeltoe | 11 lines | done (TAAK-168) |
| TAAK-167 | Non-UTF-8 file crashes the fix | EventFlow | 1 file | done (TAAK-167) |
| TAAK-181 | Self-closing `<Project Sdk=".." />` (existing, now also with FA) | modular-monolith | 82 Manual | done (TAAK-181, MediatR queue) |
| TAAK-170 | `Throw<T>()` result used → returned exception | Polly et al. | 499 Manual | done (TAAK-170; Polly 964 → 171 Manual together with TAAK-173) |
| TAAK-171 | `ContainKey(..).WhoseValue`, `ContainKeys`/`ContainValues` | Steeltoe, Polly | 377 Manual | done (TAAK-171; corpus not re-run) |
| TAAK-172 | `BeEquivalentTo` collection with value items → `ignoreOrder: true` | 13 projects | 207 Manual | done (TAAK-172) |
| TAAK-173 | `Awaiting(..).Should().NotThrowAsync()` | Polly | 131 Manual | done (TAAK-173) |

## Round 4 — corpus 2026-10-13

From `claude/tasks/CORPUSRUN-fa.md` (round 4). Test regressions first.

| Task | Bug / pattern | Found in | Count | Status |
| --- | --- | --- | ---: | --- |
| TAAK-220 | `Throw<T>()` on delegate with `Wait()`/`WaitAsync` → `AggregateException` not unwrapped | Akka.NET (Streams) | 9 regressions | ✅ done |
| TAAK-221 | `ThrowAsync`/`ThrowExactlyAsync` on cancelled task → Shouldly creates its own `TaskCanceledException` | Steeltoe | 18 regressions | ✅ done |
| TAAK-222 | `Throw<XunitException>()` on own assertion helper in another file | LiteDB | 26 regressions | ✅ done |
| TAAK-223 | Multi-target: first TFM migrated, `#if !NETFRAMEWORK` code stays | LiteDB | 709 Manual | ✅ done |
| TAAK-224 | `5.Seconds()`/`Milliseconds()` → `TimeSpan.From*` | Akka, Steeltoe | 380 Manual | ✅ done |
| TAAK-225 | `Equal(..)` on dictionary → `ShouldBe(.., ignoreOrder: true)` | Akka | 344 Manual | ✅ done |
