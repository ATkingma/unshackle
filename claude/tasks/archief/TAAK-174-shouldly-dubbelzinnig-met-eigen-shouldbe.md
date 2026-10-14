# TAAK-174 — `using Shouldly;` makes own `ShouldBe` extensions ambiguous (CS0121)

**Status**: completed (2026-10-13)
**Created**: 2026-10-12 (corpus run, `claude/tasks/CORPUSRUN-fa.md`)
**Goal**: Akka.NET builds after the fix, or gets a clear Manual instead of a broken build.

## Context for the next agent

- Akka.NET (`23a8566`): after the fix **4,984× CS0121** "ambiguous between `Akka.TestKit.AkkaSpecExtensions.ShouldBe<T>(T, T, string)` and `Shouldly.ShouldBeTestExtensions.ShouldBe<T>`". Example: `src/contrib/cluster/Akka.Cluster.Sharding.Tests.MultiNode/ClusterShardingCustomShardAllocationSpec.cs:221`. Besides that a few CS8130, CS0119, CS1061 and CS0452 (not yet investigated).
- The fix reported 8,488 Fixed / 1,868 Manual. Baseline: the large assemblies are green, with 2–3 known failures in Streams/Cluster.
- Related: TAAK-165 (own `Should()` extension) and "TAAK-150" (own `ShouldBe`) from `REGRESSIE-corpus-2.md`. That last task file was never created.

## Analysis

### Problem

Files that already import `Akka.TestKit` (with among others `ShouldBe` and `ShouldBeTrue`) get `using Shouldly;` added. That makes both the existing and the newly written calls ambiguous.

### New situation

Before `using Shouldly;` is added, the Fixer checks semantically whether extension methods with Shouldly names are already in scope (`ShouldBe`, `ShouldBeTrue`, `ShouldContain`, …). If so, the file becomes Manual ("conflicting Should* extensions from X") and the FA package stays in the project (project-file cascade). Fully qualified Shouldly calls were rejected because they become unreadable.

## Steps

- [x] 1. Unit test with an own static `ShouldBe<T>` extension in scope.
- [x] 2. Conflict detection in the using step (`UsingDirectives.cs`) or as a pre-check in the Fixer.
- [x] 3. Proof: the Akka harness builds; investigate the other error codes and create a follow-up task if needed.

## Result of step 3 (2026-10-13)

Akka.NET `23a8566`, fresh clone in `<probe-dir>`, `unshackle fix Akka.slnx --fix fluentassertions --force`, then bin/obj removed, restore + build with `-p:NuGetAudit=false -p:AllowMissingPrunePackageData=true`:

- Fix: **5,463 Fixed / 5,126 Manual** (without `project-file`/`usings`; 441 files written). Of those, 3,449 Manual with "… is ambiguous with Akka.TestKit.AkkaSpecExtensions.… once Shouldly is imported (CS0121)".
- **Build: 0 errors** (was 4,984× CS0121 + 8× CS8130, 8× CS0119, 6× CS1061, 6× CS0452). The other codes disappeared along with it; the old log no longer has line information, they were in files that are now Manual (follow-up errors of the ambiguity).
- Tests (whole solution, 46 assemblies) against the two baseline runs: known failures (Cluster `ProviderSelectionSpec`, DData, Streams `TcpSpec`) equal. New:
  - `MixedBindingPairSpec.Should_FailTheCheck_When_TheV2NodeIsStillPinnedToLegacy` — a real regression: an own `ShouldBe` in the enclosing namespace `Akka.TestKit` silently wins over Shouldly → TAAK-199.
  - `ReplicatorMessageSerializerSpec.ReplicatorMessageSerializer_should_serialize_Replicator_message` — a real regression (red 2× on its own): `object` subject `ShouldBe` → TAAK-200.
  - `ConductorBindSpec` (3×, port 2552 in use) and `ClusterShardingGetStateSpec` (MultiNode, `ExpectMsg` timeout; the diff is equivalent) — flaky: Remote.TestKit on its own 40/40 green.
- Follow-up: TAAK-198 (shrink the conflict Manual), TAAK-199, TAAK-200.

## See also

- `claude/tasks/CORPUSRUN-fa.md`
- `claude/tasks/archief/TAAK-198-shouldly-conflict-manual-verkleinen.md`
- `claude/tasks/archief/TAAK-199-eigen-shouldbe-in-omsluitende-namespace.md`
- `claude/tasks/archief/TAAK-200-shouldbe-object-gelijkheid-ddata.md`
- `claude/tasks/archief/TAAK-165-custom-assertions-should-extensie.md`
