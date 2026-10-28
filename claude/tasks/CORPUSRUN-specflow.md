# Corpus run SpecFlow → Reqnroll (round 1, TAAK-340 step 7)

Date 2026-10-28. Harness `claude/tools/corpus-regression.sh` (list `claude/tools/corpus-specflow-list.txt`, `JOBS=2`,
`MSBUILD_EXTRA=-m:2`, `TEST_TIMEOUT=1200`, own `NUGET_PACKAGES`), inventory `claude/tools/corpus-specflow-summary.py`.
Clones (shallow, HEAD 2026-10-28) in `corpus-specflow/src/`, results in `corpus-specflow/out2/<name>/` on the corpus disk.
Per repo: restore, build, test twice (TRX, per test), `fix --fix specflow`, bin/obj removed, restore, build, test, compare per test.

## Results

| Repo                                         | SpecFlow                                  | Status                                                  | Tests before (pass/fail) | Tests after               | Fix result                                                           |
| -------------------------------------------- | ----------------------------------------- | ------------------------------------------------------- | ------------------------ | ------------------------- | -------------------------------------------------------------------- |
| cucumber-examples/shouty.net                 | 3.7.38 NUnit                              | ok                                                      | 4 (2/2)                  | 4 (1/1, 2 NotExecuted)    | **migrated; 1 regression → TAAK-377**                                |
| corvus-dotnet/Corvus.Testing                 | 3.9.74 NUnit + `.NUnit.Runners`           | ok                                                      | 66 (6/60)                | 66 (6/60)                 | blocked (unknown `SpecFlow.NUnit.Runners`, SharedDeclarationBlocked) |
| corvus-dotnet/Corvus.Retry                   | via `Corvus.Testing.SpecFlow.NUnit`       | ok                                                      | 28 (28/0)                | 28 (28/0)                 | blocked (transitive runner, TAAK-379)                                |
| ais-dotnet/Ais.Net                           | via `Corvus.Testing.SpecFlow.NUnit`       | ok                                                      | 2625 (2625/0)            | 2625 (2625/0)             | blocked (transitive runner, TAAK-379)                                |
| matthewrenze/clean-architecture-demo         | 3.9.74 NUnit + LivingDoc                  | ok                                                      | 49 (47/2)                | 49 (47/2)                 | blocked (LivingDoc)                                                  |
| executeautomation/Playwrightdotnet           | 3.9.74 NUnit + Assist.Dynamic + LivingDoc | ok (UI suite)                                           | 2 (0/2)                  | 2 (0/2)                   | blocked (plugins); row on `obj/*.GlobalUsings.g.cs` → TAAK-378       |
| ERNI-Academy/net6-automation-testware        | 3.9.74 MsTest + Allure                    | ok (UI suite)                                           | 37 (7/30)                | 37 (9/28, browser flakes) | blocked (Allure)                                                     |
| SkillsFundingAgency/das-learning             | 3.9.74 NUnit + LivingDoc                  | baseline build timeout (30 min)                         | -                        | -                         | -                                                                    |
| nationalarchives/discovery-bdd-test          | 3.0-3.3 NUnit, netcoreapp2.1              | baseline build fails (SpecFlow 3.0 generator on SDK 10) | -                        | -                         | -                                                                    |
| toniarnold/aspnettest (`asp.blazor.sln`)     | 3.9.74 NUnit + LivingDoc                  | baseline build fails (`.vcxproj`)                       | -                        | -                         | -                                                                    |
| hippo-digital/playwright-specflow-pom-dotnet | 3.9.22 NUnit + LivingDoc                  | restore timeout (30 min)                                | -                        | -                         | -                                                                    |
| godrose/Solid (`src/Solid.sln`)              | 3.1.97/3.9.74 xUnit + LivingDoc           | restore fails (case-sensitive project paths)            | -                        | -                         | -                                                                    |

7 of 12 with a baseline; on all 7 the same tests were discovered before and after (S8 bar met). Blocked runs change nothing
(AllOrNothing per unit), so their equal results only prove the "no change" path. Only shouty migrated, and it regressed.

## Findings

- **TAAK-377 (bug, regression)**: `specflow.step-expression` pinned `"Lucy is at {int}, {int}"` as regex. SpecFlow 3.7.38 matched it as
  a Cucumber expression; after the pin Reqnroll reports the step undefined (Passed → NotExecuted). Pending proof in
  `Proofs/SpecFlow/_Pending/StepExpression/`.
- **TAAK-381 (harness)**: `corpus-regression.py` does not count Passed → NotExecuted, so the compare said `regressions: []`.
- **TAAK-378**: a finding on `obj/Debug/net7.0/*.GlobalUsings.g.cs` (generated from `<Using>` items).
- **TAAK-379**: the SharedDeclarationBlocked reason blames imports while the runner comes from a wrapper package's dependencies.
- Gap (no task yet, backlog for TAAK-340): `SpecFlow.NUnit.Runners` is unknown and blocks; it is a meta package
  (`SpecFlow.NUnit` + NUnit adapter) that could map to `Reqnroll.NUnit` plus `NUnit3TestAdapter`.
- Blockers by cause: LivingDoc 3 repos (+3 without baseline), Allure 1, Assist.Dynamic 1, transitive runner 2, unknown package 1.
  LivingDoc dominates, as the scout predicted.

## Inventory

- **S13 step texts**: 2 pinned as regex (both shouty, both wrong: TAAK-377); 1 more candidate in Corvus.Retry
  (`PolicyStepBindings.cs:58`) hidden behind the AllOrNothing "Not rewritten" row, so its own verdict is not in the report; 0 own Manual.
- **S15 App.config**: 0 `<specFlow>` sections in the 7 measured repos.
- **S16**: `*.feature.cs` Info rows in all 7 measured repos (code-behind present after the baseline build or checked in).
- **S17 kept/closure**: 0 `specflow.kept` / `specflow.closure` rows; every unit in a blocked repo had its own root blocker.
- `specflow.json` kept (S14 Info): 18 projects.

## Next round

- After TAAK-377..381: rerun shouty, and pick repos without LivingDoc (or strip LivingDoc in a copy) so more runs actually migrate.
- das-learning and playwright-pom with a longer timeout when the machine is idle.

## See also

- `claude/tasks/TAAK-340-specflow-to-reqnroll.md` — step 7
- `docs/projectinfo/architectuur/candidate-specflow.md` — scout survey
- `claude/tasks/TAAK-377-specflow-step-expression-cucumber-parameter.md`, `TAAK-378-specflow-generated-obj-documents.md`,
  `TAAK-379-specflow-transitive-runner-reason.md`, `TAAK-381-corpus-compare-notexecuted.md`
