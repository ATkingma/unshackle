# Council round 10 — questions (2026-10-22)

From the wave-2 agents. Code is on main (`106785a`+). Answer by id.

## Cross-cutting (C)

- **C1. Typed reason codes.** Polly, EPPlus and MediatR (TAAK-312) put the code as the first word of the Manual text, because `ManualResult` only has `Kept` (a `KeptReason` marks a finding as "kept, the rest goes ahead", which is wrong for root blockers). Add a general reason-code field to `ManualResult` (engine work), or keep the text prefix?
- **C2. Fix-level info.** Polly P5 wants the report to state the Polly version found; that needs a fix-level Info hook (IFix has none). TAAK-316 added `PatternResult.Note` and Info rows. Use a fix-level Info row, or skip?
- **C3. Blank first line.** When `using OfficeOpenXml;` was line 1 the Fixer leaves a blank first line. Fix in the engine?

## Engine (TAAK-316/317, `docs/projectinfo/architectuur/core.md`)

- **G1.** The report-only header is a Finding row (CSV/JSON carry it, IReportWriter unchanged) instead of a header/metadata parameter. Keep?
- **G2.** Under CPM, remove a renamed package's old `PackageVersion` when no project under that `Directory.Packages.props` still uses it? (Now: kept.)
- **G3.** Allow edits to a package declared only in a shared `Directory.Build.props` when every project using it has 0 blocking rows? (Now: Manual.)
- **G4.** Should project-level Manual rows (e.g. "package kept") also block a project's file edits? (Now: only document rows.)
- **G5.** `ConfigFileTransformEdit` = exact literal replacements, each exactly once. Enough for `specflow.json` → `reqnroll.json`, or a typed JSON-key rename?

## Polly (`src/Unshackle.Core/Fixes/Polly/`, `docs/projectinfo/patterns/polly-wait-and-retry-async-plan.md`)

- **P8.** A direct `Polly.Core` reference: not in the removed set; the rewritten code binds to Polly.Core, so the "uses the assembly" check would keep it forever. Remove a direct Polly.Core reference (M.E.Resilience brings it transitively)?
- **P9.** M.E.Resilience 10.11.0 on corpus projects targeting netcoreapp3.0/net5/netstandard2.0: Manual per project (`TargetFrameworkUnsupported`), or an older M.E.Resilience version?
- **P10.** Introduced names `args`, `SleepDuration`, `_` → `NameClash` Manual on collision. Acceptable, or pick fresh names?

## MediatR (TAAK-312, `AssemblyResolver.cs`)

- **R1.** Packability is guessed (library without a test-framework reference). Expose MSBuild `IsPackable` per project from DataAccess?
- **R2.** The internal method without a friend caller whose callers are unknown (`edge-assemblies`) was Fixed before TAAK-312 and stays Fixed (it scans its own project). Make it Manual per the literal A2 text (a regression), or keep?
- **R3.** "Same literal": compared per assembly of each X, so different types in one assembly count as the same scan. Acceptable?

## EPPlus (`src/Unshackle.Core/Fixes/EPPlus/`, `src/Unshackle.Tests/Core/EPPlus/`)

- **E7.** ClosedXML 0.105.1 allows `SixLabors.Fonts [1.0.0, 3.0.0)`; 2.x is under the Six Labors Split License. Pin SixLabors.Fonts < 2.0 in the user's project (via the project-file step), only warn, or nothing?
- **E8.** Column width: ClosedXML adds cell padding (EPPlus 20 reads back 19.289). Rewrite `Width = w` as `w + padding` (font-dependent), or Manual?
- **E9.** `epplus.package`: also accept a plain local without `using` (neither library needs dispose for correctness)?

## See also

- `claude/docs/council.md` — rounds 8 and 9
