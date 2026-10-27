using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-327 (council round 13, E18 step 1): a private helper method that takes one <c>ExcelWorksheet</c> parameter becomes a helper that
/// takes <c>IXLWorksheet</c>, only when the flow is provable in one file: the method is private in a non-partial type, it is only ever
/// invoked (no method group), every use of the parameter in its body is one <see cref="WorkbookUses"/> maps (cells, layout, a Name read),
/// and every caller passes a worksheet another EPPlus pattern already migrated. <see cref="PackagePattern"/> accepts a worksheet local
/// passed to such a helper (<see cref="Migratable"/>).
/// TAAK-336 (council round 16): E23, a helper may pass its parameter on to another such helper; the helpers linked that way flip together,
/// at most 3 deep and without recursion (else Manual <c>FlowUseNotMigrated</c>). E21, a private worksheet field of a non-partial type is
/// Fixed when every write is a worksheet <see cref="PackagePattern"/> migrated (a constructor parameter source stays <c>FlowFieldNotMapped</c>).
/// E22, a private helper that returns <c>package.Workbook.Worksheets.Add("literal")</c> of its one <c>ExcelPackage</c> parameter is Fixed
/// when every caller passes a migrated package; any other EPPlus return stays <c>FlowReturnNotMapped</c>. Private only (E20).
/// </summary>
public class FlowTypesPattern : IPattern
{
    // E23: the longest helper chain (in methods) that flips together; the same bound as TypeArgumentClosure (A11).
    private const int MaxChainDepth = 3;

    public string Id => "epplus.flow-types";

    public string Description => "private helper with one ExcelWorksheet parameter, a worksheet field or a returned added sheet whose uses and callers are all migrated → IXLWorksheet";

    // Methods with an ExcelWorksheet parameter or an EPPlus return type, and fields of an EPPlus type.
    // E31 (council round 18): a node whose signature is E6 public API gets one row, from UnsupportedPattern, with this pattern's reason folded in.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => IsCandidate(n, model) && UnsupportedPattern.ExposedSignature(n, model) is null);

