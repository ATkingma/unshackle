using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EfBulk.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EfBulk;

/// <summary>
/// Batch delete/update calls of Z.EntityFramework.Plus / Z.EntityFramework.Extensions on EF Core queries → EF Core 7+ native
/// <c>ExecuteDelete</c>/<c>ExecuteUpdate</c> (TAAK-342, scout report <c>docs/projectinfo/architectuur/candidate-efbulk.md</c>).
/// First slice: no package is touched, since Bulk*, Future and FromCache usually keep the Z package (TAAK-342 council question 5).
/// </summary>
public class EfBulkFix : IFix
{
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

    public IReadOnlyList<IPattern> Patterns { get; } = [new DeletePattern(), new UpdatePattern()];
}
