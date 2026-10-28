# TAAK-383 — xunit3: Test.Sdk edit is Manual when TargetFrameworks uses a property reference

**Status**: step 1/3 — not started
**Created**: 2026-10-28
**Goal**: The X15 Test.Sdk decision uses the evaluated target frameworks, so a `$(...)` TFM no longer blocks the whole solution.

## Context for the next agent

- `XunitV3Fix.TestSdkEdit` reads `IProjectProperties.TargetFrameworks` (`src/Unshackle.DataAccess/Projects/XmlProjectProperties.cs`), which
  returns null on any `$(` and the fix then makes the Test.Sdk edit Manual. Council round 25 X15 asked for the evaluated union "after imports
  and conditions". Under AllOrNothing that one Manual row reverts the whole solution.
- Corpus (`claude/tasks/CORPUSRUN-xunit.md`): serilog `43105b3` — the only blocker; `test/Serilog.Tests` has
  `<TargetFrameworks>$(TargetFrameworks);net10.0;net9.0;net8.0</TargetFrameworks>` (net48;net462 added only on Windows). Ardalis.Result
  `14cf45a` — 4 rows, `$(NetCoreFrameworks)` from `tests/Directory.Build.props` (there masked by a real net6.0 blocker).
- The Roslyn project already knows the evaluated TFM: MSBuildWorkspace loads one `Project` per TFM, and `XunitV3TargetFramework` reads
  `NETFRAMEWORK`/`NETCOREAPP` from `ParseOptions.PreprocessorSymbolNames`. The union over the sibling projects with the same `FilePath` is the
  evaluated set for the current OS (an OS-conditional netfx TFM, as in serilog, is then invisible on Linux: say so in the Info row).
- Pending repro: `src/Unshackle.Tests/Proofs/_Pending/XunitV3/TestSdkTargetFramework/props-reference.repro.txt`.

## Steps

- [ ] 1. Council: evaluated symbols (current OS) vs. raw XML superset; what to say about OS-conditional TFMs
- [ ] 2. Build it with a unit test from the repro
- [ ] 3. Re-run serilog in the corpus

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core/Fixes/XunitV3/XunitV3Fix.cs` (`TestSdkEdit`) |
| **How**   | Use the evaluated TFMs (preprocessor symbols of the loaded projects) |
| **Why**   | TFMs from props files and `$(TargetFrameworks)` are common; today they block a migration that would build |

## See also

- `claude/tasks/CORPUSRUN-xunit.md` — corpus round
- `claude/tasks/TAAK-341-xunit-v3.md` — the xunit3 fix, X15
