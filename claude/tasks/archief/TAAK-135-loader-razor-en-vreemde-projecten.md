# TAAK-135 — Loading: Razor components, `.dcproj` and non-loading bystander projects block the whole solution

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/CORPUSRUN-mediatr.md` (eShopOnWeb, dotnet/eShop, kgrzybek)

## Problem

`MsBuildSolutionStore.LoadAsync` rejects the solution on:

1. **CS0246/CS0234 on Razor-generated types**: eShopOnWeb `BlazorAdmin` (`Edit`, `Delete`, `Details` are
   `.razor` components), dotnet/eShop `WebApp` (`eShop.WebApp.Components`). Builds fine with `dotnet build`;
   in the workspace the Razor generator apparently does not run. Even after restore: "Types do not resolve".
2. **Non-C# projects**: `docker-compose.dcproj` ("extension not associated with a language").
3. **One bystander project that does not load**: kgrzybek `_build` (Nuke) and `Database.Build` (MSBuild.Sdk.SqlProj) — they
   do not use MediatR but hold up the whole migration.

As a result, two of the best-known MediatR samples (eShopOnWeb, eShop) never load.

## What, where, how

- **Where**: `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs` (`IsTolerable`, the CS0246 check).
- **How**: (1) CS0246 check only on projects that reference the fix's package, or ignore diagnostics in
  `*.razor`/generated files and unknown names that exist as a `.razor` file in the project;
  (2) tolerate projects without a language (`.dcproj`, `.sqlproj`) as a warning; (3) tolerate a non-loading project that
  does not reference the package to be removed. Keep a warning in the report.
- No proof: unit tests in `src/Unshackle.Tests/DataAccess/`.

## Steps

- [x] Red tests (razor component, dcproj, failing project without the package)
- [x] Tolerance
- [x] `dotnet build` + `dotnet test` green

## Result

`src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs`:

- `RazorNames`: CS0246/CS0234 on a name that is a `.razor` file or a folder with `.razor` files in the project folder
  does not count as "not restored" (the workspace does not run the Razor generator).
- `IsTolerable` + `SharesNoPackage`: an existing project file that does not load (`.dcproj`, `.sqlproj`, missing SDK) is
  skipped with a warning if it shares no `PackageReference` with the loaded projects; if it shares one, it stays
  fatal (TAAK-090: a shared package file may then change). Only rooted paths count; previously `.dcproj` was
  accidentally tolerated because the fragment `'.dcproj'` "did not exist".
- Unrestored bystander projects without dependents: already solved in TAAK-112 (reused, kgrzybek `_build`).
- Tests: `SampleShopTests.Load_RazorComponentNames_DoNotFailTheLoad`,
  `Load_UnloadableBystanderProject_IsSkippedUnlessItSharesAPackage` (2 cases).
- Corpus (fresh copies `scratchpad/v131/`): eShopOnWeb loads, 9 Fixed / 0 Manual, **build green**. dotnet/eShop (MAUI projects
  removed from the copy solution, as in the corpus run): loads, 36 Manual (all-or-nothing), build fails only on ASPIRE009 like the baseline.
  kgrzybek loads (258 Manual), build fails only on MSBuild.Sdk.SqlProj (environment).


## See also

- `claude/tasks/CORPUSRUN-mediatr.md`
