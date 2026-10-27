using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EfBulk.Patterns;

/// <summary>
/// Every other call or constructor of Z.EntityFramework.Plus / Z.EntityFramework.Extensions (<c>Bulk*</c>, <c>Future</c>, <c>FromCache</c>,
/// <c>IncludeFilter</c>, <c>new Audit()</c>, ...) as a Manual row (TAAK-342 step 8): EF Core has no native twin, and each one keeps the Z package.
/// </summary>
public class UnsupportedPattern : IPattern
{
    // Handled by DeletePattern and UpdatePattern.
    private static readonly HashSet<string> s_batchNames = new(StringComparer.Ordinal)
    {
        "Delete", "DeleteAsync", "DeleteFromQuery", "DeleteFromQueryAsync",
        "Update", "UpdateAsync", "UpdateFromQuery", "UpdateFromQueryAsync",
    };

    private static readonly HashSet<string> s_assemblies = new([EfBulkCall.PlusAssembly, EfBulkCall.ExtensionsAssembly], StringComparer.OrdinalIgnoreCase);

    public string Id => "efbulk.unsupported";

    public string Description => "Bulk*/Future/FromCache/IncludeFilter/Audit (Z.EntityFramework) → Manual, keeps the Z package";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax && Unsupported(n, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        IMethodSymbol method = Unsupported(node, model)!;
        string name = method.MethodKind == MethodKind.Constructor ? $"new {method.ContainingType.Name}" : method.Name;
        return PatternResult.Manual(EfBulkReason.Unsupported, $"'{EfBulkCall.Shown(node)}': {Family(method)} Z.EntityFramework stays in this project while {name} is used.");
    }

    private static IMethodSymbol? Unsupported(SyntaxNode node, SemanticModel model) =>
        model.GetSymbolInfo(node).Symbol is IMethodSymbol method
        && method.ContainingAssembly is { } assembly && s_assemblies.Contains(assembly.Name)
        && !(method.IsExtensionMethod && s_batchNames.Contains(method.Name))
            ? method
            : null;

    private static string Family(IMethodSymbol method)
    {
        string name = method.MethodKind == MethodKind.Constructor ? method.ContainingType.Name : method.Name;
        if (name.Contains("Bulk", StringComparison.Ordinal))
        {
            // Council round 17, B8: one neutral line on the MIT fork (MIT on NuGet, 10.22.0 of 2025-12-09 targets EF Core 10; checked 2026-10-27).
            return "EF Core has no bulk insert/update/merge/synchronize (AddRange + SaveChanges is not equivalent in speed); not migrated. " +
                "An alternative, not migrated automatically, is EFCore.BulkExtensions.MIT.";
        }

        if (name.StartsWith("Future", StringComparison.Ordinal) || name.StartsWith("Deferred", StringComparison.Ordinal))
        {
            return "EF Core has no query batching (Future/Deferred); not migrated.";
        }

        if (name.Contains("Cache", StringComparison.Ordinal))
        {
            return "EF Core has no second-level query cache (FromCache); not migrated.";
        }

        if (name.StartsWith("Include", StringComparison.Ordinal))
        {
            return "IncludeFilter/IncludeOptimized: EF Core 5+ filtered Include is the native twin, with different load semantics; not migrated.";
        }

        if (name.Contains("Audit", StringComparison.Ordinal) || method.ContainingType.Name.Contains("Audit", StringComparison.Ordinal)
            || method.Parameters.Any(p => p.Type.Name.Contains("Audit", StringComparison.Ordinal)))
        {
            return "EF Core has no audit trail (Audit); a SaveChangesInterceptor is the usual replacement; not migrated.";
        }

        return "No EF Core twin is known; not migrated.";
    }
}
