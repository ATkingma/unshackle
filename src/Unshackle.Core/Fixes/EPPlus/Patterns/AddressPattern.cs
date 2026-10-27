using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-327 (council round 13, E18 step 2): the address of a range taken straight from a worksheet's <c>Cells</c>.
/// <c>ws.Cells[..].Rows</c> / <c>.Columns</c> become <c>.RowCount()</c> / <c>.ColumnCount()</c>, and <c>.Start.Row</c> / <c>.Start.Column</c> /
/// <c>.End.Row</c> / <c>.End.Column</c> become <c>.RangeAddress.FirstAddress.RowNumber</c> / ... <c>.LastAddress.ColumnNumber</c>. Only a range
/// of four int expressions, or a literal
/// "A1:C2" address in order. Harness (TAAK-327): with four ints both libraries sort the corners (<c>Cells[5, 4, 2, 3]</c> is rows 2-5 in
/// both); a reversed literal address ("C5:A2") throws in EPPlus and not in ClosedXML, so it is Manual (<c>UnmappedAddress</c>), as is a single cell. The <c>Cells</c> receiver is left to <see cref="PackagePattern"/> (it becomes <c>ws.Range(..)</c>), like
/// <see cref="DimensionPattern"/> leaves its sheet. A range in a local, a field or a parameter is not mapped here.
/// </summary>
public class AddressPattern : IPattern
{
    public string Id => "epplus.address";

    public string Description => "ws.Cells[r1, c1, r2, c2].Rows / .Start.Row ... → .RowCount() / .RangeAddress.FirstAddress.RowNumber (ranges in order only)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Where(a => a.Name.Identifier.ValueText is "Rows" or "Columns" or "Start" or "End" && a.Expression is ElementAccessExpressionSyntax cells && IsCells(cells, model)
                && IsEPPlusMember(a, model))
            .Select(a => a.Name.Identifier.ValueText is "Start" or "End" && a.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Row" or "Column" } part && part.Expression == a ? part : a);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var access = (MemberAccessExpressionSyntax)node;
        (ElementAccessExpressionSyntax cells, string? text) = access switch
        {
            { Name.Identifier.ValueText: "Rows", Expression: ElementAccessExpressionSyntax c } => (c, "RowCount()"),
            { Name.Identifier.ValueText: "Columns", Expression: ElementAccessExpressionSyntax c } => (c, "ColumnCount()"),
            { Name.Identifier.ValueText: var part and ("Row" or "Column"), Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var corner, Expression: ElementAccessExpressionSyntax c } } =>
                (c, $"RangeAddress.{(corner == "Start" ? "First" : "Last")}Address.{part}Number"),
            { Expression: MemberAccessExpressionSyntax { Expression: ElementAccessExpressionSyntax c } } => (c, null),
            _ => ((ElementAccessExpressionSyntax)access.Expression, null),
        };

        if (text is null)
        {
            return PatternResult.Manual(EPPlusReason.UnmappedAddress, $"'{Shorten(access)}' ({Where(access)}): only .Rows, .Columns and .Start/.End .Row/.Column of a range "
                + "are mapped (council round 13, E18 step 2). Migrate it by hand (ClosedXML: RangeAddress.FirstAddress / LastAddress).");
        }

        if (!InOrder(cells.ArgumentList.Arguments, model))
        {
            return PatternResult.Manual(EPPlusReason.UnmappedAddress, $"'{Shorten(access)}' ({Where(access)}) reads the address of a range that is not mapped "
                + "(a single cell, a non-literal address, or a literal address whose second corner is before its first: EPPlus throws on it, ClosedXML "
                + "sorts it; harness, TAAK-327). Migrate it by hand.");
        }

        return PatternResult.Fixed(SyntaxFactory.ParseExpression($"{cells.WithoutTrivia()}.{text}").WithTriviaFrom(access), "ClosedXML.Excel");
    }

    private static bool IsCells(ElementAccessExpressionSyntax cells, SemanticModel model) =>
        cells.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Cells" } member
        && model.GetTypeInfo(member.Expression).Type is INamedTypeSymbol { Name: "ExcelWorksheet", ContainingNamespace: { Name: EPPlusSyntax.Namespace, ContainingNamespace.IsGlobalNamespace: true } };

    private static bool IsEPPlusMember(MemberAccessExpressionSyntax access, SemanticModel model) =>
        model.GetSymbolInfo(access).Symbol is IPropertySymbol { ContainingNamespace: { Name: EPPlusSyntax.Namespace, ContainingNamespace.IsGlobalNamespace: true } };

    // Four ints (both libraries sort the corners), or a literal "A1:C2" with both corners in order.
    private static bool InOrder(SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel model) => arguments switch
    {
        [_, _, _, _] => arguments.All(a => model.GetTypeInfo(a.Expression).Type?.SpecialType == SpecialType.System_Int32),
        [{ Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } address }] => LiteralInOrder(address.Token.ValueText),
        _ => false,
    };

    private static bool LiteralInOrder(string address)
    {
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(address, "^([A-Z]{1,3})([1-9][0-9]{0,6}):([A-Z]{1,3})([1-9][0-9]{0,6})$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        return match.Success && Column(match.Groups[1].Value) <= Column(match.Groups[3].Value)
            && int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) <= int.Parse(match.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int Column(string letters) => letters.Aggregate(0, (n, c) => (n * 26) + (c - 'A' + 1));

    private static string Where(SyntaxNode node) => $"line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";

    private static string Shorten(SyntaxNode node)
    {
        string text = node.WithoutTrivia().ToString().ReplaceLineEndings(" ");
        return text.Length <= 60 ? text : text[..57] + "...";
    }
}
