# TAAK-277 — FluentAssertions: residual Manual patterns (proposed, from TAAK-271)

**Status**: step 0/4 — proposed, not started (not in the original queue; order after TAAK-275)
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

## Steps

- [ ] 1. Inventory: FA Manual root causes in the blind FA repos
- [ ] 2. Probes per candidate; decide Fixed vs principally manual
- [ ] 3. Implementation + proofs + unit tests
- [ ] 4. Corpus `after` on the FA repos: 0 regressions; commit

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/archief/TAAK-271-analyse-manual-blockers.md`, `claude/tasks/TAAK-275-fa-custom-assertion-classes.md`
