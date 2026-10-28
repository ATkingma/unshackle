# TAAK-386 — xunit3: FactAttribute subclass needs the source-information constructor (xUnit3003)

**Status**: step 1/3 — not started
**Created**: 2026-10-28
**Goal**: A v2 class deriving from `FactAttribute`/`TheoryAttribute` gets the v3 `[CallerFilePath]`/`[CallerLineNumber]` constructor, so
TreatWarningsAsErrors builds keep building.

## Context for the next agent

- Corpus (`claude/tasks/CORPUSRUN-xunit.md`), YamlDotNet `4f7e6e3` (`Directory.Build.props` sets `TreatWarningsAsErrors`): after the fix
  the build fails with xUnit3003 on `YamlDotNet.Samples/Helpers/SampleAttribute.cs` (`class SampleAttribute : FactAttribute` without a
  constructor). Everything else (15 Fixed, 0 Manual) built.
- v3 `FactAttribute` has `FactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)`; the analyzer
  wants each subclass to forward them. TAAK-341 X12 does not report `FactAttribute` subclasses (they compile).
- Related to TAAK-382 (other analyzer ids under TreatWarningsAsErrors), but mechanical: a pattern can add the constructor (or forward the
  parameters in every existing constructor).
- Pending proof: `src/Unshackle.Tests/Proofs/XunitV3/_Pending/FactAttributeSubclass/source-info-constructor.*` (check the v3 3.2.2 constructor
  signature when moving it out of `_Pending`).

## Steps

- [ ] 1. Pattern `xunit3.fact-source-info`: no constructor → add one; existing constructors → add the two parameters and the base call; a
  constructor that already calls another base overload → Manual
- [ ] 2. Proof out of `_Pending`, KingmaDoc plan + verify, README row
- [ ] 3. Re-run YamlDotNet

## See also

- `claude/tasks/CORPUSRUN-xunit.md` — corpus round
- `claude/tasks/TAAK-382-xunit3-analyzer-warnings.md` — other analyzer warnings
- `claude/tasks/TAAK-341-xunit-v3.md` — the xunit3 fix
