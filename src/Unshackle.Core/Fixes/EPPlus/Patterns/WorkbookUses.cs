using System.Buffers;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// The uses of one ExcelPackage local that PackagePattern maps (queue rows 2 and 3), collected as replacements for its scope. Worksheets are
// only known from the Worksheets.Add("literal") statements of the scope itself (the package starts empty), so a lookup by name or index is
// Fixed only when it provably finds one of them; E3 decides the index base from the EPPlus version the project references.
internal class WorkbookUses
{
    private static readonly SearchValues<char> s_invalidSheetNameCharacters = SearchValues.Create(@"[]:*?/\");

    private readonly SemanticModel _model;
    private readonly SyntaxNode _scope;
    private readonly ILocalSymbol _package;
    private readonly List<ILocalSymbol> _worksheets = [];
    private Models.PatternResult? _unknownVersion;

    public WorkbookUses(SemanticModel model, SyntaxNode scope, ILocalSymbol package)
    {
        _model = model;
        _scope = scope;
        _package = package;
    }

    public Dictionary<SyntaxNode, SyntaxNode> Replacements { get; } = [];

    // Null when every use is mapped; otherwise the Manual result that says why not.
    public Models.PatternResult? Map()
    {
        foreach (IdentifierNameSyntax use in UsesOf(_package))
        {
            if (!MapPackageUse(use))
            {
                return _unknownVersion ?? Models.PatternResult.Manual($"ExcelPackage local {_package.Name} is used as '{Shorten(use.Parent ?? use)}' (line {Line(use)}), which is not mapped yet (epplus-queue).");
            }
        }

        // A worksheet local found above: its Name and its single cells (rows 4-5).
        for (int i = 0; i < _worksheets.Count; i++)
        {
            foreach (IdentifierNameSyntax use in UsesOf(_worksheets[i]))
            {
                if (!IsNameAccess(use) && !MapCellUse(use))
                {
                    return _unknownVersion ?? Models.PatternResult.Manual($"Worksheet local {_worksheets[i].Name} is used as '{Shorten(use.Parent ?? use)}' (line {Line(use)}), which is not mapped yet (epplus-queue row 4+).");
                }
            }
        }

        return null;
    }

    // ws.Cells[r, c] / ws.Cells["A1"] → ws.Cell(r, c) / ws.Cell("A1"), as the target of a covered Value assignment (row 5, E4) or as the
    // receiver of a style statement StylesPattern already migrated (row 8).
    private bool MapCellUse(IdentifierNameSyntax use)
    {
        if (use.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Cells" } cells || cells.Expression != use
            || cells.Parent is not ElementAccessExpressionSyntax { ArgumentList.Arguments: var arguments } cell || !IsSingleCell(arguments))
        {
            return false;
        }

        StatementSyntax? statement = cell.FirstAncestorOrSelf<StatementSyntax>();
        bool styled = statement is not null && statement.HasAnnotations(StylesPattern.MigratedAnnotationKind)
            && cell.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Style" };
        bool value = cell.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Value" } access
            && access.Parent is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Parent: ExpressionStatementSyntax } assignment
            && assignment.Left == access && IsCovered(assignment.Right);
        if (!styled && !value)
        {
            return false;
        }

        Replacements[cell] = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, use, SyntaxFactory.IdentifierName("Cell")),
            SyntaxFactory.ArgumentList(arguments));
        return true;
    }

    // Two int row/column expressions (both libraries are 1-based), or one literal single-cell address.
    private bool IsSingleCell(SeparatedSyntaxList<ArgumentSyntax> arguments) => arguments switch
    {
        [var row, var column] => _model.GetTypeInfo(row.Expression).Type?.SpecialType == SpecialType.System_Int32
            && _model.GetTypeInfo(column.Expression).Type?.SpecialType == SpecialType.System_Int32,
        [{ Expression: LiteralExpressionSyntax address }] => address.IsKind(SyntaxKind.StringLiteralExpression)
            && Regex.IsMatch(address.Token.ValueText, "^[A-Z]{1,3}[1-9][0-9]{0,6}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)),
        _ => false,
    };

    // E4: the value types the harness proves equal (CellValueHarnessTests). ClosedXML writes numbers with 15 significant digits and EPPlus 4.5.3.3
    // with 17, and EPPlus writes a DateTime as a General serial number: those, and every other type, are Manual (UncoveredRuntimeType).
    private bool IsCovered(ExpressionSyntax value)
    {
        ITypeSymbol? type = _model.GetTypeInfo(value).Type;
        switch (type?.SpecialType)
        {
            case SpecialType.System_String or SpecialType.System_Boolean or SpecialType.System_Int32 or SpecialType.System_Int16
                or SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_UInt16:
                return true;
            case SpecialType.System_Double or SpecialType.System_Single or SpecialType.System_Decimal or SpecialType.System_Int64
                when _model.GetConstantValue(value) is { HasValue: true, Value: { } constant } && RoundTripsIn15Digits(Convert.ToDouble(constant, CultureInfo.InvariantCulture)):
                return true;
            default:
                _unknownVersion ??= Models.PatternResult.Manual(EPPlusReason.UncoveredRuntimeType,
                    $"cell value '{Shorten(value)}' (line {Line(value)}) has type {type?.ToDisplayString() ?? "unknown"}, which the cell-compare harness does not prove equal "
                    + "(string, bool and int types always; double, float, decimal and long only as a constant that round-trips in 15 digits; DateTime is written as a "
                    + "General serial number by EPPlus 4.5.3.3). Write it by hand.");
                return false;
        }
    }

    private static bool RoundTripsIn15Digits(double value) =>
        double.Parse(value.ToString("G15", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).Equals(value);

    private IEnumerable<IdentifierNameSyntax> UsesOf(ILocalSymbol local) =>
        _scope.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(_model.GetSymbolInfo(i).Symbol, local));

    private bool MapPackageUse(IdentifierNameSyntax use)
    {
        // p.SaveAs(new FileInfo(path)); → p.SaveAs(path);
        if (use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "SaveAs" } saveAs
            && saveAs.Parent is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var path] } file }], Parent: ExpressionStatementSyntax } invocation
            && _model.GetTypeInfo(file).Type is INamedTypeSymbol { Name: "FileInfo", ContainingNamespace: { Name: "IO", ContainingNamespace.Name: "System" } }
            && _model.GetTypeInfo(path.Expression).Type is { SpecialType: SpecialType.System_String })
        {
            Replacements[invocation.ArgumentList] = SyntaxFactory.ArgumentList([path.WithoutTrivia()]).WithTriviaFrom(invocation.ArgumentList);
            return true;
        }

        if (use.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Workbook" } workbook
            || workbook.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Worksheets" } worksheets)
        {
            return false;
        }

        // p.Workbook.Worksheets.Add("name") → p.Worksheets.Add("name")
        if (worksheets.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add" } add
            && add.Parent is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax name }] } addCall
            && name.IsKind(SyntaxKind.StringLiteralExpression) && IsValidSheetName(name.Token.ValueText)
            && (addCall.Parent is ExpressionStatementSyntax || IsWorksheetContext(addCall)))
        {
            Replacements[worksheets] = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, use, worksheets.Name);
            return true;
        }

        // p.Workbook.Worksheets["name"] / [index] → p.Worksheet("name") / p.Worksheet(position)
        if (worksheets.Parent is ElementAccessExpressionSyntax { ArgumentList.Arguments: [{ Expression: var key }] } lookup
            && IsWorksheetContext(lookup) && Position(lookup, key) is ExpressionSyntax position)
        {
            Replacements[lookup] = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, use, SyntaxFactory.IdentifierName("Worksheet")),
                SyntaxFactory.ArgumentList([SyntaxFactory.Argument(position)]));
            return true;
        }

        return false;
    }

    // Where a worksheet expression may stand: .Name, or the initializer of a single worksheet local in a block.
    private bool IsWorksheetContext(ExpressionSyntax worksheet)
    {
        if (IsNameAccess(worksheet))
        {
            return true;
        }

        if (worksheet.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator }
            && declarator.Parent is VariableDeclarationSyntax { Variables.Count: 1, Parent: LocalDeclarationStatementSyntax { UsingKeyword.RawKind: (int)SyntaxKind.None, Parent: BlockSyntax } } declaration
            && _model.GetDeclaredSymbol(declarator) is ILocalSymbol local)
        {
            _worksheets.Add(local);
            if (!declaration.Type.IsVar)
            {
                Replacements[declaration.Type] = SyntaxFactory.ParseTypeName("IXLWorksheet").WithTriviaFrom(declaration.Type);
            }

            return true;
        }

        return false;
    }

    // x.Name read, or x.Name = "literal" with a valid name no added sheet has.
    private bool IsNameAccess(SyntaxNode worksheet)
    {
        if (worksheet.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Name" } access || access.Expression != worksheet)
        {
            return false;
        }

        return access.Parent is not AssignmentExpressionSyntax assignment || assignment.Left != access
            || assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && assignment.Right is LiteralExpressionSyntax value
                && value.IsKind(SyntaxKind.StringLiteralExpression) && IsValidSheetName(value.Token.ValueText)
                && !AddedBefore(_scope.FullSpan.End).Contains(value.Token.ValueText, StringComparer.OrdinalIgnoreCase);
    }

    // The ClosedXML position (1-based) or name for a lookup that provably finds an added sheet; null otherwise.
    private ExpressionSyntax? Position(ElementAccessExpressionSyntax lookup, ExpressionSyntax key)
    {
        IReadOnlyList<string> added = AddedBefore(lookup.SpanStart);
        if (key is LiteralExpressionSyntax text && text.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return added.Contains(text.Token.ValueText, StringComparer.Ordinal) ? key.WithoutTrivia() : null;
        }

        if (key is not LiteralExpressionSyntax { Token.Value: int index })
        {
            return null;
        }

        // E3: EPPlus 5+ is 0-based; ClosedXML's Worksheet(position) is 1-based. EPPlus 4.x is not provable: the cell-compare harness shows
        // 4.5.3.3's .NET Standard build is 0-based while its .NET Framework builds default to 1-based, and both read an override from
        // configuration (EPPlus:ExcelPackage.Compatibility.IsWorksheets1Based). Council question, TAAK-309.
        Version? version = EPPlusSyntax.Version(_model);
        if (version is null || version.Major < 5)
        {
            _unknownVersion = version is null
                ? Models.PatternResult.Manual(EPPlusReason.UnknownEPPlusVersion, $"Worksheets[{index}] (line {Line(lookup)}): the referenced EPPlus version is unknown, so its index base is (4.x 0- or 1-based, 5+ 0-based).")
                : Models.PatternResult.Manual(EPPlusReason.WorksheetIndexBaseNotProvable, $"Worksheets[{index}] (line {Line(lookup)}): EPPlus {version.Major}.x is 0-based on .NET Standard/Core and 1-based on .NET Framework by default, "
                    + "and configuration can switch it (EPPlus:ExcelPackage.Compatibility.IsWorksheets1Based); use the sheet name or rewrite by hand.");
            return null;
        }

        int? first = 0;
        int position = index - first.Value + 1;
        return position >= 1 && position <= added.Count
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(position))
            : null;
    }

    // Names of the p.Workbook.Worksheets.Add("literal") calls that surely ran before a position: directly in the scope's own block, not
    // in a nested statement (a branch or loop).
    private IReadOnlyList<string> AddedBefore(int position) =>
        [.. UsesOf(_package)
            .Where(u => u.SpanStart < position)
            .Select(u => u.Parent?.Parent?.Parent?.Parent)
            .OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add", Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Worksheets" } }
                && i.ArgumentList.Arguments is [{ Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } }]
                && IsTopLevel(i))
            .Select(i => ((LiteralExpressionSyntax)i.ArgumentList.Arguments[0].Expression).Token.ValueText)];

    private bool IsTopLevel(SyntaxNode node)
    {
        StatementSyntax? statement = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        return statement?.Parent is BlockSyntax block && (block == _scope || block.Parent == _scope);
    }

    private static bool IsValidSheetName(string name) =>
        name.Length is > 0 and <= 31 && name.AsSpan().IndexOfAny(s_invalidSheetNameCharacters) < 0 && name[0] != '\'' && name[^1] != '\'';

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static string Shorten(SyntaxNode node)
    {
        string text = node.ToString().ReplaceLineEndings(" ");
        return text.Length <= 60 ? text : text[..57] + "...";
    }
}
