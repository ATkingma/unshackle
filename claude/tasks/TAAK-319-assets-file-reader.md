# TAAK-319 — Engine: package assets per project (`obj/project.assets.json`)

**Status**: step 3/3 — done (engine-2); Cli.cs line listed for the main session
**Created**: 2026-10-22
**Goal**: A Core interface for the restored package assets of a project (present, current against the project file, resolved versions and requirements), with a DataAccess reader of `obj/project.assets.json`, so EPPlus E7 can decide whether to pin SixLabors.Fonts below 2.0.

## Context for the next agent

- Spec: `claude/docs/council.md`, round 10, E7. Pin through the project-file step only when the assets file is present, is current, and shows no requirement of 2.0 or higher. Missing or stale assets: a Warning only. A project that already resolves 2.x: Manual `LicensedDependency`.
- Engine claim: Core root, Abstractions, Models, DataAccess. EPPlus code (`Fixes/EPPlus`) belongs to the EPPlus agent; it only consumes the interface.

## Steps

- [x] 1. Models `PackageAssets` and `AssetsState`; interface `IPackageAssetsReader` in Abstractions
- [x] 2. DataAccess `JsonPackageAssetsReader`: reads `obj/project.assets.json`; stale when it is for another project, older than the project file or its nearest `Directory.Build.props` / `Directory.Packages.props`, or misses a declared `PackageReference`
- [x] 3. Tests (`src/Unshackle.Tests/DataAccess/Projects/PackageAssetsReaderTests.cs`), including the E7 cases; `core.md` section

## How EPPlus E7 uses it

```csharp
PackageAssets assets = reader.Read(project.FilePath!);
if (assets.ResolvesAtLeast("SixLabors.Fonts", new Version(2, 0))) → Manual ProjectBlockingReason.LicensedDependency (ProjectBlockers)
else if (assets.State == AssetsState.Current && !assets.RequiresAtLeast("SixLabors.Fonts", new Version(2, 0))) → pin edit (EditsFor)
else → Warning only
```

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core/Abstractions/IPackageAssetsReader.cs`, `src/Unshackle.Core/Models/PackageAssets.cs`, `AssetsState.cs`, `src/Unshackle.DataAccess/Projects/JsonPackageAssetsReader.cs` |
| **How**   | Engine extension (read-only I/O behind a Core interface) |
| **Why**   | E7 must not pin a package below a version that another package needs |

## See also

- `claude/docs/council.md` — round 10, E7
- `claude/tasks/TAAK-318-engine-round-10.md` — engine round 10
- `docs/projectinfo/architectuur/core.md` — engine rules
- `claude/docs/INDEX.md` — documentation index