    internal static bool IsCandidate(SyntaxNode node, SemanticModel model) =>
        node switch
        {
            MethodDeclarationSyntax m => m.ParameterList.Parameters.Any(p => p.Type is not null && (IsWorksheet(model.GetTypeInfo(p.Type).Type) || EPPlusSyntax.IsRangeType(model.GetTypeInfo(p.Type).Type)))
                || model.GetTypeInfo(m.ReturnType).Type is { } returned && EPPlusApi.Mentions(returned),
            FieldDeclarationSyntax f => model.GetTypeInfo(f.Declaration.Type).Type is { } type && EPPlusApi.Mentions(type),
            _ => false,
        };

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is FieldDeclarationSyntax field)
        {
            if (field.Declaration.Variables is not [var variable] || model.GetDeclaredSymbol(variable) is not IFieldSymbol symbol)
            {
                return FieldManual(field, "declares several variables");
            }

            if (FieldBlocked(symbol, model, migrated: true) is PatternResult blockedField)
            {
                return blockedField;
            }

            TypeSyntax type = SyntaxFactory.ParseTypeName("IXLWorksheet").WithTriviaFrom(field.Declaration.Type);
            return PatternResult.Fixed(field.ReplaceNode(field.Declaration.Type, type), PackagePattern.AddedNamespace);
        }

        var declaration = (MethodDeclarationSyntax)node;
        if (model.GetDeclaredSymbol(declaration) is not IMethodSymbol method)
        {
            return PatternResult.Manual(EPPlusReason.FlowMemberNotPrivate, $"method at {Where(node)} does not bind; migrate its ExcelWorksheet parameter by hand.");
        }

        if (EPPlusApi.Mentions(method.ReturnType))
        {
            return RewriteReturn(method, declaration, model);
        }

        if (WorksheetParameter(method) is null && method.Parameters.Any(p => EPPlusSyntax.IsRangeType(p.Type)))
        {
            return RewriteRange(method, declaration, model);
        }

        if (Chain(method, model, out IParameterSymbol? parameter, out WorkbookUses? uses, out IReadOnlyCollection<IMethodSymbol> chain) is PatternResult blocked)
        {
            return blocked;
        }

        // Every caller outside the chain must pass a worksheet another pattern migrated (this pattern runs after them).
        foreach (IMethodSymbol member in chain)
        {
            int position = member.Parameters.IndexOf(WorksheetParameter(member)!);
            foreach (InvocationExpressionSyntax call in Invocations(member, declaration.SyntaxTree.GetRoot(), model))
            {
                if (call.ArgumentList.Arguments.Count <= position
                    || !IsChainParameter(call.ArgumentList.Arguments[position].Expression, chain, model) && IsWorksheet(model.GetTypeInfo(call.ArgumentList.Arguments[position].Expression).Type))
                {
                    return PatternResult.Manual(EPPlusReason.FlowCallerNotMigrated, $"'{Shorten(call)}' ({Where(call)}) passes an EPPlus worksheet that no pattern migrated; "
                        + $"{member.Name} can only take IXLWorksheet when every caller passes one (council round 13, E18). Migrate the caller first.");
                }
            }
        }

        Dictionary<SyntaxNode, SyntaxNode> replacements = uses!.Replacements;
        ParameterSyntax parameterSyntax = declaration.ParameterList.Parameters[method.Parameters.IndexOf(parameter!)];
        replacements[parameterSyntax.Type!] = SyntaxFactory.ParseTypeName("IXLWorksheet").WithTriviaFrom(parameterSyntax.Type!);
        return PatternResult.Fixed(Replace(declaration, replacements), PackagePattern.AddedNamespace);
    }

    // The worksheet parameter PackagePattern may pass a migrated worksheet local to, or null when this pattern would not migrate the method
    // (with the helpers it passes the parameter on to, or gets it from: E23).
    internal static IParameterSymbol? Migratable(IMethodSymbol method, SemanticModel model) =>
        method.DeclaringSyntaxReferences is [var reference] && reference.SyntaxTree == model.SyntaxTree
            && reference.GetSyntax() is MethodDeclarationSyntax
            && Chain(method, model, out IParameterSymbol? parameter, out _, out _) is null ? parameter : null;

    // E21: true when a write of this field may take a worksheet PackagePattern migrates (checked before PackagePattern runs).
    internal static bool MigratableField(IFieldSymbol field, SemanticModel model) => FieldBlocked(field, model, migrated: false) is null;

    // E25: the ExcelRange parameter WorkbookUses may pass a migrated range (ws.Range(..) or a range local) to, or null.
    internal static IParameterSymbol? MigratableRange(IMethodSymbol method, SemanticModel model) =>
        method.DeclaringSyntaxReferences is [var reference] && reference.SyntaxTree == model.SyntaxTree
            && reference.GetSyntax() is MethodDeclarationSyntax declaration
            && RangeBlocked(method, declaration, model, out IParameterSymbol? range, out _) is null ? range : null;

    // E22: the ExcelPackage parameter of a helper that returns package.Workbook.Worksheets.Add("literal"), or null.
    internal static IParameterSymbol? MigratableReturn(IMethodSymbol method, SemanticModel model) =>
        method.DeclaringSyntaxReferences is [var reference] && reference.SyntaxTree == model.SyntaxTree
            && reference.GetSyntax() is MethodDeclarationSyntax declaration
            && ReturnBlocked(method, declaration, model, out IParameterSymbol? package, out _) is null ? package : null;

    // E29: the literal sheet name a return helper adds (call only after MigratableReturn accepted it), or null.
    internal static string? ReturnedSheetName(IMethodSymbol method) =>
        method.DeclaringSyntaxReferences is [var reference] && reference.GetSyntax() is MethodDeclarationSyntax declaration
            && (declaration.ExpressionBody?.Expression ?? (declaration.Body?.Statements is [ReturnStatementSyntax { Expression: { } value }] ? value : null))
                is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } name }] }
            ? name.Token.ValueText
            : null;

    // The one ExcelWorksheet parameter of a method, or null.
    internal static IParameterSymbol? WorksheetParameter(IMethodSymbol method) =>
        method.Parameters.Where(p => IsWorksheet(p.Type)).ToList() is [var single] ? single : null;

    // E23: the method and the helpers its worksheet parameter flows to or from (by argument), all in this tree. They flip together: each
    // must pass Analyze, the links must not form a cycle and no path may be longer than MaxChainDepth methods.
    private static PatternResult? Chain(IMethodSymbol method, SemanticModel model, out IParameterSymbol? parameter, out WorkbookUses? uses, out IReadOnlyCollection<IMethodSymbol> chain)
    {
        var declaration = (MethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax();
        (HashSet<IMethodSymbol> members, List<(IMethodSymbol Caller, IMethodSymbol Callee)> links) = Linked(method, model);
        chain = members;
        if (Analyze(method, declaration, model, members, out parameter, out uses) is PatternResult own)
        {
            return own;
        }

        int? depth = Longest(members, links);
        if (depth is not int length || length > MaxChainDepth)
        {
            parameter = null;
            uses = null;
            return PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{method.Name} ({Where(declaration)}) passes its worksheet through a helper chain that is "
                + (depth is null ? "recursive" : $"{depth} methods deep") + $"; only chains of at most {MaxChainDepth} private helpers without recursion are mapped "
                + "(council round 16, E23). Change the helpers by hand.");
        }

        foreach (IMethodSymbol member in members.Where(m => !SymbolEqualityComparer.Default.Equals(m, method)))
        {
            PatternResult? failed = member.DeclaringSyntaxReferences is [var reference] && reference.GetSyntax() is MethodDeclarationSyntax other
                ? Analyze(member, other, model, members, out _, out _)
                : PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{member.Name} has no single method declaration in this file.");
            if (failed is not null)
            {
                parameter = null;
                uses = null;
                string why = failed is ManualResult { Reason: var reason } ? $" Failing member {member.Name}: {reason}" : string.Empty;
                return PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{method.Name} ({Where(declaration)}) passes its worksheet to or from {member.Name}, "
                    + "which this pattern does not migrate; the helpers of a chain flip together (council round 16, E23). Change them by hand." + why);
            }
        }

        return null;
    }

    // The methods of this tree linked to the method by passing a worksheet parameter as an argument (both directions), and the links.
    private static (HashSet<IMethodSymbol> Members, List<(IMethodSymbol Caller, IMethodSymbol Callee)> Links) Linked(IMethodSymbol start, SemanticModel model)
    {
        SyntaxNode root = model.SyntaxTree.GetRoot();
        HashSet<IMethodSymbol> members = new(SymbolEqualityComparer.Default) { start };
        List<(IMethodSymbol, IMethodSymbol)> links = [];
        var pending = new Queue<IMethodSymbol>([start]);
        while (pending.TryDequeue(out IMethodSymbol? method))
        {
            if (method.DeclaringSyntaxReferences is not [var reference] || reference.SyntaxTree != model.SyntaxTree)
            {
                continue;
            }

            // Down: the worksheet parameters passed on as an argument to a method of this tree.
            foreach (IdentifierNameSyntax use in reference.GetSyntax().DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (model.GetSymbolInfo(use).Symbol is IParameterSymbol { ContainingSymbol: var owner } p && SymbolEqualityComparer.Default.Equals(owner, method) && IsWorksheet(p.Type)
                    && use.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } }
                    && model.GetSymbolInfo(call).Symbol is IMethodSymbol callee
                    && callee.OriginalDefinition.DeclaringSyntaxReferences is [var calleeReference] && calleeReference.SyntaxTree == model.SyntaxTree)
                {
                    Link(method, callee.OriginalDefinition);
                }
            }

            // Up: callers that pass one of their own worksheet parameters.
            foreach (InvocationExpressionSyntax call in Invocations(method, root, model))
            {
                foreach (ArgumentSyntax argument in call.ArgumentList.Arguments)
                {
                    if (model.GetSymbolInfo(argument.Expression).Symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol caller } p && IsWorksheet(p.Type)
                        && caller.DeclaringSyntaxReferences is [var callerReference] && callerReference.SyntaxTree == model.SyntaxTree)
                    {
                        Link(caller, method);
                    }
                }
            }
        }

        return (members, links);

        void Link(IMethodSymbol caller, IMethodSymbol callee)
        {
            if (!links.Contains((caller, callee)))
            {
                links.Add((caller, callee));
            }

            foreach (IMethodSymbol m in new[] { caller, callee }.Where(members.Add))
            {
                pending.Enqueue(m);
            }
        }
    }

    // The number of methods on the longest path of the links, or null when they form a cycle (a method passing its worksheet to itself too).
    private static int? Longest(HashSet<IMethodSymbol> members, List<(IMethodSymbol Caller, IMethodSymbol Callee)> links)
    {
        Dictionary<IMethodSymbol, int> done = new(SymbolEqualityComparer.Default);
        HashSet<IMethodSymbol> active = new(SymbolEqualityComparer.Default);
        bool cycle = false;
        int longest = members.Count == 0 ? 0 : members.Max(Depth);
        return cycle ? null : longest;

        int Depth(IMethodSymbol method)
        {
            if (done.TryGetValue(method, out int known))
            {
                return known;
            }

            if (!active.Add(method))
            {
                cycle = true;
                return 0;
            }

            int depth = 1 + links.Where(l => SymbolEqualityComparer.Default.Equals(l.Caller, method)).Select(l => Depth(l.Callee)).DefaultIfEmpty(0).Max();
            active.Remove(method);
            done[method] = depth;
            return depth;
        }
    }

    private static bool IsChainParameter(ExpressionSyntax argument, IReadOnlyCollection<IMethodSymbol> chain, SemanticModel model) =>
        model.GetSymbolInfo(argument).Symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol owner } && chain.Contains(owner, SymbolEqualityComparer.Default);

    private static PatternResult? Analyze(IMethodSymbol method, MethodDeclarationSyntax declaration, SemanticModel model, IReadOnlyCollection<IMethodSymbol> chain,
        out IParameterSymbol? parameter, out WorkbookUses? uses)
    {
        parameter = null;
        uses = null;
        if (!IsPrivateHelper(method, declaration))
        {
            return PatternResult.Manual(EPPlusReason.FlowMemberNotPrivate, $"{method.Name} ({Where(declaration)}) takes an ExcelWorksheet but is not a private, non-virtual method of a "
                + "non-partial type with a body, so its callers may be out of reach (council round 13, E18: private only for now). Change the signature by hand.");
        }

        IParameterSymbol[] epplus = [.. method.Parameters.Where(p => EPPlusApi.Mentions(p.Type))];
        if (epplus is not [{ RefKind: RefKind.None, IsParams: false, HasExplicitDefaultValue: false } single] || !IsWorksheet(single.Type) || EPPlusApi.Mentions(method.ReturnType))
        {
            return PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{method.Name} ({Where(declaration)}) has EPPlus types other than one by-value ExcelWorksheet parameter "
                + "(another EPPlus parameter, a ref/out/params/optional worksheet, or an EPPlus return type); not mapped yet (council round 13, E18).");
        }

        if (MethodGroup(method, model) is { } group)
        {
            return PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{method.Name} is used as '{Shorten(group.Parent ?? group)}' ({Where(group)}), not invoked: "
                + "a delegate type would name ExcelWorksheet. Change it by hand.");
        }

        var worksheetUses = new WorkbookUses(model, declaration, single, chain);
        if (worksheetUses.Map() is PatternResult unmapped)
        {
            return unmapped;
        }

        parameter = single;
        uses = worksheetUses;
        return null;
    }

    private static bool IsPrivateHelper(IMethodSymbol method, MethodDeclarationSyntax declaration) =>
        method is { MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Private, IsVirtual: false, IsAbstract: false, IsOverride: false, IsExtern: false, PartialDefinitionPart: null, PartialImplementationPart: null }
        && method.ContainingType.DeclaringSyntaxReferences.Length == 1 && (declaration.Body is not null || declaration.ExpressionBody is not null);

    // A reference to the method that is not the callee of an invocation (a method group), or null.
    private static IdentifierNameSyntax? MethodGroup(IMethodSymbol method, SemanticModel model) =>
        References(method, model.SyntaxTree.GetRoot(), model).FirstOrDefault(r => CallOf(r) is null);

    // E25: a private helper whose one EPPlus parameter is a by-value ExcelRange, used for its address only, called with migrated ranges only.
    private static PatternResult RewriteRange(IMethodSymbol method, MethodDeclarationSyntax declaration, SemanticModel model)
    {
        if (RangeBlocked(method, declaration, model, out IParameterSymbol? range, out WorkbookUses? uses) is PatternResult blocked)
        {
            return blocked;
        }

        int position = method.Parameters.IndexOf(range!);
        foreach (InvocationExpressionSyntax call in Invocations(method, declaration.SyntaxTree.GetRoot(), model))
        {
            if (call.ArgumentList.Arguments.Count <= position || EPPlusSyntax.IsRangeType(model.GetTypeInfo(call.ArgumentList.Arguments[position].Expression).Type))
            {
                return PatternResult.Manual(EPPlusReason.FlowCallerNotMigrated, $"'{Shorten(call)}' ({Where(call)}) passes an EPPlus range that no pattern migrated; "
                    + $"{method.Name} can only take an IXLRange when every caller passes one (council round 16, E25). Migrate the caller first.");
            }
        }

        Dictionary<SyntaxNode, SyntaxNode> replacements = uses!.Replacements;
        ParameterSyntax rangeSyntax = declaration.ParameterList.Parameters[position];
        replacements[rangeSyntax.Type!] = SyntaxFactory.ParseTypeName("IXLRange").WithTriviaFrom(rangeSyntax.Type!);
        return PatternResult.Fixed(Replace(declaration, replacements), PackagePattern.AddedNamespace);
    }

    private static PatternResult? RangeBlocked(IMethodSymbol method, MethodDeclarationSyntax declaration, SemanticModel model, out IParameterSymbol? range, out WorkbookUses? uses)
    {
        range = null;
        uses = null;
        if (!IsPrivateHelper(method, declaration))
        {
            return PatternResult.Manual(EPPlusReason.FlowMemberNotPrivate, $"{method.Name} ({Where(declaration)}) takes an ExcelRange but is not a private, non-virtual method of a "
                + "non-partial type with a body, so its callers may be out of reach (council round 16, E25: private only). Change the signature by hand.");
        }

        IParameterSymbol[] epplus = [.. method.Parameters.Where(p => EPPlusApi.Mentions(p.Type))];
        if (epplus is not [{ RefKind: RefKind.None, IsParams: false, HasExplicitDefaultValue: false } single] || !EPPlusSyntax.IsRangeType(single.Type) || EPPlusApi.Mentions(method.ReturnType))
        {
            return PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{method.Name} ({Where(declaration)}) has EPPlus types other than one by-value ExcelRange parameter "
                + "(another EPPlus parameter, a ref/out/params/optional range, or an EPPlus return type); not mapped yet (council round 16, E25).");
        }

        if (MethodGroup(method, model) is { } group)
        {
            return PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{method.Name} is used as '{Shorten(group.Parent ?? group)}' ({Where(group)}), not invoked: "
                + "a delegate type would name ExcelRange. Change it by hand.");
        }

        var rangeUses = new WorkbookUses(model, declaration, single);
        if (rangeUses.Map() is PatternResult unmapped)
        {
            return unmapped;
        }

        range = single;
        uses = rangeUses;
        return null;
    }

    // E22: package.Workbook.Worksheets.Add("literal") returned by a private helper whose one EPPlus parameter is that ExcelPackage.
    private static PatternResult RewriteReturn(IMethodSymbol method, MethodDeclarationSyntax declaration, SemanticModel model)
    {
        if (ReturnBlocked(method, declaration, model, out IParameterSymbol? package, out MemberAccessExpressionSyntax? worksheets) is PatternResult blocked)
        {
            return blocked;
        }

        int position = method.Parameters.IndexOf(package!);
        foreach (InvocationExpressionSyntax call in Invocations(method, declaration.SyntaxTree.GetRoot(), model))
        {
            if (call.ArgumentList.Arguments.Count <= position || EPPlusSyntax.IsPackageType(model.GetTypeInfo(call.ArgumentList.Arguments[position].Expression).Type))
            {
                return PatternResult.Manual(EPPlusReason.FlowCallerNotMigrated, $"'{Shorten(call)}' ({Where(call)}) passes an ExcelPackage that no pattern migrated; "
                    + $"{method.Name} can only take an XLWorkbook when every caller passes one (council round 16, E22). Migrate the caller first.");
            }
        }

        ParameterSyntax packageSyntax = declaration.ParameterList.Parameters[position];
        Dictionary<SyntaxNode, SyntaxNode> replacements = new()
        {
            [declaration.ReturnType] = SyntaxFactory.ParseTypeName("IXLWorksheet").WithTriviaFrom(declaration.ReturnType),
            [packageSyntax.Type!] = SyntaxFactory.ParseTypeName("XLWorkbook").WithTriviaFrom(packageSyntax.Type!),
            [worksheets!] = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ((MemberAccessExpressionSyntax)worksheets!.Expression).Expression, worksheets.Name),
        };
        return PatternResult.Fixed(Replace(declaration, replacements), PackagePattern.AddedNamespace);
    }

    private static PatternResult? ReturnBlocked(IMethodSymbol method, MethodDeclarationSyntax declaration, SemanticModel model, out IParameterSymbol? package, out MemberAccessExpressionSyntax? worksheets)
    {
        package = null;
        worksheets = null;
        if (!IsPrivateHelper(method, declaration))
        {
            return PatternResult.Manual(EPPlusReason.FlowMemberNotPrivate, $"{method.Name} ({Where(declaration)}) returns an EPPlus type but is not a private, non-virtual method of a "
                + "non-partial type with a body, so its callers may be out of reach (council round 16, E20: private only). Change it by hand.");
        }

        ExpressionSyntax? returned = declaration.ExpressionBody?.Expression ?? (declaration.Body?.Statements is [ReturnStatementSyntax { Expression: { } value }] ? value : null);
        IParameterSymbol[] epplus = [.. method.Parameters.Where(p => EPPlusApi.Mentions(p.Type))];
        if (!IsWorksheet(method.ReturnType) || epplus is not [{ RefKind: RefKind.None, IsParams: false, HasExplicitDefaultValue: false } single] || !EPPlusSyntax.IsPackageType(single.Type)
            || returned is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add", Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Worksheets", Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Workbook", Expression: IdentifierNameSyntax receiver } } access } } add
            || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, single)
            || References(single, declaration, model).Count() != 1)
        {
            return PatternResult.Manual(EPPlusReason.FlowReturnNotMapped, $"{method.Name} ({Where(declaration)}) returns an EPPlus type; only a private helper that returns "
                + "package.Workbook.Worksheets.Add(\"name\") of its one ExcelPackage parameter (and uses it nowhere else) is mapped (council round 16, E22). Change the return type and its callers by hand.");
        }

        if (add.ArgumentList.Arguments is not [{ Expression: LiteralExpressionSyntax name }] || !name.IsKind(SyntaxKind.StringLiteralExpression) || !WorkbookUses.IsValidSheetName(name.Token.ValueText))
        {
            return PatternResult.Manual(EPPlusReason.FlowReturnNotMapped, $"{method.Name} ({Where(declaration)}) returns a sheet added under '{Shorten(add.ArgumentList)}', which is not a "
                + "valid literal sheet name: an invalid name throws a different exception in each library, and no constant is followed through calls (council round 16, E22). Change it by hand.");
        }

        if (MethodGroup(method, model) is { } group)
        {
            return PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{method.Name} is used as '{Shorten(group.Parent ?? group)}' ({Where(group)}), not invoked: "
                + "a delegate type would name EPPlus types. Change it by hand.");
        }

        package = single;
        worksheets = access;
        return null;
    }

    // E21: a private worksheet field (one variable, no initializer) of a non-partial type, referenced by its bare name only. Every write is
    // a statement 'field = package.Workbook.Worksheets.Add("x")' (or a lookup) of an ExcelPackage local, directly in that local's scope; after
    // PackagePattern ran (migrated) that package is no longer an ExcelPackage. Other uses are Name reads, or uses later in a write's scope
    // (PackagePattern maps those as uses of a worksheet local).
    private static PatternResult? FieldBlocked(IFieldSymbol field, SemanticModel model, bool migrated)
    {
        if (field is not { DeclaredAccessibility: Accessibility.Private, IsConst: false } || !IsWorksheet(field.Type) || field.ContainingType.DeclaringSyntaxReferences.Length != 1
            || field.DeclaringSyntaxReferences is not [var reference] || reference.SyntaxTree != model.SyntaxTree
            || reference.GetSyntax() is not VariableDeclaratorSyntax { Initializer: null, Parent: VariableDeclarationSyntax { Variables.Count: 1, Parent: FieldDeclarationSyntax declaration } })
        {
            return FieldManual(null, $"'{field.Name}' is not a private worksheet field (one variable, no initializer) of a non-partial type");
        }

        List<(SyntaxNode Scope, int End)> scopes = [];
        List<IdentifierNameSyntax> others = [];
        foreach (IdentifierNameSyntax use in References(field, model.SyntaxTree.GetRoot(), model))
        {
            if (use.Parent is MemberAccessExpressionSyntax qualified && qualified.Name == use)
            {
                return FieldManual(declaration, $"is used as '{Shorten(qualified)}' ({Where(use)}), through a qualifier");
            }

            if (use.Parent is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Parent: ExpressionStatementSyntax statement } write && write.Left == use)
            {
                if (Source(write.Right, model, migrated) is not ILocalSymbol package || PackageScope(package) is not SyntaxNode scope
                    || statement.Parent is not BlockSyntax block || block != scope && block.Parent != scope)
                {
                    return FieldManual(declaration, $"is assigned '{Shorten(write.Right)}' ({Where(write)}), which is not a worksheet added to or looked up in an ExcelPackage local "
                        + (migrated ? "that PackagePattern migrated " : string.Empty) + "in the same block (a constructor parameter or another member decides what it is)");
                }

                scopes.Add((scope, statement.Span.End));
            }
            else if (!(use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Name" } name && name.Expression == use
                && !(name.Parent is AssignmentExpressionSyntax nameWrite && nameWrite.Left == name)))
            {
                others.Add(use);
            }
        }

        if (scopes.Count == 0)
        {
            return FieldManual(declaration, "is never assigned a migrated worksheet");
        }

        return others.FirstOrDefault(o => !scopes.Any(s => s.Scope.Span.Contains(o.Span) && o.SpanStart > s.End)) is { } outside
            ? FieldManual(declaration, $"is used as '{Shorten(outside.Parent ?? outside)}' ({Where(outside)}) outside the block after a write that migrates it (only Name reads are mapped there)")
            : null;
    }

    private static PatternResult FieldManual(FieldDeclarationSyntax? field, string why) =>
        PatternResult.Manual(EPPlusReason.FlowFieldNotMapped, $"field {(field is null ? string.Empty : $"'{Shorten(field.Declaration)}' ({Where(field)}) ")}{why}; "
            + "a field is Fixed only when every write is a worksheet of a migrated package local in one method (council round 16, E21). Change the field and its uses by hand.");

    // The package local a worksheet expression comes from: before PackagePattern, p.Workbook.Worksheets.Add("x") / p.Workbook.Worksheets[..]
    // of an ExcelPackage local; after it (migrated), p.Worksheets.Add("x") / p.Worksheet(..) of a local that is no longer an ExcelPackage.
    private static ILocalSymbol? Source(ExpressionSyntax value, SemanticModel model, bool migrated)
    {
        ExpressionSyntax? receiver = (migrated, value) switch
        {
            (false, InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add", Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Worksheets", Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Workbook" } workbook } } }) => workbook.Expression,
            (false, ElementAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Worksheets", Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Workbook" } workbook } }) => workbook.Expression,
            (true, InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add", Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Worksheets" } worksheets } }) => worksheets.Expression,
            (true, InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Worksheet" } lookup }) => lookup.Expression,
            _ => null,
        };
        return receiver is IdentifierNameSyntax && model.GetSymbolInfo(receiver).Symbol is ILocalSymbol local
            && EPPlusSyntax.IsPackageType(local.Type) != migrated && !(migrated && model.GetTypeInfo(value).Type is { } type && EPPlusApi.Mentions(type))
            ? local
            : null;
    }

    // The scope PackagePattern rewrites for a package local: its using statement, or the block of its declaration.
    private static SyntaxNode? PackageScope(ILocalSymbol local) =>
        local.DeclaringSyntaxReferences is [var reference] && reference.GetSyntax() is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration }
            ? declaration.Parent switch
            {
                UsingStatementSyntax statement => statement,
                LocalDeclarationStatementSyntax { Parent: BlockSyntax block } => block,
                _ => null,
            }
            : null;

    private static SyntaxNode Replace(SyntaxNode node, Dictionary<SyntaxNode, SyntaxNode> replacements)
    {
        SyntaxNode result = node.TrackNodes(replacements.Keys);
        var byCurrent = replacements.ToDictionary(r => result.GetCurrentNode(r.Key)!, r => r.Value);
        return result.ReplaceNodes(byCurrent.Keys, (current, _) => byCurrent[current]);
    }

    private static IEnumerable<IdentifierNameSyntax> References(ISymbol symbol, SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.ValueText == symbol.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol?.OriginalDefinition, symbol.OriginalDefinition));

    private static IEnumerable<InvocationExpressionSyntax> Invocations(IMethodSymbol method, SyntaxNode root, SemanticModel model) =>
        References(method, root, model).Select(CallOf).OfType<InvocationExpressionSyntax>();

    // The invocation a name is the callee of (Fill(..), this.Fill(..), Report.Fill(..)); null for a method group.
    private static InvocationExpressionSyntax? CallOf(IdentifierNameSyntax name)
    {
        ExpressionSyntax callee = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? access : name;
        return callee.Parent is InvocationExpressionSyntax invocation && invocation.Expression == callee ? invocation : null;
    }

    private static bool IsWorksheet(ITypeSymbol? type) =>
        type is INamedTypeSymbol { Name: "ExcelWorksheet", ContainingNamespace: { Name: EPPlusSyntax.Namespace, ContainingNamespace.IsGlobalNamespace: true } };

    private static string Where(SyntaxNode node) => $"line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";

    private static string Shorten(SyntaxNode node)
    {
        string text = node.ToString().ReplaceLineEndings(" ");
        return text.Length <= 60 ? text : text[..57] + "...";
    }
}
