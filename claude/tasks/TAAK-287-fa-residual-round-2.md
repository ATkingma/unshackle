# TAAK-287 — FluentAssertions residual Manual, round 2 (inventory first)

**Status**: done on the branch (not pushed): VERIFY OK at 08ab032, 1589 tests
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

## Corpus after (tool = this branch, `<corpus>/corpus-blind/_results/t287/tool-new`, harness `after`)

| Repo        | Fixed / Manual before | Fixed / Manual after | Tests before → after | Regressions |
| ----------- | --------------------- | -------------------- | -------------------- | ----------: |
| mihome      | 319 / 15              | 328 / 6              | 448/448 → 448/448    | 0 |
| catalogapi  | 222 / 4               | 223 / 2              | 44/54 → 44/54        | 0 |
| ogooreck    | 23 / 9                | 23 / 9               | 20/20 → 20/20        | 0 |
| xrepo       | 122 / 9               | 122 / 9              | 71/71 → 71/71        | 0 |
| buildenator | 623 / 46              | 623 / 46             | 127/127 → 127/127 ¹  | 0 |
| bearded     | 375 / 80              | 383 / 72             | 1294/1294 → 1294/1294 | 0 |

¹ Display names of generated-data tests differ per run (same pass count, not regressions), as in round 5. All six build after `fix`.

Remaining Manual are the principally manual causes in the inventory table (exception type checks, used results, equivalency options, `ContainInOrder`, property receivers) plus the open small ones.

## Steps

- [x] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [x] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [x] 3. Corpus `after` on mihome, catalogapi, buildenator, bearded, ogooreck, xrepo: build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [x] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
