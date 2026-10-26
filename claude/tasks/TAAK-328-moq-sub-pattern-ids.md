# TAAK-328 — Moq sub-pattern ids (council round 13, M8)

**Status**: done
**Created**: 2026-10-26
**Branch**: `pool/taak-328-wip`
**Goal**: every Moq finding row carries the id of the `moq-queue.md` row it belongs to (rename only, no behaviour change); the `SetupsOverlap` reason text names `Configure()` as a manual route (M9).

## Context for the next agent

- `FileFixer.ApplyPatternAsync` and `ReportOnlyRun` take the row id from `pattern.Id`; an `IPattern` has one id.
- Results that report under a foreign id today, and why they cannot be split into separate patterns:
  - `SetupProperty`/`SetupAllProperties` (row 5, `moq.properties`) report as `moq.create`: they must be rewritten in the same pass as the create analysis (`allTracked` pins); a separate earlier pattern would change the pins (behaviour change).
  - `Mock.Of<T>(predicate)` (row 3, `moq.setup-returns`) reports as `moq.create` Manual `MockOfSetup`.
  - Mock-use blockers `StrictBehaviour`, `ProtectedMember`, `MockAs`, `MockInvocations`, `MockReset`, `MockRaise` (row 13, `moq.unsupported`) surface on whichever pattern analysed the mock: the id depends on the result, not on the node.
- A stateful pattern (id set during `Rewrite`) is not safe: files run in parallel (`Fixer`, `Parallel.ForEachAsync`) on shared pattern instances.

## Core seam (approved by the coordinator)

- `PatternResult` gets `public string? PatternId { get; init; }` (null: the pattern's own id).
- `FileFixer` (around line 282) and `ReportOnlyRun` (around line 74) use `reported.PatternId ?? pattern.Id`; `FindingFactory.CreateFinding(Finding, PatternResult)` keeps the original row id.
- Moq: `MoqBlocker` maps its code to a row id; `CreatePattern.RewriteTracking` and `RewriteStatic` (predicate) set `moq.properties` / `moq.setup-returns`.

## Steps

- [x] 1. Approve the Core seam (council or owner)
- [x] 2. Core seam + unit test in `src/Unshackle.Tests/Core/`
- [x] 3. Moq ids per result; report tests; `SetupsOverlap` text names `Configure()`
- [x] 4. KingmaDoc docs (`moq-create-*`, `moq-properties-*`, `moq-unsupported-*`), README rows
- [x] 5. Build + full test run green

## Progress log

- 2026-10-26: analysed; stopped before code because the change needs Core, which this assignment may not touch.
- 2026-10-26: seam approved and built; Moq ids per result (`MoqBlocker.ToResult`, `CreatePattern`), M9 `Configure()` route text on `SetupsOverlap`; `SubPatternIdTests` (4); 3731 tests green (one unrelated flaky failure on a first run, green on rerun). README rows unchanged (no description column).

## See also

- `claude/tasks/COUNCIL-round-13.md` (M8, M9)
- `claude/tasks/moq-queue.md`
- `docs/projectinfo/patterns/moq-properties-verify.md` (step 1 already notes the missing per-result id)
