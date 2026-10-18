# TAAK-277 — FluentAssertions: residual Manual patterns (proposed, from TAAK-271)

**Status**: done — 2026-10-18 (branch `worktree-agent-a5a969704f036379e`, resumed after commit `2b29350`; not pushed)
**Created**: 2026-10-17
**Goal**: the FA Manual findings in SampleShop that are not custom assertion classes become Fixed wherever Shouldly can reproduce the pass/fail behaviour exactly; the rest gets a precise "principally manual" reason.

## Context for the next agent

- Source: TAAK-271 SampleShop FA scan, 8 findings in `samples/SampleShop/SampleShop.Advanced.Tests/AdvancedAssertionTests.cs` and `SampleShop.Tests/RepositoryTests.cs`. They are synthetic; the blind-corpus FA repos have their own Manual causes (skunkdeco 14, ogooreck 9, xrepo 9, buildenator 318, bearded 80) that TAAK-271 did not classify: extend this task with that inventory first (`scan` the FA corpus, group by root cause), because the real numbers decide the order.
- Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Candidates (each needs an FA 7.2.2 probe before choosing the rewrite)

| Finding                                                                                                                                              | Idea                                                                                                                       |
| ---------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `HaveCount(...)` with named arguments or non-constant `becauseArgs` (`:78`)                                                                          | map named arguments to the positional form; non-constant format args stay Manual                                           |
| `NotBeNull(...)` after `.And` (`:150`), `ContainKey(...)` result used via `.Which`/`.WhoseValue` (`:69`)                                             | split the chain into statements (`dict.ShouldContainKey(k); dict[k].ShouldBe(...)`)                                        |
| `Dictionary.HaveCount` (`RepositoryTests.cs:16`)                                                                                                     | FA counts pairs: `dict.Count.ShouldBe(n)`                                                                                  |
| `BeEquivalentTo` with `Excluding`, `ExcludingMissingMembers`, `WithStrictOrdering`, `ContainEquivalentOf` on a class (`:98`, `:101`, `:111`, `:113`) | member-wise comparison for types whose members are all leaves; any other type stays Manual (Shouldly compares by `Equals`) |

## Result

What lay in `2b29350` (earlier agent) and how it was checked in this session:

| Residual | Change | Proof |
| --- | --- | --- |
| `Dictionary.HaveCount` (`RepositoryTests.cs:16`) | `dict.Count.ShouldBe(n)` (was Manual "Count or Keys?") | probe `unshackle-corpus/probe277` against FA 7.2.2: Dictionary and IReadOnlyDictionary, HaveCount and HaveCountGreaterThan at n-1/n/n+1 all same pass/fail; null dictionary fails in both (NRE vs assertion). Proof `HaveCount/dictionary.*` |
| `_ = x.Should()...;` (buildenator) | new `fa.discard-assignment`, plain statement then rewritten | probe: discard has no runtime effect (pass and fail cases same); unit tests + proofs `DiscardAssignment/*`, `_Combined/discard-assignment.*` |
| `Type.Should().Be<T>()/Be(typeof(T))`, `NotBe` | `ShouldBe(typeof(T))`, `ShouldNotBe(typeof(T))` | probe: same, base, interface, open generic, null subject/expected, all same; proofs `ShouldBe/type.*`, `NotBe/type.*` |
| named constant `because:` (this session) | positional customMessage (message only, no pass/fail effect; only a constant is moved so evaluation order is kept); other named arguments stay Manual | proofs `BeGreaterLess/named-because`, `ThrowAsync/named-because`, `Contain/named-because`, `ShouldBe/edge-named-because` turned from manual into before/after; fixes `HaveCount(2, because: ..., 2)` (`:78`) |

Principally manual (SampleShop `AdvancedAssertionTests.cs`): `ContainKey(...).WhoseValue` (:69; Shouldly's `ShouldContainKey` returns void, probed, so a rewrite needs the dictionary and key evaluated twice), `BeEquivalentTo` with `ExcludingMissingMembers` (:98), `Excluding` (:101), `WithStrictOrdering` on a class (:111) and `ContainEquivalentOf` on a class (:113): Shouldly compares by `Equals` / all members, FA by members with options; no equivalent code exists.

SampleShop FA Manual (code findings): 8 before, 5 after in `scan` (:78 named because and :16 dictionary fixed; :150 was already fixed by TAAK-275), plus the project-file row of `SampleShop.Advanced.Tests`. `SampleShop.Tests` now migrates completely in one fix (tests `SampleShopTests` follow).

Blind corpus (FA repos, Manual before TAAK-277 baseline in `_results/277/scan-old` -> now): skunkdeco 14 -> 0, buildenator 318 -> 46, bearded 80 -> 80, ogooreck 9 -> 9, xrepo 9 -> 9, pathlib 0. Remaining causes (144): `ContainInOrder` 19 (no Shouldly equivalent, principled), `Contain(IEnumerable)` 11, `Throw<XunitException>` and similar exception-type checks about 25 (Shouldly throws ShouldAssertException; principled), `BeEquivalentTo` options/non-plain types about 25, `NotBeEquivalentTo` 6 (no equivalent), custom assertion classes with named tolerance arguments in bearded (BeApproximately, 12). Further work is candidate for a follow-up, not needed for this task.

Corpus `after` (tool published from this branch, `_results/after`): skunkdeco 26/26, ogooreck 20/20 (builds now), xrepo 71/71, bearded 1294/1294, buildenator 127/127 (1 flaky, not a regression), pathlib 177/184 both before and after: **0 regressions**.

Note: a stale `_results/scan` and `_results/after` of earlier rounds were replaced (`after-prev277` keeps the old after).

## Steps

- [x] 1. Inventory: FA Manual root causes in the blind FA repos (above)
- [x] 2. Probes per candidate; decide Fixed vs principally manual (above)
- [x] 3. Implementation + proofs + unit tests
- [x] 4. Corpus `after` on the FA repos: 0 regressions; commit

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/archief/TAAK-271-analyse-manual-blockers.md`, `claude/tasks/archief/TAAK-275-fa-custom-assertion-classes.md`
