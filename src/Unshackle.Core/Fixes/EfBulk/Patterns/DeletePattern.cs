using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EfBulk.Patterns;

/// <summary><c>q.Delete()</c> / <c>q.DeleteFromQuery()</c> (+Async) of Z.EntityFramework.Plus/Extensions → <c>q.ExecuteDelete()</c> (+Async).</summary>
public class DeletePattern : IPattern
{
    private static readonly Dictionary<string, string> s_names = new(StringComparer.Ordinal)
    {
        ["Delete"] = "ExecuteDelete",
        ["DeleteFromQuery"] = "ExecuteDelete",
        ["DeleteAsync"] = "ExecuteDeleteAsync",
        ["DeleteFromQueryAsync"] = "ExecuteDeleteAsync",
    };

    public string Id => "efbulk.delete";

    public string Description => "q.Delete()/DeleteFromQuery() (Z.EntityFramework) → q.ExecuteDelete()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => EfBulkCall.OldMethod(i, model, s_names) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var invocation = (InvocationExpressionSyntax)node;
        SimpleNameSyntax name = EfBulkCall.Name(invocation)!;
        string target = s_names[name.Identifier.ValueText];
        if (!EfBulkCall.HasNative(model, "ExecuteDelete"))
        {
            return PatternResult.Manual(EfBulkReason.NoNativeExecute,
                $"'{EfBulkCall.Shown(invocation)}': ExecuteDelete needs EF Core 7+ with a relational provider.");
        }

        bool isAsync = target.EndsWith("Async", StringComparison.Ordinal);
        bool argumentsMap = invocation.ArgumentList.Arguments switch
        {
            [] => true,
            [var token] => isAsync && EfBulkCall.IsCancellationToken(token, model),
            _ => false,
        };
        return argumentsMap
            ? PatternResult.Fixed(EfBulkCall.Renamed(invocation, name, target), EfBulkCall.EfCoreNamespace)
            : PatternResult.Manual(EfBulkReason.BatchOptions,
                $"'{EfBulkCall.Shown(invocation)}': {EfBulkCall.BatchOptions(invocation)} (BatchSize, Executing, ...) has no ExecuteDelete equivalent.");
    }
}
