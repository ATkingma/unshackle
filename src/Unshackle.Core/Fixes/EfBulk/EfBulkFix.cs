using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EfBulk.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EfBulk;

/// <summary>
/// Batch delete/update calls of Z.EntityFramework.Plus / Z.EntityFramework.Extensions on EF Core queries → EF Core 7+ native
/// <c>ExecuteDelete</c>/<c>ExecuteUpdate</c> (TAAK-342, scout report <c>docs/projectinfo/architectuur/candidate-efbulk.md</c>).
/// Both Z packages leave a project only when no Z usage is left (council round 17, B5); Bulk*, Future, FromCache etc. are Manual rows.
/// </summary>
public class EfBulkFix : IFix
{
    private readonly IProjectProperties? _projectProperties;

    // Without project properties every kept row names both packages (council round 21, B13: the unreadable case).
    public EfBulkFix()
    {
    }

    public EfBulkFix(IProjectProperties projectProperties)
    {
        ArgumentNullException.ThrowIfNull(projectProperties);
        _projectProperties = projectProperties;
    }

    public string Id => "efbulk";

    // Removed only where nothing in the file binds to it anymore; Future/FromCache/IncludeFilter keep it.
    public IReadOnlyList<string> RemovedNamespaces { get; } = ["Z.EntityFramework.Plus", "Z.EntityFramework.Extensions"];

    // Each rewrite names Microsoft.EntityFrameworkCore itself; an unchanged file gets nothing.
    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    // The Extensions methods may live outside a Z namespace, so the pre-filter matches the assemblies.
    public IReadOnlyList<string> OldLibraryAssemblies { get; } = [EfBulkCall.PlusAssembly, EfBulkCall.ExtensionsAssembly];

    // Stateless patterns.
    public bool FixesFilesInParallel => true;

    public string? ChangedCodeInfo =>
        "ExecuteDelete/ExecuteUpdate run directly on the database like the Z batch calls: tracked entities are not updated and SaveChanges interceptors do not run.";

    // Council round 17, B3: one project row "upgrade first" next to the per-call NoNativeExecute rows.
    public IReadOnlyList<PatternResult> ProjectSummary(IReadOnlyList<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        int blocked = findings.Count(f => f.FixId == Id && f.Result is ManualResult { Code: nameof(EfBulkReason.NoNativeExecute) });
        return blocked == 0
            ? []
            : [PatternResult.Manual(EfBulkReason.EfCoreBelow7,
                $"EF Core < 7: upgrade to EF Core 7+ with a relational provider first; {blocked} batch call(s) blocked (ExecuteDelete/ExecuteUpdate are missing).")];
    }

    // Council round 17, B5: both Z packages leave a project only at zero Z usages. The engine keeps them for any bound Z symbol, also for a
    // project that references this one, and lists the remaining uses with file:line. EF Core 7+ is already referenced, so nothing is added.
    public PackageMigration? Package { get; } = new()
    {
        RemovedPackage = EfBulkCall.PlusAssembly,
        AlsoRemovedPackages = [EfBulkCall.ExtensionsAssembly],
        AddedPackages = [],
    };

    // Council round 21, B14: one row per file with every remaining Z use, also next to a Manual row.
    public bool ReportsEveryRemainingUse => true;

    // Council round 21, B13: name the Z package(s) the project file references; unknown: Plus "or" Extensions.
    public string? KeptPackageName(string projectPath)
    {
        string fallback = $"{EfBulkCall.PlusAssembly} or {EfBulkCall.ExtensionsAssembly}";
        IReadOnlyList<string>? references = _projectProperties?.PackageReferences(projectPath);
        if (references is null)
        {
            return fallback;
        }

        List<string> named = [.. new[] { EfBulkCall.PlusAssembly, EfBulkCall.ExtensionsAssembly }.Where(z => references.Contains(z, StringComparer.OrdinalIgnoreCase))];
        return named.Count == 0 ? fallback : string.Join(" and ", named);
    }

    public IReadOnlyList<IPattern> Patterns { get; } = [new DeletePattern(), new UpdatePattern(), new UnsupportedPattern()];
}
