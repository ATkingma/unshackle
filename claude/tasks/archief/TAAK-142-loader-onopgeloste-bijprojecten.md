# TAAK-142 — Loading: skip projects with unresolved types if they use no migrated library

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/REGRESSIE-corpus.md` (Akka.NET 302, Steeltoe 502 errors)

## Problem

`MsBuildSolutionStore.LoadAsync` refuses the whole solution as soon as one project gives CS0246/CS0234. In Akka.NET and Steeltoe
those were AOT apps, benchmarks and samples (`Akka.Hosting.AOT.App`, `Akka.Benchmarks`, `RemotePingPong`,
`Steeltoe.Management.GitProperties.Build`, `ConfigurationSchemaGenerator`): they use no FluentAssertions or MediatR,
but held back the whole migration. TAAK-112 only dropped projects without `System`, TAAK-135 only projects that
do not load at all.

## What, where, how

- **Where**: `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs` (the CS0246 check).
- **How**: a project with unresolved names is left out of the solution with a warning if (1) no project depends on it,
  (2) it uses no migrated library (no assembly reference and no unresolved name that starts with a namespace of a
  fix) and (3) at least one other project does resolve. Otherwise it stays fatal (a project that uses FA but does not
  resolve would be missed; a completely unrestored solution still gives the restore advice).
- Which libraries: `IFix.RemovedNamespaces` of all fixes, passed in the composition root (`Cli.cs`) to the constructor.
  `ISolutionStore` stays unchanged.

## Steps

- [x] Red test: `SampleShopTests.Load_UnresolvedLeafProject_IsSkippedUnlessItUsesAMigratedLibrary` (2 cases)
- [x] Tolerance + constructor parameter
- [x] `dotnet build`, `dotnet test` (695), sample tests (38) green
- [x] Corpus: `claude/tools/corpus-regression.sh` on akka and steeltoe

## Result

**The original 302/502 errors were mostly environment, not the loader.** Checking `out/akka/work` showed that
`~/.nuget/packages` had been thinned out between restore and fix (among others `akka.analyzers/0.3.3`, `newtonsoft.json/13.0.1` and the net8 targeting
packs gone): because of that even `Akka` (Newtonsoft) and Steeltoe (`Attribute`, `InternalsVisibleTo`) did not resolve.

Fresh run (`OUT=<corpus>/out142`, sources `src142/` = the old working copies without bin/obj, fix
`fluentassertions`, tool from this branch):

| Solution | loading                                                                            | fix                                     | build after fix     |
| -------- | ---------------------------------------------------------------------------------- | --------------------------------------- | ------------------- |
| Akka.NET | ok (only warnings: SourceLink "Unable to locate repository", 2× `.fsproj`)         | 8453 Fixed / 1899 Manual, 742 files     | fails: 4962× CS0121 |
| Steeltoe | ok, no warnings                                                                    | 5043 Fixed / 755 Manual, 298 files      | fails: 58× CS1061   |

The new tolerance skipped no project in these runs (no "uses no migrated library" warning): with an intact
NuGet cache all projects resolve. It stays useful for solutions with really broken side projects, but was not
demonstrated on the corpus.

New product findings (follow-up tasks, not in this task):

- **Akka**: `Akka.TestKit.AkkaSpecExtensions.ShouldBe<T>` (own extension, same name) makes every `ShouldBe` after the fix
  ambiguous with Shouldly (CS0121).
- **Steeltoe**: `fa.contain-key` rewrites `ContainKey` on `HttpResponseHeaders` and `IReadOnlyDictionary<,>` to
  `ShouldContainKey`, which Shouldly only has for `IDictionary<,>` (CS1061).

## See also

- `claude/tasks/archief/TAAK-112-bogus-load-builder-project.md`
- `claude/tasks/archief/TAAK-135-loader-razor-en-vreemde-projecten.md`
- `claude/tasks/REGRESSIE-corpus.md`
