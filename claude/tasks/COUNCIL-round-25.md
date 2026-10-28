# Council round 25 — Test.Sdk scope, App.config section, feature.cs, mixed helper

Date: 2026-10-28. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. Three separate sub-agents
gave positions. Round 1 agreed on S15 and S16 and split on X15 (G: bump every target; P, A: netfx only) and S17 (G, A: keep both units;
P: accept with a Warning). Round 2 put a joint proposal to each dissenter; both accepted with amendments, folded into the rules below
(G: evaluated TFMs, unresolvable → Manual, NU1605 case Manual; P: root blocker named, fixpoint closure, closure-only count).
Questions: TAAK-341 council question 1, TAAK-340 council questions 14 and 15, TAAK-353 council question 1.
Follow-ups: TAAK-341 (X15), TAAK-340 step 5 (S15, S16), TAAK-353 (S17).

Facts checked in the decompiled packages (`Reqnroll.dll` 3.3.4, `Reqnroll.SpecFlowCompatibility` 3.3.3, the latest; copies in
`/mnt/dikkeschijf/unshackle-corpus/tmp/c25-compat/`):

- Reqnroll runtime `ConfigurationLoader.Load`: json config or defaults; App.config is never read.
- Generator side: an App.config source gives a stub (defaults + `ConfigSourceText`); Reqnroll itself applies nothing from it.
- Only the compat package's `GeneratorPlugin` (on `ConfigurationDefaults`) parses `<specFlow>` via its `ConfigurationSectionHandler`:
  `language`, `bindingCulture`, `generator`, `runtime`, `trace`, `stepAssemblies`. It has no `unitTestProvider` or `plugins`
  property, so such an element would be rejected by `DeserializeSection` (inferred, not built).
- Runner 3.1.5: `Microsoft.TestPlatform.ObjectModel` 17.13.0 for net472 only (TAAK-341 step 6).

| # | Decision | Joint rule |
| --- | --- | --- |
| X15 | Test.Sdk bump scope | Bump to 17.13.0 only when an evaluated TFM (union of `TargetFramework`/`TargetFrameworks` after imports and conditions) is .NET Framework. An unresolvable TFM → Manual. Net-only below 17.13.0 → Info "17.13.0+ recommended". A CPM `PackageVersion` below 17.13.0 shared with a non-xUnit consumer while a netfx xunit3 project uses it → Manual (restore fails, NU1605), naming the other project. |
| S15 | App.config `<specFlow>` (Q14) | Empty or `<unitTestProvider>`-only section → Info "ignored by Reqnroll; delete it", unless `Reqnroll.SpecFlowCompatibility` is referenced (then blocker: it rejects `unitTestProvider`). Any other element stays a blocker. Reason text reworded: Reqnroll ignores App.config; only the SpecFlowCompatibility generator plugin reads language, bindingCulture, generator, runtime, trace and stepAssemblies; move them to reqnroll.json. |
| S16 | `*.feature.cs` (Q15) | Patterns skip a `*.feature.cs` that has a sibling `.feature`; one Info row per project "generated code-behind skipped; regenerated on build". |
| S17 | Mixed helper (TAAK-353) | Units that share any dependent project migrate together or not at all (transitive, to a fixpoint). The held-back unit's row names the chain and the root blocker on the kept unit; the summary counts units held back only by the closure. |

## X15 — Test.Sdk

- G: first wanted every target for discovery safety; dropped it, since net8 restores fine and the risk was unverified.
- P: net8-only bumps are churn and widen the CPM conflict surface.
- A: the rule must match its cause, the net472 dependency.

## S15 — App.config

- G: Info only without the compat package; with it, generation likely fails.
- P: blocking a project for dead config costs much for zero risk.
- A: the old reason text is half wrong; name what moves to reqnroll.json.

## S16 — feature.cs

- G: edits that vanish on the next build mislead reviewers.
- P: diff noise reviewed for nothing.
- A: we fix sources, the build makes outputs; a hand-written `.feature.cs` without a `.feature` is still processed.

## S17 — mixed helper

- G: two runtimes in one closure fail at run time (undefined steps, hooks not firing).
- P: accepted, provided the hand step stays one fix (root blocker named).
- A: "migrate together or not at all" is a predictable graph closure.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-23.md`
- `claude/tasks/TAAK-340-specflow-to-reqnroll.md`
- `claude/tasks/TAAK-341-xunit-v3.md`
- `claude/tasks/TAAK-353-specflow-partial-migration.md`
