# Models: mediatr.vendored-package

Facts from `src/Unshackle.Core/VendoredPackages.cs`, `VendoredPlan.cs` and `Fixes/MediatR/ArdalisSharedKernel.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `IFix.VendoredPackages` | Packages the fix vendors; empty by default, `MediatRFix` gives `[ArdalisSharedKernel.Package]` |
| `VendoredPackage` / `VendoredVersion` / `VendoredFile` | Name, folder, header, extra files (licence); per version the files and dependencies |
| `ArdalisSharedKernel` | Reads the embedded source code (`Vendored/ArdalisSharedKernel/common` + family `1.4.0`, `1.5.0`, `2.1.0`) and the nuspec dependencies |
| `VendoredPackages` | Chooses version and root project, creates documents, new files, migrations and rows (`vendored-package`) |
| `VendoredPlan` | Outcome; `AddTo` puts the documents into the resolved solution |
| `ProjectFileChange.Creates` | New file; `XmlProjectFileStore.SaveAsync` checks that it does not exist and rolls back |
| `UnsupportedPattern.IsVendored` | No Manual for a supported version |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Patterns | `Fixer.FixAsync` | User code is migrated; `UnsupportedPattern` skips Ardalis.SharedKernel |
| Plan | `VendoredPackages.PlanAsync` | Users = projects with the assembly; version from `AssemblyIdentity`; root via `ProjectDependencyGraph` |
| Source | `PlanAsync` | Header + resource, line ending of the project; a document per target of the root project |
| Dependencies | `Resolved` | Version from the NuGet path of the reference, otherwise the nuspec |
| Project files | `Fixer.MigrateProjectFilesAsync` | MediatR migrations and vendor migrations in one `IProjectFileStore.EditAsync` |
| Write | `FixCommand` → `SaveAsync` | New files with the rest, in one check up front |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Fixed (source + swap) | supported version, one root, folder free | `_Combined/Vendored/shared-kernel`, `FixAsync_UsersShareOneProject_*` |
| Manual "no single project for its source" | users without a common project | `FixAsync_UsersShareNoProject_ReportsManual` |
| Manual "no source of this version" | 1.0.0–1.3.0 (also `UnsupportedPattern`) | code review (`PlanAsync`) |
| Manual "already has source files" | the folder exists in the solution | code review (`PlanAsync`) |
| Write error "already exists" | the file is already on disk | `SaveAsync_NewFileAlreadyThere_*` |

## See also

- [mediatr-vendored-package-plan.md](mediatr-vendored-package-plan.md)
- [mediatr-vendored-package-verify.md](mediatr-vendored-package-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/_Combined/Vendored/`
