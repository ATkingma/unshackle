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
    // The default target (council 18 X2): 3.2.x runs on Microsoft Testing Platform v1, so VSTest CI keeps working through
    // xunit.runner.visualstudio 3.x; 4.x (MTP v2) is a later opt-in. The proofs compile every rewrite against it (XunitV3References).
    public const string V3Version = "3.2.2";

    // X3: the runner that runs xunit.v3 under dotnet test/VSTest; a 2.x (or older 3.x) reference is raised to it.
    public const string RunnerVersion = "3.1.5";

    public const string RunnerFromVersion = "2.0.0";

    public string Id => "xunit3";

    // v3 keeps Xunit; the patterns swap Xunit.Abstractions themselves (the model still binds it to v2 while fixing).
    public IReadOnlyList<string> RemovedNamespaces { get; } = [];

    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    public IReadOnlyList<string> OldLibraryAssemblies { get; } = XunitV3Assemblies.V2;

    // Half a migration does not build: a rewrite (ValueTask, the Xunit using) needs v3, a Manual row leaves code that needs v2.
    // Conservative until the council decides on per-project partial runs (TAAK-341, council questions).
    public bool AllOrNothing => true;

    public Task<IReadOnlyList<ManualResult>> ProjectBlockersAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return XunitV3TargetFramework.BlockersAsync(project, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectFileEdit>> EditsForAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!XunitV3Assemblies.ReferencesV2(project))
        {
            return [];
        }

        // A rename to a package the project already declares removes the old reference (xunit and xunit.core both become xunit.v3).
        // Council round 18, X4: only a project with Microsoft.NET.Test.Sdk AND test attributes becomes an executable on xunit.v3. Any other
        // project with the xunit package keeps its OutputType and gets the library packages (v3 xunit.v3 refuses a non-executable project).
        List<ProjectFileEdit> edits = [];
        if (await XunitV3Assemblies.IsTestProjectAsync(project, cancellationToken))
        {
            edits.Add(new PropertyEdit { Name = "OutputType", Value = "Exe" });
            edits.Add(new PackageRenameEdit { OldName = "xunit", NewName = "xunit.v3", Version = V3Version });
            edits.Add(new PackageRenameEdit { OldName = "xunit.core", NewName = "xunit.v3", Version = V3Version });
            // X3: xunit.v3 needs runner 3.x to run under dotnet test/VSTest.
            // X9: a 1.x runner (pre-netcore era) is not raised automatically; an Info row says to update it by hand.
            edits.Add(new PackageVersionEdit
            {
                Name = "xunit.runner.visualstudio",
                From = RunnerFromVersion,
                To = RunnerVersion,
                BelowFromInfo = $"a 1.x runner cannot run xunit.v3 tests; update it to {RunnerVersion} by hand.",
            });
        }
        else if (XunitV3Assemblies.ReferencesExecution(project))
        {
            edits.Add(new PackageRenameEdit { OldName = "xunit", NewName = "xunit.v3.extensibility.core", Version = V3Version });
            edits.Add(new PackageRenameEdit { OldName = "xunit.core", NewName = "xunit.v3.extensibility.core", Version = V3Version });
            if (XunitV3Assemblies.ReferencesAssert(project))
            {
                edits.Add(new PackageAddEdit { Name = "xunit.v3.assert", Version = V3Version });
            }
        }

        edits.Add(new PackageRenameEdit { OldName = "xunit.assert", NewName = "xunit.v3.assert", Version = V3Version });
        edits.Add(new PackageRenameEdit { OldName = "xunit.extensibility.core", NewName = "xunit.v3.extensibility.core", Version = V3Version });
        edits.Add(new PackageRenameEdit { OldName = "xunit.extensibility.execution", NewName = "xunit.v3.extensibility.core", Version = V3Version });
        edits.Add(new PackageRemoveEdit { Name = "xunit.abstractions" });
        return edits;
    }

    // X4: a project with the xunit package that is not a test project by both signs keeps its OutputType; the row says why.
    public async Task<IReadOnlyList<string>> ProjectInfoAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!XunitV3Assemblies.ReferencesExecution(project) || await XunitV3Assemblies.IsTestProjectAsync(project, cancellationToken))
        {
            return [];
        }

        string sdk = XunitV3Assemblies.ReferencesTestSdk(project) ? "references" : "does not reference";
        string attributes = await XunitV3Assemblies.HasTestAttributesAsync(project, cancellationToken) ? "has" : "has no";
        return [$"Not migrated as a test project: it {sdk} Microsoft.NET.Test.Sdk and {attributes} xUnit test attributes (both are needed). Its xunit package " +
            "becomes xunit.v3.extensibility.core plus xunit.v3.assert and OutputType stays; if it runs tests, set OutputType Exe and reference xunit.v3 by hand."];
    }

    public IReadOnlyList<IPattern> Patterns { get; } =
    [
        new AbstractionsNamespacePattern(), new AsyncLifetimePattern(), new AsyncVoidTestPattern(), new RecordExceptionAsyncPattern(),
        new TypeNameAttributePattern(), new ExtensibilityPattern(),
    ];
}
