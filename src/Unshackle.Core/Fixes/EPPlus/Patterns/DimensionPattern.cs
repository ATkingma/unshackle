using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-309 (queue row 7): <c>sheet.Dimension.End.Row</c> / <c>.End.Column</c> / <c>.Start.Row</c> / <c>.Start.Column</c> become
/// <c>sheet.LastRowUsed().RowNumber()</c> / <c>LastColumnUsed().ColumnNumber()</c> / <c>FirstRowUsed()</c> / <c>FirstColumnUsed()</c>.
/// The null behaviour of an empty sheet is kept: <c>Dimension.</c> (throws on an empty sheet) stays <c>.</c>, <c>Dimension?.</c> becomes
/// <c>?.</c> (null on an empty sheet). Any other use of <c>Dimension</c> is Manual <c>UnmappedDimension</c>. The sheet is left to row 3.
/// </summary>
public class DimensionPattern : IPattern
{
    public const string UnmappedCode = "UnmappedDimension";

    public string Id => "epplus.dimension";

    public string Description => "sheet.Dimension.End.Row / Start.Column ... → sheet.LastRowUsed().RowNumber() / FirstColumnUsed().ColumnNumber() (null kept)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Where(d => d.Name.Identifier.ValueText == "Dimension" && DimensionMapping.IsDimension(d, model))
            .Select(DimensionMapping.Outer)
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        DimensionMapping.Rewrite(node) is string text
            ? PatternResult.Fixed(SyntaxFactory.ParseExpression(text))
            : PatternResult.Manual($"[{UnmappedCode}] EPPlus 'Dimension' used as '{Shorten(node)}': only .End/.Start .Row/.Column are mapped "
                + "(queue row 7). Migrate it by hand (LastCellUsed/RangeUsed in ClosedXML; Dimension is null on an empty sheet).");

    private static string Shorten(SyntaxNode node)
    {
        string text = node.WithoutTrivia().ToString().ReplaceLineEndings(" ");
        return text.Length > 60 ? text[..60] + "..." : text;
    }
}
