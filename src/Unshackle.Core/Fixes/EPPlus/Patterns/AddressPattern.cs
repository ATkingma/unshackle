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
/// <see cref="DimensionPattern"/> leaves its sheet. TAAK-337 (council round 16, E25): the same reads on an <c>ExcelRange</c> local that is
/// declared once from such a <c>Cells[..]</c> range and never written again (<see cref="WorkbookUses"/> turns the local into a ClosedXML
/// range); any other range local is Manual. A range parameter is left to <see cref="FlowTypesPattern"/>; a field is not mapped.
/// </summary>
public class AddressPattern : IPattern
{
    public string Id => "epplus.address";

    public string Description => "ws.Cells[r1, c1, r2, c2].Rows / .Start.Row ... → .RowCount() / .RangeAddress.FirstAddress.RowNumber (ranges in order only)";

    // R3 (council round 19): an address read left Manual in a member that epplus.unsupported reports anyway is not found here; its reason is
    // folded into that one row (as E31), so each node has one row and the summary counts it once.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        List<SyntaxNode> candidates = [.. Candidates(root, model)];
        return candidates.Where(n => Rewrite(n, model) is not ManualResult || !UnsupportedPattern.ReportsBesides(n, candidates, root, model));
    }

    // Every address read this pattern handles, Fixed or Manual.
    internal static IEnumerable<SyntaxNode> Candidates(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Where(a => a.Name.Identifier.ValueText is "Rows" or "Columns" or "Start" or "End"
                && (a.Expression is ElementAccessExpressionSyntax cells && IsCells(cells, model) || IsRangeLocal(a.Expression, model))
                && IsEPPlusMember(a, model))
            .Select(a => a.Name.Identifier.ValueText is "Start" or "End" && a.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Row" or "Column" } part && part.Expression == a ? part : a);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var access = (MemberAccessExpressionSyntax)node;
        (ExpressionSyntax range, string? text) = Mapped(access);

        if (text is null)
        {
            return PatternResult.Manual(EPPlusReason.UnmappedAddress, $"'{Shorten(access)}' ({Where(access)}): only .Rows, .Columns and .Start/.End .Row/.Column of a range "
                + "are mapped (council round 13, E18 step 2). Migrate it by hand (ClosedXML: RangeAddress.FirstAddress / LastAddress).");
        }

        var cells = range as ElementAccessExpressionSyntax;
        if (range is IdentifierNameSyntax local)
        {
            cells = model.GetSymbolInfo(local).Symbol is ILocalSymbol symbol ? LocalSource(symbol, model) : null;
            if (cells is null)
            {
                return PatternResult.Manual(EPPlusReason.UnmappedAddress, $"'{Shorten(access)}' ({Where(access)}) reads the address of range local {local.Identifier.ValueText}, "
                    + "which is not declared once from a worksheet's Cells[..] and never written again (council round 16, E25). Migrate it by hand.");
            }
        }

        if (cells is null || !InOrder(cells.ArgumentList.Arguments, model))
        {
            return PatternResult.Manual(EPPlusReason.UnmappedAddress, $"'{Shorten(access)}' ({Where(access)}) reads the address of a range that is not mapped "
                + "(a single cell, a non-literal address, or a literal address whose second corner is before its first: EPPlus throws on it, ClosedXML "
                + "sorts it; harness, TAAK-327). Migrate it by hand.");
        }

        return PatternResult.Fixed(SyntaxFactory.ParseExpression($"{range.WithoutTrivia()}.{text}").WithTriviaFrom(access), "ClosedXML.Excel");
    }

    // The range expression of an address read (r.Rows, r.Start.Row, ...) and its ClosedXML text, or null text when the read is not mapped.
    internal static (ExpressionSyntax Range, string? Text) Mapped(MemberAccessExpressionSyntax access) => access switch
    {
        { Name.Identifier.ValueText: "Rows", Expression: var c } => (c, "RowCount()"),
        { Name.Identifier.ValueText: "Columns", Expression: var c } => (c, "ColumnCount()"),
        { Name.Identifier.ValueText: var part and ("Row" or "Column"), Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var corner and ("Start" or "End"), Expression: var c } } =>
            (c, $"RangeAddress.{(corner == "Start" ? "First" : "Last")}Address.{part}Number"),
        { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Start" or "End", Expression: var c } } => (c, null),
        _ => (access.Expression, null),
    };

    // E25: the Cells[..] a range local is declared from, when it is the one local of a declaration statement in a block, initialized from a
    // worksheet's Cells[..], and no reference to it writes it (assignment, ref/out argument, ++/--); otherwise null.
    internal static ElementAccessExpressionSyntax? LocalSource(ILocalSymbol local, SemanticModel model)
    {
        if (!EPPlusSyntax.IsRangeType(local.Type)
            || local.DeclaringSyntaxReferences is not [var reference]
            || reference.GetSyntax() is not VariableDeclaratorSyntax { Initializer.Value: ElementAccessExpressionSyntax cells, Parent: VariableDeclarationSyntax { Variables.Count: 1, Parent: LocalDeclarationStatementSyntax { UsingKeyword.RawKind: (int)SyntaxKind.None, Parent: BlockSyntax block } } }
            || !IsCells(cells, model))
        {
            return null;
        }

        bool written = block.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol, local))
            .Any(i => i.Parent is AssignmentExpressionSyntax assignment && assignment.Left == i
                || i.Parent is ArgumentSyntax argument && !argument.RefKindKeyword.IsKind(SyntaxKind.None)
                || i.Parent is PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax);
        return written ? null : cells;
    }

    private static bool IsRangeLocal(ExpressionSyntax receiver, SemanticModel model) =>
        receiver is IdentifierNameSyntax && model.GetSymbolInfo(receiver).Symbol is ILocalSymbol local && EPPlusSyntax.IsRangeType(local.Type);

    private static bool IsCells(ElementAccessExpressionSyntax cells, SemanticModel model) =>
        cells.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Cells" } member
        && model.GetTypeInfo(member.Expression).Type is INamedTypeSymbol { Name: "ExcelWorksheet", ContainingNamespace: { Name: EPPlusSyntax.Namespace, ContainingNamespace.IsGlobalNamespace: true } };

    private static bool IsEPPlusMember(MemberAccessExpressionSyntax access, SemanticModel model) =>
        model.GetSymbolInfo(access).Symbol is IPropertySymbol { ContainingNamespace: { Name: EPPlusSyntax.Namespace, ContainingNamespace.IsGlobalNamespace: true } };

    // Four ints (both libraries sort the corners), or a literal "A1:C2" with both corners in order.
    internal static bool InOrder(SeparatedSyntaxList<ArgumentSyntax> arguments, SemanticModel model) => arguments switch
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
