# Corpus run Moq (TAAK-307 row 14, lane L6)

Tool: `main` 0842b542 (all Moq rows 0-13), published to `corpus-moq/tool-0842b542`. Harness: `claude/tools/corpus-moq-run.sh` (base, scan, after per repo,
one at a time) over `corpus-moq-am.sh` (a copy of `corpus-am.sh` with `RUNNER_FORCE=vstest` for solutions that mix VSTest projects with an MTP marker:
fluentmigrator). Repos: `claude/tools/corpus-moq-list.txt`. Tables: `python3 -I claude/tools/corpus-moq-summary.py <corpus root>`.
Results and logs on the corpus disk: `<corpus-moq root>/_results/{base,scan,after}/<repo>` (the corpus disk, never /tmp).

Status: WIP, the run is long (one repo takes 1.5 h or more at the current machine load of ~40) and continues in the background.

## Results

See the table at the bottom (regenerate with the summary script; paste it here when the run ends).

## Bugs and findings (repro, not fixed)

### B1 fluentmigrator: a test that asserts the runtime proxy type name

`MigrationRunnerTests.CanListVersions` (`test/FluentMigrator.Tests/Unit/MigrationRunnerTests.cs:642`) expects log lines such as `2011010101: IMigrationProxy`:
the name of the Castle proxy class Moq generates for `new Mock<IMigration>().Object`. After `moq.create`, `Substitute.For<IMigration>()` is a
differently named proxy, so the test fails (the only regression of the repo: 5605 pass before, 5604 pass and 1 fail after).
Not provable as equal: the type name of the mock object differs between the libraries. Candidate: `moq.create` reports Manual (or an Info line) when
`.Object` flows into code that is not visible (here `MigrationInfo(.., mockMigration.Object)` whose `ToString()`/`GetType().Name` is asserted); needs a council decision
(detecting it statically is hard: any `.Object` can reach a `GetType()`).

### H1 ocelot: restore fails on the first attempt

`restore0` NU1102 on `manual/Ocelot.ManualTest.csproj` (packages found on nuget.org but not resolvable: transient under load); retried at the end of the run.

## Fixed / Manual counts per repo

(from `report.json`; fluentmigrator: 628 Fixed, 1299 Manual; Manual reasons: ClassMockSetup 524, DefaultValueDiffers 266, MockUsedBeyondObject 233,
MockPassedAround 109, ClassMockNoCallBase 59, MatcherTypeDiffers 39, CallShape 28, MockGet 17, TooManyDefaultPins 12, StrictBehaviour 3.)

## See also

- `claude/tasks/TAAK-307-moq-to-nsubstitute.md`
- `claude/tasks/moq-queue.md`
- `docs/projectinfo/architectuur/corpus-moq.md`
