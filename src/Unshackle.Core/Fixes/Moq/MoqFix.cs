using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.Moq.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq;

public class MoqFix : IFix, IProjectFileMigration
{
    // The version our proofs compile every rewrite against (Directory.Packages.props): BSD-3, no license or sponsor check.
    public const string NSubstituteVersion = "6.2.0";

    public MoqFix()
    {
        AddedReferences = [];
    }

    // The NSubstitute assembly comes from the composition root: Core does no I/O.
    public MoqFix(IReadOnlyList<MetadataReference> nsubstitute)
    {
        ArgumentNullException.ThrowIfNull(nsubstitute);
        AddedReferences = nsubstitute;
    }

    public string Id => "moq";

    public IReadOnlyList<MetadataReference> AddedReferences { get; }

    public IReadOnlyList<string> RemovedNamespaces { get; } = ["Moq", "Moq.Language", "Moq.Language.Flow", "Moq.Protected"];

    public IReadOnlyList<string> AddedNamespaces { get; } = ["NSubstitute"];

    public PackageMigration Package { get; } =
        new() { RemovedPackage = "Moq", AddedPackages = [new() { Name = "NSubstitute", Version = NSubstituteVersion }] };

    // Later rows of claude/tasks/moq-queue.md (setups, verifies) go before CreatePattern: it only converts a mock used as .Object.
    // Council round 9, Q3 / round 10, G4: a project whose target cannot use NSubstitute gets a blocking project row.
    public Task<IReadOnlyList<ManualResult>> ProjectBlockersAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Task.FromResult(Blockers(project));
    }

    private static IReadOnlyList<ManualResult> Blockers(Project project)
    {
        return project.ParseOptions is { } options && MoqTargetFramework.Unsupported(options.PreprocessorSymbolNames) is { } framework
            ? [(ManualResult)PatternResult.Manual(ProjectBlockingReason.TargetFrameworkUnsupported, MoqTargetFramework.Reason(framework))]
            : [];
    }

    // Council round 14, M13 (corpus B1): an accepted limitation, stated once per run that changed code.
    public const string ProxyInfo =
        "Mock objects are now NSubstitute proxies; tests that assert a mock's GetType() name or ToString() will see a different value.";

    public string? ChangedCodeInfo => ProxyInfo;

    // No project-file edits besides the package swap (Package).
    public Task<IReadOnlyList<ProjectFileEdit>> EditsForAsync(Project project, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ProjectFileEdit>>([]);

    public IReadOnlyList<IPattern> Patterns { get; } = [new VerifyAllPattern(), new SetupReturnsPattern(), new ReturnsLambdaPattern(), new ThrowsPattern(), new SetupSequencePattern(), new CallbackPattern(), new LoggerVerifyPattern(), new VerifyPattern(), new PropertiesPattern(), new ClassMocksPattern(), new UnsupportedPattern(), new CreatePattern()];
}
