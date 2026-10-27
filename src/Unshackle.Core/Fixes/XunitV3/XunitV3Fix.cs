using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.XunitV3.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3;

// xUnit v2 → xunit.v3 (TAAK-341, scout docs/projectinfo/architectuur/candidate-testfw.md). A version upgrade: v3 keeps the Xunit
// namespace, so the engine's PackageMigration (keyed on "code still binds to the old assembly") does not fit. The package renames
// and OutputType Exe are typed project-file edits instead.
public class XunitV3Fix : IFix, IProjectFileMigration
{
    // The version our proofs compile every rewrite against (the test project's own xunit.v3, Directory.Packages.props).
    public const string V3Version = "4.0.2";

    public string Id => "xunit3";

    // v3 keeps Xunit; the patterns swap Xunit.Abstractions themselves (the model still binds it to v2 while fixing).
    public IReadOnlyList<string> RemovedNamespaces { get; } = [];

    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    public IReadOnlyList<string> OldLibraryAssemblies { get; } = XunitV3Assemblies.V2;

    // Half a migration does not build: a rewrite (ValueTask, the Xunit using) needs v3, a Manual row leaves code that needs v2.
    // Conservative until the council decides on per-project partial runs (TAAK-341, council questions).
    public bool AllOrNothing => true;

    public IReadOnlyList<ManualResult> ProjectBlockers(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return XunitV3TargetFramework.Blockers(project);
    }

    public IReadOnlyList<ProjectFileEdit> EditsFor(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!XunitV3Assemblies.ReferencesV2(project))
        {
            return [];
        }

        // A rename to a package the project already declares removes the old reference (xunit and xunit.core both become xunit.v3).
        List<ProjectFileEdit> edits = XunitV3Assemblies.IsTestProject(project)
            ?
            [
                new PropertyEdit { Name = "OutputType", Value = "Exe" },
                new PackageRenameEdit { OldName = "xunit", NewName = "xunit.v3", Version = V3Version },
                new PackageRenameEdit { OldName = "xunit.core", NewName = "xunit.v3", Version = V3Version },
            ]
            : [];
        edits.Add(new PackageRenameEdit { OldName = "xunit.assert", NewName = "xunit.v3.assert", Version = V3Version });
        edits.Add(new PackageRenameEdit { OldName = "xunit.extensibility.core", NewName = "xunit.v3.extensibility.core", Version = V3Version });
        edits.Add(new PackageRenameEdit { OldName = "xunit.extensibility.execution", NewName = "xunit.v3.extensibility.core", Version = V3Version });
        edits.Add(new PackageRemoveEdit { Name = "xunit.abstractions" });
        return edits;
    }

    // The runner is a build-only package (no assembly to see) and a same-name version bump is not an edit the engine has yet.
    public IReadOnlyList<string> ProjectInfo(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return XunitV3Assemblies.IsTestProject(project)
            ? ["xunit.v3 needs xunit.runner.visualstudio 3.0 or later to run under dotnet test/VSTest; raise it by hand if the project references an older one."]
            : [];
    }

    public IReadOnlyList<IPattern> Patterns { get; } = [new AbstractionsNamespacePattern(), new AsyncLifetimePattern(), new AsyncVoidTestPattern()];
}
