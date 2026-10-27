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
    private readonly ILocalSymbol? _package;
    private readonly List<ISymbol> _worksheets = [];
    private readonly List<ISymbol> _ranges = [];
    private readonly IReadOnlyCollection<IMethodSymbol> _chain = [];
    private Models.PatternResult? _unknownVersion;

    private readonly WorksheetIndexBase _indexBase;

    public WorkbookUses(SemanticModel model, SyntaxNode scope, ILocalSymbol package, WorksheetIndexBase indexBase)
    {
        _indexBase = indexBase;
        _model = model;
        _scope = scope;
        _package = package;
    }

    // TAAK-327 (council E18): the uses of one ExcelWorksheet parameter of a private helper, with no package in reach. Only what is provable
    // without knowing the workbook: cells, layout and a Name read (a rename could clash with a sheet the caller added), and (E23) passing it
    // on to a helper of the same chain, which flips with it.
    public WorkbookUses(SemanticModel model, SyntaxNode scope, IParameterSymbol worksheet, IReadOnlyCollection<IMethodSymbol> chain)
    {
        _chain = chain;
        _indexBase = WorksheetIndexBase.Unknown(worksheet.Name);
        _model = model;
        _scope = scope;
        _worksheets.Add(worksheet);
    }

    // TAAK-337 (council round 16, E25): the uses of one ExcelRange parameter of a private helper: address reads, nothing else.
    public WorkbookUses(SemanticModel model, SyntaxNode scope, IParameterSymbol range)
    {
        _indexBase = WorksheetIndexBase.Unknown(range.Name);
        _model = model;
        _scope = scope;
        _ranges.Add(range);
    }

    public Dictionary<SyntaxNode, SyntaxNode> Replacements { get; } = [];

    // Statements to insert before a statement of the scope (row 6: the MemoryStream that replaces GetAsByteArray).
    public Dictionary<StatementSyntax, List<StatementSyntax>> Insertions { get; } = [];

    // True when the result needs System.IO (a MemoryStream was added).
    public bool UsesSystemIO { get; private set; }

    // True when an integer worksheet index was rewritten (the result then says where IsWorksheets1Based was looked for, E11).
    public bool UsesIndex { get; private set; }

    // Null when every use is mapped; otherwise the Manual result that says why not.
    public Models.PatternResult? Map()
    {
        foreach (IdentifierNameSyntax use in _package is null ? [] : UsesOf(_package))
        {
            if (!MapPackageUse(use))
            {
                return _unknownVersion ?? Models.PatternResult.Manual($"ExcelPackage local {_package!.Name} is used as '{Shorten(use.Parent ?? use)}' (line {Line(use)}), which is not mapped yet (epplus-queue).");
            }
        }

        // A worksheet local found above: its Name and its single cells (rows 4-5).
        for (int i = 0; i < _worksheets.Count; i++)
        {
            foreach (IdentifierNameSyntax use in UsesOf(_worksheets[i]))
            {
                if (!IsNameAccess(use) && !MapCellUse(use) && !IsLayoutUse(use) && !IsFlowArgument(use) && !IsFieldWrite(use))
                {
                    string text = $"Worksheet {(_package is null ? "parameter" : "local")} {_worksheets[i].Name} is used as '{Shorten(use.Parent ?? use)}' (line {Line(use)}), which is not mapped yet (epplus-queue row 4+).";
                    return _unknownVersion ?? (_package is null ? Models.PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, text) : Models.PatternResult.Manual(text));
                }
            }
        }

        // E25: a range local declared from a mapped Cells[..] (it became ws.Range(..)), or a range parameter: address reads only.
        foreach (ISymbol range in _ranges)
        {
            foreach (IdentifierNameSyntax use in UsesOf(range))
            {
                if (!MapRangeRead(use) && !IsRangeFlowArgument(use))
                {
                    string text = $"Range {(range is IParameterSymbol ? "parameter" : "local")} {range.Name} is used as '{Shorten(use.Parent ?? use)}' (line {Line(use)}); only "
                        + "its address (.Rows, .Columns, .Start/.End .Row/.Column) and passing a local on to a range helper are mapped (council round 16, E25).";
                    return _unknownVersion ?? (_package is null ? Models.PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, text) : Models.PatternResult.Manual(text));
                }
            }
        }

        return null;
    }

    // r.RowCount() / r.RangeAddress.FirstAddress.RowNumber as AddressPattern wrote it, or the EPPlus r.Rows / r.Start.Row, rewritten here
    // (a range parameter: AddressPattern leaves those to FlowTypesPattern).
    private bool MapRangeRead(IdentifierNameSyntax use)
    {
        if (IsAddressRead(use))
        {
            return true;
        }

        if (use.Parent is not MemberAccessExpressionSyntax first || first.Expression != use)
        {
            return false;
        }

        MemberAccessExpressionSyntax access = first.Name.Identifier.ValueText is "Start" or "End" && first.Parent is MemberAccessExpressionSyntax corner && corner.Expression == first ? corner : first;
        if (AddressPattern.Mapped(access) is not (var range, string text) || range != use
            || _model.GetSymbolInfo(access).Symbol is not IPropertySymbol { ContainingNamespace: { Name: EPPlusSyntax.Namespace, ContainingNamespace.IsGlobalNamespace: true } })
        {
            return false;
        }

        Replacements[access] = SyntaxFactory.ParseExpression($"{use.Identifier.ValueText}.{text}").WithTriviaFrom(access);
        return true;
    }

    // E25: a range local passed (by position, by value) to a private helper whose ExcelRange parameter FlowTypesPattern migrates.
    private bool IsRangeFlowArgument(IdentifierNameSyntax use) =>
        _model.GetSymbolInfo(use).Symbol is ILocalSymbol && IsRangeHelperArgument(use);

    private bool IsRangeHelperArgument(ExpressionSyntax range)
    {
        if (range.Parent is not ArgumentSyntax { RefKindKeyword.RawKind: (int)SyntaxKind.None, NameColon: null } argument
            || argument.Parent is not ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } list
            || _model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        int position = list.Arguments.IndexOf(argument);
        return position < method.Parameters.Length && FlowTypesPattern.MigratableRange(method.OriginalDefinition, _model) is { } target
            && SymbolEqualityComparer.Default.Equals(target, method.OriginalDefinition.Parameters[position]);
    }

    // ws.Cells[r, c] / ws.Cells["A1"] → ws.Cell(r, c) / ws.Cell("A1"), as the target of a covered Value assignment (row 5, E4) or as the
    // receiver of a style statement StylesPattern already migrated (row 8).
    private bool MapCellUse(IdentifierNameSyntax use)
    {
        if (use.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Cells" } cells || cells.Expression != use
            || cells.Parent is not ElementAccessExpressionSyntax { ArgumentList.Arguments: var arguments } cell)
        {
            return false;
        }

        bool single = IsSingleCell(arguments);
        if (!single && !IsRange(arguments))
        {
            return false;
        }

        // Reads differ (CellReadHarnessTests): EPPlus .Text of a bool is "1" (ClosedXML "TRUE"), GetValue<string> of an empty cell is null
        // (ClosedXML ""), and .Value is object (ClosedXML XLCellValue).
        if (cell.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Text" or "GetValue" or "Value" } read
            && !(read.Parent is AssignmentExpressionSyntax { Left: var left } && left == read))
        {
            _unknownVersion ??= Models.PatternResult.Manual(EPPlusReason.CellReadDiffers,
                $"'{Shorten(read.Parent ?? read)}' (line {Line(read)}) reads a cell: EPPlus .Text of a bool is \"1\" where ClosedXML's GetFormattedString() is \"TRUE\", "
                + "GetValue<string>() of an empty cell is null where ClosedXML returns \"\", and .Value is object where ClosedXML has XLCellValue. GetValue<T> for int, long, double, decimal, DateTime and bool differs on an empty cell at least "
                + "(EPPlus returns default(T), ClosedXML throws; council round 12 E12, GetValueMatrixTests). Rewrite the read by hand.");
            return false;
        }

        StatementSyntax? statement = cell.FirstAncestorOrSelf<StatementSyntax>();
        bool styled = statement is not null && statement.HasAnnotations(StylesPattern.MigratedAnnotationKind)
            && cell.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Style" };
        // range.Merge(); as LayoutPattern writes it for Merge = true (row 9).
        bool merged = !single && cell.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Merge" } merge
            && merge.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0, Parent: ExpressionStatementSyntax };
        // range.InsertData(items); as LoadFromPattern writes it for LoadFromCollection (row 10).
        bool inserted = cell.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "InsertData" } insert
            && insert.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1, Parent: ExpressionStatementSyntax };
        bool value = cell.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Value" } access
            && access.Parent is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Parent: ExpressionStatementSyntax } assignment
            && assignment.Left == access && IsCovered(assignment.Right);
        // range.RowCount() / range.RangeAddress.FirstAddress.RowNumber as AddressPattern writes it for .Rows / .Start.Row (TAAK-327, E18 step 2).
        bool addressed = !single && IsAddressRead(cell);
        // E25: var r = ws.Cells[..]; becomes a ClosedXML range local (its uses are checked in Map), or the range is passed to a range helper.
        bool local = !single && IsRangeLocal(cell, arguments);
        bool passed = !single && AddressPattern.InOrder(arguments, _model) && IsRangeHelperArgument(cell);
        if (!styled && !value && !merged && !inserted && !addressed && !local && !passed)
        {
            return false;
        }

        Replacements[cell] = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, use, SyntaxFactory.IdentifierName(single ? "Cell" : "Range")),
            SyntaxFactory.ArgumentList(arguments));
        return true;
    }

    // E25: the initializer of a range local AddressPattern accepts (one local, declared once, never written, corners in order).
    private bool IsRangeLocal(ElementAccessExpressionSyntax cell, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        if (cell.Parent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } declarator }
            || _model.GetDeclaredSymbol(declarator) is not ILocalSymbol range
            || AddressPattern.LocalSource(range, _model) != cell || !AddressPattern.InOrder(arguments, _model))
        {
            return false;
        }

        _ranges.Add(range);
        if (!declaration.Type.IsVar)
        {
            Replacements[declaration.Type] = SyntaxFactory.ParseTypeName("IXLRange").WithTriviaFrom(declaration.Type);
        }

        return true;
    }

    // Four int expressions (from row, from column, to row, to column), or one literal range address "A1:C2".
    private bool IsRange(SeparatedSyntaxList<ArgumentSyntax> arguments) => arguments switch
    {
        [_, _, _, _] => arguments.All(a => _model.GetTypeInfo(a.Expression).Type?.SpecialType == SpecialType.System_Int32),
        [{ Expression: LiteralExpressionSyntax address }] => address.IsKind(SyntaxKind.StringLiteralExpression)
            && Regex.IsMatch(address.Token.ValueText, "^[A-Z]{1,3}[1-9][0-9]{0,6}:[A-Z]{1,3}[1-9][0-9]{0,6}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)),
        _ => false,
    };

    // Statements LayoutPattern wrote on the worksheet itself (row 9): ws.Row(i).Height = h; and ws.SheetView.Freeze(r, c); are the same in ClosedXML.
    // Also the ws.LastRowUsed(XLCellsUsedOptions.All) etc. that DimensionPattern wrote (row 7).
    private static bool IsLayoutUse(IdentifierNameSyntax use) =>
        use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "LastRowUsed" or "LastColumnUsed" or "FirstRowUsed" or "FirstColumnUsed" } used && used.Expression == use
            && used.Parent is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "All" } }] }
        ||
        use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Row" } row && row.Expression == use
            && row.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } rowCall
            && rowCall.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Height" } height
            && height.Parent is AssignmentExpressionSyntax { Parent: ExpressionStatementSyntax } assignment && assignment.Left == height
        || use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "SheetView" } view && view.Expression == use
            && view.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Freeze" } freeze
            && freeze.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 2, Parent: ExpressionStatementSyntax };

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
        if (type is null or IErrorTypeSymbol && IsRowOrColumnNumber(value))
        {
            return true; // sheet.LastRowUsed(XLCellsUsedOptions.All).RowNumber() from DimensionPattern: an int, but unbound in this pass
        }

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

    private static bool IsAddressRead(ExpressionSyntax range) =>
        range.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RowCount" or "ColumnCount" } count && count.Expression == range
            && count.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 }
        || range.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RangeAddress" } address && address.Expression == range
            && address.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "FirstAddress" or "LastAddress" } corner
            && corner.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RowNumber" or "ColumnNumber" };

    // The range of an address read is ws.Range(..) (was ws.Cells[..]) or, E25, a range local declared from one.
    private bool IsRowOrColumnNumber(ExpressionSyntax value) =>
        value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RowCount" or "ColumnCount", Expression: var range } } && IsAddressRange(range) && IsAddressRead(range)
        || value is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RowNumber" or "ColumnNumber", Expression: MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: var corner } } } && IsAddressRange(corner) && IsAddressRead(corner)
        || value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RowNumber" or "ColumnNumber", Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "LastRowUsed" or "LastColumnUsed" or "FirstRowUsed" or "FirstColumnUsed" } } } };

    private bool IsAddressRange(ExpressionSyntax range) =>
        range is ElementAccessExpressionSyntax
        || range is IdentifierNameSyntax && _model.GetSymbolInfo(range).Symbol is ILocalSymbol local && AddressPattern.LocalSource(local, _model) is not null;

    private static bool RoundTripsIn15Digits(double value) =>
        double.Parse(value.ToString("G15", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).Equals(value);

    private static bool IsStream(ITypeSymbol? type)
    {
        for (ITypeSymbol? t = type; t is not null; t = t.BaseType)
        {
            if (t is { Name: "Stream", ContainingNamespace: { Name: "IO", ContainingNamespace.Name: "System" } })
            {
                return true;
            }
        }

        return false;
    }

    // A local name no symbol in reach of the statement uses, and that this rewrite has not handed out yet.
    private string FreeName(StatementSyntax statement, string name)
    {
        HashSet<string> taken = [.. _model.LookupSymbols(statement.SpanStart).Select(s => s.Name),
            .. _scope.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v => v.Identifier.ValueText),
            .. Insertions.Values.SelectMany(l => l).OfType<LocalDeclarationStatementSyntax>().Select(d => d.Declaration.Variables[0].Identifier.ValueText)];
        string candidate = name;
        for (int i = 2; taken.Contains(candidate); i++)
        {
            candidate = name + i.ToString(CultureInfo.InvariantCulture);
        }

        return candidate;
    }

    // TAAK-327: the worksheet local passed (by position, by value) to a private helper whose worksheet parameter FlowTypesPattern migrates.
    private bool IsFlowArgument(IdentifierNameSyntax use)
    {
        if (use.Parent is not ArgumentSyntax { RefKindKeyword.RawKind: (int)SyntaxKind.None, NameColon: null } argument
            || argument.Parent is not ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } list
            || _model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        int position = list.Arguments.IndexOf(argument);
        IParameterSymbol? target = _package is not null ? FlowTypesPattern.Migratable(method, _model)
            : _chain.Contains(method.OriginalDefinition, SymbolEqualityComparer.Default) ? FlowTypesPattern.WorksheetParameter(method.OriginalDefinition) : null;
        return position < method.Parameters.Length && target is not null
            && SymbolEqualityComparer.Default.Equals(target, method.OriginalDefinition.Parameters[position]);
    }

    // E21: the field itself on the left of 'field = <worksheet>;' (the worksheet on the right is mapped as a package use).
    private bool IsFieldWrite(IdentifierNameSyntax use) =>
        _package is not null && _model.GetSymbolInfo(use).Symbol is IFieldSymbol
        && use.Parent is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Parent: ExpressionStatementSyntax } assignment && assignment.Left == use;

    // E22: p passed (by value, by position) as the package of a private helper that returns p.Workbook.Worksheets.Add("literal"), whose result
    // is dropped or stands where a worksheet may.
    private bool IsReturnFlowArgument(IdentifierNameSyntax use)
    {
        if (use.Parent is not ArgumentSyntax { RefKindKeyword.RawKind: (int)SyntaxKind.None, NameColon: null } argument
            || argument.Parent is not ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } list
            || _model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        int position = list.Arguments.IndexOf(argument);
        return position < method.Parameters.Length
            && FlowTypesPattern.MigratableReturn(method.OriginalDefinition, _model) is { } package
            && SymbolEqualityComparer.Default.Equals(package, method.OriginalDefinition.Parameters[position])
            && (invocation.Parent is ExpressionStatementSyntax || IsWorksheetContext(invocation));
    }

    private IEnumerable<IdentifierNameSyntax> UsesOf(ISymbol local) =>
        _scope.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(_model.GetSymbolInfo(i).Symbol, local));

    private bool MapPackageUse(IdentifierNameSyntax use)
    {
        if (IsReturnFlowArgument(use))
        {
            return true;
        }

        // Row 6: p.SaveAs(stream); is the same call (SaveHarnessTests: both leave the stream open at its end).
        if (use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "SaveAs" } toStream
            && toStream.Parent is InvocationExpressionSyntax { ArgumentList.Arguments: [var target], Parent: ExpressionStatementSyntax }
            && IsStream(_model.GetTypeInfo(target.Expression).Type))
        {
            return true;
        }

        // Row 6: p.GetAsByteArray() in a statement of a block → using var s = new MemoryStream(); p.SaveAs(s); before it, and s.ToArray().
        if (use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetAsByteArray" } bytes
            && bytes.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } bytesCall
            && bytesCall.FirstAncestorOrSelf<StatementSyntax>() is { Parent: BlockSyntax } statement
            && !bytesCall.Ancestors().TakeWhile(a => a != statement).Any(a => a is AnonymousFunctionExpressionSyntax))
        {
            string stream = FreeName(statement, "workbookStream");
            if (!Insertions.TryGetValue(statement, out List<StatementSyntax>? before))
            {
                Insertions[statement] = before = [];
            }

            before.Add(SyntaxFactory.ParseStatement($"using var {stream} = new MemoryStream();"));
            before.Add(SyntaxFactory.ParseStatement($"{use.Identifier.ValueText}.SaveAs({stream});"));
            Replacements[bytesCall] = SyntaxFactory.ParseExpression($"{stream}.ToArray()");
            UsesSystemIO = true;
            return true;
        }

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

        // E21: _field = p.Workbook.Worksheets.Add("name"); for a private worksheet field FlowTypesPattern migrates with it.
        if (worksheet.Parent is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Parent: ExpressionStatementSyntax, Left: IdentifierNameSyntax target } assignment
            && assignment.Right == worksheet && _package is not null
            && _model.GetSymbolInfo(target).Symbol is IFieldSymbol field && FlowTypesPattern.MigratableField(field, _model))
        {
            if (!_worksheets.Contains(field, SymbolEqualityComparer.Default))
            {
                _worksheets.Add(field);
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
            || _package is not null && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && assignment.Right is LiteralExpressionSyntax value
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

        // E3 for 5+, E10/E11 for 4.x (WorksheetIndexBase); ClosedXML's Worksheet(position) is 1-based.
        if (_indexBase.First is not int first)
        {
            _unknownVersion = _indexBase.Blocked;
            return null;
        }

        UsesIndex = true;
        int position = index - first + 1;
        return position >= 1 && position <= added.Count
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(position))
            : null;
    }

    // Names of the p.Workbook.Worksheets.Add("literal") calls that surely ran before a position: directly in the scope's own block, not
    // in a nested statement (a branch or loop).
    private IReadOnlyList<string> AddedBefore(int position) =>
        _package is null ? [] : [.. UsesOf(_package)
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

    internal static bool IsValidSheetName(string name) =>
        name.Length is > 0 and <= 31 && name.AsSpan().IndexOfAny(s_invalidSheetNameCharacters) < 0 && name[0] != '\'' && name[^1] != '\'';

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static string Shorten(SyntaxNode node)
    {
        string text = node.ToString().ReplaceLineEndings(" ");
        return text.Length <= 60 ? text : text[..57] + "...";
    }
}
