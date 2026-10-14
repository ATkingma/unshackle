# TAAK-111 — Project file not editable → code rewritten anyway (Shouldly missing)

**Status**: done
**Created**: 2026-10-11 (corpus run, `claude/tasks/CORPUSRUN-fa.md` follows)

## Context

Polly (308× CS0246 `Shouldly`) and Ardalis.Result (96× CS0246, 5 csproj files): finding `project-file` Manual "cannot be edited without changing unrelated text; migrate it by hand". The FA package stays, Shouldly is not added, but the `.cs` files do get `using Shouldly` → build broken.
Polly.Specs.csproj starts with a UTF-8 BOM (`EF BB BF`); presumably the byte-exact round-trip check (review B, L5) fails on the BOM.

## Steps

1. Reproduce with a csproj with BOM (and CRLF) in `XmlProjectFileStoreTests`.
2. Make the round trip preserve BOM and line endings (write the BOM back).
3. Safety rule: if a project's project file cannot be edited, do not rewrite any `.cs` files of that project (everything Manual with the same reason) — never leave behind a project that does not build.
4. Evidence: Polly and Ardalis.Result copies `fix` again + build green.

## Outcome (2026-10-11)

**Status**: done. The cause was not the BOM but the byte-exact round trip (e.g. a double space before an attribute); solved with
TAAK-134 (textual splice). Safety rule in `Fixer.MigrateProjectFilesAsync`: if `EditAsync` fails, all
`.cs` files of the affected projects go back to the original text and their Fixed findings become Manual
(test `FixerProjectFileTests.FixAsync_EditThrows_LeavesTheCodeOfThatProjectUnchanged`; BOM+formatting in `XmlProjectFileStoreTests`).

Evidence on fresh copies (`scratchpad/v134/`, fix + build):
- Polly: `Polly.Specs.csproj` gets Shouldly (formatting preserved); 308x CS0246 gone. Remaining: 1x CS1061 `Guid.ShouldNotBeEmpty`
  (`ContextSpecs.cs:43`, separate pattern problem, not a project file).
- Ardalis.Result: build green (289 Fixed, 39 Manual).

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
