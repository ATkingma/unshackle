using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-309 (queue row 10): <c>range.LoadFromCollection(items);</c> (no headers) as a statement becomes <c>range.InsertData(items);</c>
/// (plain cells), only when every public property of the element type has a type the harness covers (council round 8, E4). Headers,
/// tables, <c>LoadFromDataTable</c> (column types are only known at run time), <c>LoadFromArrays</c>/<c>LoadFromText</c> and a used result
/// are Manual. The range is left to row 4.
/// </summary>
public class LoadFromPattern : IPattern
{
    public string Id => "epplus.load-from";

    public string Description => "range.LoadFromCollection(items); → range.InsertData(items); (no headers, covered property types only)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => LoadFromMapping.Method(i, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var invocation = (InvocationExpressionSyntax)node;
        IMethodSymbol method = LoadFromMapping.Method(invocation, model)!;
        var access = (MemberAccessExpressionSyntax)invocation.Expression;
        if (method.Name != "LoadFromCollection")
        {
            return Manual(method.Name == "LoadFromDataTable" ? EPPlusReason.UncoveredRuntimeType : EPPlusReason.UnmappedLoadFrom, method.Name, method.Name == "LoadFromDataTable"
                ? "the column types of a DataTable are only known at run time, so the harness cannot prove the values (E4)"
                : "this load method is not mapped yet");
        }

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count > 2 || (arguments.Count == 2 && !arguments[1].Expression.IsKind(SyntaxKind.FalseLiteralExpression)))
        {
            return Manual(EPPlusReason.LoadFromHeadersNotMapped, method.Name, "headers or a table are asked for; ClosedXML's InsertTable adds a table and InsertData writes no header row");
        }

        if (invocation.Parent is not ExpressionStatementSyntax)
        {
            return Manual(EPPlusReason.UnmappedLoadFrom, method.Name, "its result (the filled range) is used, and ClosedXML returns a different range type");
        }

        if (LoadFromMapping.DifferingMember(method.TypeArguments[0]) is string differing)
        {
            return Manual(EPPlusReason.LoadFromTypeDiffers, method.Name, $"{differing}: the harness shows ClosedXML writes this type with another value or number format (council round 12, E15)");
        }

        if (LoadFromMapping.UncoveredMember(method.TypeArguments[0]) is string uncovered)
        {
            return Manual(EPPlusReason.UncoveredRuntimeType, method.Name, $"{uncovered} has a type the harness does not cover (E4)");
        }

        return PatternResult.Fixed(SyntaxFactory.ParseExpression($"{access.Expression.WithoutTrivia()}.InsertData({arguments[0].Expression.WithoutTrivia()})"));
    }

    private static PatternResult Manual(EPPlusReason code, string method, string why) =>
        PatternResult.Manual(code, $"EPPlus '{method}': {why} (queue row 10). Migrate it by hand.");
}
