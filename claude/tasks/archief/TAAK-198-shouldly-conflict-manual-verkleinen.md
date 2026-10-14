# TAAK-198 — Shrink the conflict Manual for own `Should*` extensions (Akka: 3,449 findings)

**Status**: ✅ completed (2026-10-13)
**Created**: 2026-10-13 (TAAK-174 step 3, Akka repeat)
**Goal**: still migrate most of the files with their own `ShouldBe`/`ShouldBeTrue` in scope (Akka.TestKit), without CS0121 and without fully qualified Shouldly calls.

## Context for the next agent

- After TAAK-174 Akka.NET (`23a8566`) builds green, but the price is high: **3,449 of 5,126 Manuals** are "Not rewritten: Shouldly.… is ambiguous with Akka.TestKit.AkkaSpecExtensions.… once Shouldly is imported (CS0121)". Fixed dropped from 8,488 (broken build) to 5,463.
- Distribution of that reason: `ShouldBe` 2,018, `ShouldBeTrue` 1,114, `ShouldBeFalse` 172, `ShouldBeGreater…` 104, rest small.
- The conflict check marks the **whole file** Manual, also the calls whose Shouldly name does not clash (e.g. `Throw` → `Should.Throw`, `Contain` → `ShouldContain`).
- Fully qualified calls (`Shouldly.ShouldBeTestExtensions.ShouldBe(x, y)`) were deliberately rejected in TAAK-174.

## Analysis

### Possible approaches

1. **Targeted `using static`**: instead of `using Shouldly;` only import the Shouldly classes that contain no clashing names (`using static Shouldly.ShouldThrowExtensions;` etc.; `Should` as a type via `using Should = Shouldly.Should;`). Calls that end up on a clashing name stay Manual; the rest becomes Fixed. Note: `using static` brings in all extensions of that class, so check per class.
2. **Reuse the own extension**: if the own method does semantically the same (Akka `ShouldBe<T>(T, T, string)` = equality, `ShouldBeTrue(bool, string)`), rewrite `x.Should().Be(y)` to the own `x.ShouldBe(y)` without a Shouldly import. Requires a whitelist or a semantic check (signature + name); risk of different behaviour (messages, `IEquatable`).
3. Per file: only Manual if at least one rewritten call needs a clashing name (now: as soon as a clashing extension is in scope?) — first check what the current check does exactly in `UsingDirectives.cs`/Fixer.

### Other error codes from the first Akka run

The 8× CS8130, 8× CS0119, 6× CS1061 and 6× CS0452 from the first run disappeared after TAAK-174 (build 0 errors). The saved `build1.log` of that run no longer contains them per line, so the exact places cannot be traced; they were almost certainly in files that are now Manual (follow-up errors of the same ambiguity, e.g. inference with deconstruction and method groups). If approach 1 or 2 migrates those files again, watch for these codes again.

## Steps

- [x] 1. Read the current conflict check (`Fixer.cs`, `UsingDirectives.AmbiguityWithAddedAsync`: after the rewrite, on the rewritten document; one ambiguous call → the whole file rolled back) and determine where to intervene per call.
- [x] 2. Unit tests: file with an own `ShouldBe` + FA calls that do/do not clash.
- [x] 3. Implement approach 1 (targeted imports); approach 2 only after consulting the user.
- [x] 4. Proof: the Akka repeat builds green, Fixed rises, 0 test regressions; watch for CS8130/CS0119/CS1061/CS0452.

## Result

- Chosen: approach 1 (targeted imports), data-driven. Only when the existing check (TAAK-174/199) finds a conflict does `Fixer.FixWithTargetedImportsAsync` run: no `using Shouldly;` but a `using static` per used Shouldly class plus an alias per named type (`using Should = Shouldly.Should;`). Rounds: CS0121 → that class stays out and that name is blocked; misbinding → name blocked; a rewrite that calls a blocked name becomes Manual per call (`Fixer.BlockedCall`) and stays FA. Check without the global model using; if another compile error appears, the old per-file fallback. Approach 2 not done.
- Code: `Fixer.cs` (`FixFileAsync` taken out of the loop, `FilePass`), `UsingDirectives.cs` (`ConflictsWithAddedAsync`, `AddedUsageAsync`, `AddTargetedImports`, `ErrorCountAsync`), `AddedConflict`, `AddedUsage`.
- Tests: 3 new in `AmbiguousAddedNamespaceTests` (mixed file, namespace variant, fallback on a clashing alias); proof `Proofs/FluentAssertions/BeTrueFalse/own-shouldbe-in-scope.*`. 1066 tests green.
- Akka (`23a8566`, clean, restore, fix, build): Fixed 5,463 → **6,016**, Manual 5,518 → **4,649**; conflict Manuals 3,449 → 2,943 (now per call: `ShouldBe` 1,832, `ShouldBeTrue` 506, `ShouldBeOfType` 252 (class left out), `ShouldBeFalse` 146 …). 584 files written (was 431). Build **0 errors** (no CS8130/CS0119/CS1061/CS0452). Tests equal to the earlier logs (`t2-akka`, `t2-ddata`, t1): Akka.Tests 1553/0 failed, Akka.TestKit.Tests 337/0, DData 201/1 failed — the same known `ReplicatorMessageSerializerSpec` (TAAK-200, warning). 0 regressions.

## See also

- `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`
- `claude/tasks/CORPUSRUN-fa.md`
