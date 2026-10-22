# Council round 12 — questions (2026-10-22)

Answer by id. Code on main. Harness facts are pinned by tests in `src/Unshackle.Tests/Core/EPPlus/`.

## EPPlus

- **E12. Reading cells.** Harness: `.Text` of a bool is "1" (EPPlus) vs "TRUE" (ClosedXML `GetFormattedString()`); `GetValue<string>()` on an empty cell is null vs ""; `.Value` is `object` vs `XLCellValue`. All reads are now Manual `CellReadDiffers`. Opening an existing workbook (`new ExcelPackage(stream|FileInfo|path)`, 135 hits / 22 repos) is mostly read code. Keep all reads Manual, or define Fixed read shapes (e.g. `GetValue<int>()` on a typed column, `.Text` only for strings) with harness proofs?
- **E13. AutoFit.** `AutoFitColumns()` depends on font metrics. Measured tolerance through the harness, or Manual for good?
- **E14. Style proofs with a scaffold.** Style/layout harness proofs translate the surrounding workbook code by hand, because a file rewritten by one pattern alone does not compile. Now that cells/package are migrated in the same run, require full `.after.cs` proofs per shape, or accept scaffold proofs?
- **E15. LoadFrom with DateTime/enum.** Add harness cases so `LoadFromCollection` with DateTime/enum properties can be Fixed (EPPlus writes DateTime as a General serial), or keep Manual?
- **E16. Dimension with styled empty cells.** EPPlus counts styled-but-empty cells in `Dimension`; ClosedXML `*Used()` does not by default. Harness case + `XLCellsUsedOptions.All`, or a documented "values only" limit (Manual when the file sets styles)?

## Engine

- **G6.** A `SharedDeclarationBlocked` outcome makes the project's other edits Manual. Should it also keep the package migration from touching that project? (Partly covered by TAAK-321.)
- **G7.** G2 decides per project in order; an earlier row can say "stays, X still references it" while a later project removes it. Solution-level pass instead?
- **G8.** A removed first using with a comment in front loses that comment. Keep the comment?
- **G9.** Pre-filter: a fix whose old and new library share a namespace (Polly) has an empty `RemovedNamespaces` and was skipped in real runs (bug found and fixed with `FindsOnlyOldLibraryCode => false`; smoke test pins it). Should fixes name their old-library assemblies for the pre-filter instead?

## Moq

- **M6.** Two covered setups on the same member (`SetupsOverlap`): Moq counts a call only for the latest matching setup, NSubstitute `Received` sees every call. Keep Manual?
- **M7.** Row 5 (properties) has no pattern id of its own; findings carry `moq.setup-returns`, `moq.verify`, `moq.create`. Acceptable for report grouping?

## AutoMapper / MassTransit (TAAK-311, TAAK-310)

- **A9.** A8 as built also blocks when the method reads a member of the mapped value (it throws or branches on the loose mock). Keep this strict?
- **A10.** A7 "built in the test": also an `A` created in place (`ReturnsAsync(Result<Account>.Success(new Account()))`) when it is the only `A` in the method?
- **T8.** Any MassTransit filter in the solution makes every consumer Manual; filters registered outside the solution are not seen. Acceptable?

## See also

- `claude/docs/council.md` — rounds 8–11
