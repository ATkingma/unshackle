# TAAK-287 — FluentAssertions residual Manual, round 2 (inventory first)

**Status**: step 2/4 done (implementation); step 3 (corpus after) next
**Branch**: `worktree-agent-a6a00e1539f53e94e` (worktree `.claude/worktrees/agent-a6a00e1539f53e94e`; not pushed)
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- FA migrates fully on 5/12 blind repos; the rest is partial: mihome (`Invoking` on non-trivial receivers, `.And` chains), catalogapi (`BeEquivalentTo` on a list, `ThrowAsync` on another delegate), buildenator 46, bearded 80, ogooreck 9, xrepo 9 Manual left.
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Scan these repos, group Manual by root cause, then probe FA 7.2.2 per cause and rewrite what is provably the same (same pass/fail); state principally-manual causes (e.g. equivalency options Shouldly lacks) in the reason. Start with the causes that unblock the most findings.


## Inventory (round 5 tool `db05908`, scan of the 6 repos; `<corpus>/corpus-blind/_results/t287/scan-base`)

Manual findings by root cause (rows without `project-file`):

| Cause (reason) | Count | Repos | Decision |
| -------------- | ----: | ----- | -------- |
| `ContainInOrder` has no Shouldly equivalent | 19 | buildenator | principally manual: no exact subsequence assertion in Shouldly |
| `Contain(IEnumerable)` several items at once | 11 | bearded 9, buildenator 2 | not done: per-item rewrite changes the failure shape only, but the receiver type rules are open |
| Throw on `Func<V>` / `Func<Task<V>>` (value-type result) | 8 | bearded 8, catalogapi 1 (ThrowAsync) | **done**: wrap `() => f()` (probed same pass/fail) |
| `Invoking` kept in a local (`var act = x.Invoking(...)`) | 8 | mihome | **done**: new `fa.invoking-local` |
| `Invoking` on `mock.Object` (property receiver) | 2 | mihome | principally manual: a property read may have side effects, not provable |
| `Throw<XunitException>` etc. (exception type checked) | 25 | bearded | principally manual: Shouldly throws `ShouldAssertException` |
| Result of an assertion used (`.And`/`.Which`/value) | ~15 | all | principally manual: Shouldly returns nothing |
| `BeEquivalentTo`/`NotBeEquivalentTo` order, runtime members, no `ShouldNotBeEquivalentTo` | ~17 | ogooreck, buildenator, catalogapi | principally manual: equivalency options Shouldly lacks |
| named arguments / non-constant `because` | 10 | bearded | open (small) |
| `.And` continuing on another subject | ~6 | xrepo, buildenator | open |

Probes for the two done causes (FA 7.2.2): `Func<int>.Should().Throw<E>()` fails exactly when the func does not throw; a call as subject runs once (probe in `<corpus>/probe287`).

## Steps

- [x] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [x] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [ ] 3. Corpus `after` on mihome, catalogapi, buildenator, bearded, ogooreck, xrepo: build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
