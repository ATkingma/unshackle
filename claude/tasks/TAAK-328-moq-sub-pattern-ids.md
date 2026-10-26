# TAAK-328 — Moq sub-pattern ids (council round 13, M8)

**Status**: blocked (needs a Core seam outside the Moq scope)
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

## Proposed Core seam (needs approval: outside `src/Unshackle.Core/Fixes/Moq/**`)

- `PatternResult` gets `public string? PatternId { get; init; }` (null: the pattern's own id).
- `FileFixer` (around line 282) and `ReportOnlyRun` (around line 74) use `reported.PatternId ?? pattern.Id`; `FindingFactory.CreateFinding(Finding, PatternResult)` keeps the original row id.
- Moq: `MoqBlocker` maps its code to a row id; `CreatePattern.RewriteTracking` and `RewriteStatic` (predicate) set `moq.properties` / `moq.setup-returns`.

## Steps

- [ ] 1. Approve the Core seam (council or owner)
- [ ] 2. Core seam + unit test in `src/Unshackle.Tests/Core/`
- [ ] 3. Moq ids per result; report tests; `SetupsOverlap` text names `Configure()`
- [ ] 4. KingmaDoc docs (`moq-create-*`, `moq-properties-*`, `moq-unsupported-*`), README rows
- [ ] 5. Build + full test run green

## Progress log

- 2026-10-26: analysed; stopped before code because the change needs Core, which this assignment may not touch.

## See also

- `claude/tasks/COUNCIL-round-13.md` (M8, M9)
- `claude/tasks/moq-queue.md`
- `docs/projectinfo/patterns/moq-properties-verify.md` (step 1 already notes the missing per-result id)
