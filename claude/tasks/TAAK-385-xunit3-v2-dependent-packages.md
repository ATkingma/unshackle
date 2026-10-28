# TAAK-385 — xunit3: third-party packages built on xUnit v2 keep v2 assemblies (CS0433)

**Status**: step 1/3 — not started
**Created**: 2026-10-28
**Goal**: A project that references a package depending on xUnit v2 is a blocker (Manual) instead of a migration that does not build.

## Context for the next agent

- Corpus (`claude/tasks/CORPUSRUN-xunit.md`), Bogus `6ece18c`: `Source/Bogus.Tests` references `Verify.Xunit` 22.8.0, which depends on xunit v2.
  After the fix v2 `xunit.core` stays in the graph next to `xunit.v3.core`: 1284x CS0433 (`FactAttribute` exists in both).
- Second case, FakeItEasy `ff2aa81` (masked by a real `xunit3.extensibility` Manual row): `tests/FakeItEasy.Specs` uses `bbv.LambdaTale`
  2.9.305 (598 `[Scenario]` tests; LambdaTale builds on v2). The fix sees no xUnit test attributes there (X4) and would migrate it as a
  library (`xunit.v3.extensibility.core`, no `Exe`): the tests would stop being discovered, and v2 stays in the graph.
- The fix only looks at the project's own `xunit*` references. Other v2-based packages seen in the corpus: `Verify.Xunit` (v3:
  `Verify.XunitV3`), `Xunit.SkippableFact` (FakeItEasy, X14: out of scope, Info), `XunitXml.TestLogger` (logger, not a v2 dependency).
- Detection idea: after the package edits, a v2 assembly (`XunitV3Assemblies.V2`) still referenced by the compilation through a non-xunit package
  is a blocker; or a known list (package → v3 counterpart). Council question.
- Pending repro: `src/Unshackle.Tests/Proofs/_Pending/XunitV3/V2DependentPackage/verify-xunit.repro.txt`.

## Steps

- [ ] 1. Council: transitive-reference detection vs. a known list; rename `Verify.Xunit` → `Verify.XunitV3` or only block
- [ ] 2. Build it with a unit test
- [ ] 3. Re-run Bogus

## See also

- `claude/tasks/CORPUSRUN-xunit.md` — corpus round
- `claude/tasks/TAAK-341-xunit-v3.md` — the xunit3 fix
