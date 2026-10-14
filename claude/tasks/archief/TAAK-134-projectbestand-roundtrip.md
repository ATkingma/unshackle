# TAAK-134 — Project files: "cannot be edited without changing unrelated text" on ordinary formatting

**Status**: done
**Created**: 2026-10-11
**Found in**: `claude/tasks/CORPUSRUN-mediatr.md` — biggest blocker: 6 of 9 solutions

## Problem

`ProjectFile` (DataAccess) rejects every file whose `XDocument` round trip is not byte-identical. In the corpus
this failed on perfectly normal formatting:

| Formatting | Example | Projects |
| --- | --- | --- |
| `/>` without a space | `<InternalsVisibleTo Include="X"/>` | evo2, kgrzybek |
| space before `>` | `<PackageReference Include="StyleCop.Analyzers" >` | amantin, nadirbad |
| `>` in an attribute | `Include="@(DistFiles->'%(FullPath)')"` | jason (`Web.csproj`) |
| attributes over multiple lines | `<PackageReference`⏎`Include=...` | evo1 |
| mixed CRLF/LF | test csproj with two LF lines | amantin |

Moreover, the nearest `Directory.Build.props` is always loaded, even if nothing in it needs to
change (no MediatR in it): the rejection then affects all projects below it. Every rejection is a Manual,
so via all-or-nothing 0 changes.

Measured: with these files normalized to be semantically equal (`scratchpad/scripts/norm.py`) this
Manual disappears everywhere; evo1 and evo2 are then fully rewritten (and run into TAAK-130/131/135).

## What, where, how

- **Where**: `src/Unshackle.DataAccess/Projects/ProjectFile.cs`, `XmlProjectFileStore.cs`.
- **How** (smallest first): (1) only load/validate a file when something in it actually changes;
  (2) textual splice: only rewrite the changed elements (positions via `IXmlLineInfo`), leave the rest
  of the text untouched. Then `/>`, `->`, multi-line attributes and mixed line endings are irrelevant.
- No proof (DataAccess): unit tests in `src/Unshackle.Tests/DataAccess/` per row of the table.

## Steps

- [ ] Red tests per formatting form + "Directory.Build.props without MediatR stays untouched"
- [ ] Lazy loading
- [ ] Splice writing
- [ ] `dotnet build` + `dotnet test` green

## Outcome (2026-10-11)

**Status**: done. `ProjectFile` walks the original text alongside the `XDocument` and only writes changed nodes
(attribute values in place, new elements inserted, removed ones cut out); the reader does not normalize line endings.
Lazy loading turned out to be unnecessary: an unchanged file produces no change. Tests per formatting form (BOM, `/>`, `" >`,
`->`, multi-line attributes, mixed CRLF/LF, comments, `'` quotes) + unchanged `Directory.Build.props`.
The old test "closing tag in trailing comment throws" is now "stays correct" (the scanner no longer cuts at `</Project>`).

Evidence: evo1 (8 Fixed / 0 Manual) and evo2 (25 / 0) get through all-or-nothing; for Polly and Ardalis.Result see TAAK-111.

## See also

- `claude/tasks/CORPUSRUN-mediatr.md`
