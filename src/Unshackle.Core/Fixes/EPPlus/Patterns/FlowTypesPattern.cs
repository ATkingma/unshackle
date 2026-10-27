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
/// passed to such a helper (<see cref="Migratable"/>). A field of an EPPlus type is Manual <c>FlowFieldNotMapped</c> and a method with an EPPlus
/// return type Manual <c>FlowReturnNotMapped</c> (conservative until TAAK-327 council questions 2 and 3 are answered); packages and ranges as
/// parameters are not mapped yet.
/// </summary>
public class FlowTypesPattern : IPattern
{
    public string Id => "epplus.flow-types";

    public string Description => "private helper with one ExcelWorksheet parameter whose uses and callers are all migrated → IXLWorksheet parameter";

    // Methods with an ExcelWorksheet parameter or an EPPlus return type, and fields of an EPPlus type (the last two are Manual for now).
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n switch
        {
            MethodDeclarationSyntax m => m.ParameterList.Parameters.Any(p => p.Type is not null && IsWorksheet(model.GetTypeInfo(p.Type).Type))
                || model.GetTypeInfo(m.ReturnType).Type is { } returned && EPPlusApi.Mentions(returned),
            FieldDeclarationSyntax f => model.GetTypeInfo(f.Declaration.Type).Type is { } type && EPPlusApi.Mentions(type),
            _ => false,
        });

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is FieldDeclarationSyntax field)
        {
            // Council question 2 (TAAK-327): a field's flow crosses every method of the type and the constructor's callers; not proven yet.
            return PatternResult.Manual(EPPlusReason.FlowFieldNotMapped, $"field '{Shorten(field.Declaration)}' ({Where(field)}) has an EPPlus type; a field is assigned and read "
                + "across methods (and from constructor callers), which E18 step 1 does not prove yet (TAAK-327 council question 2). Change the field and its uses by hand.");
        }

        var declaration = (MethodDeclarationSyntax)node;
        if (model.GetTypeInfo(declaration.ReturnType).Type is { } returnType && EPPlusApi.Mentions(returnType))
        {
            // Council question 3 (TAAK-327): what a returned sheet or package is depends on every caller's use of the result.
            return PatternResult.Manual(EPPlusReason.FlowReturnNotMapped, $"{declaration.Identifier.ValueText} ({Where(declaration)}) returns an EPPlus type; the returned value "
                + "flows into every caller, which E18 step 1 does not prove yet (TAAK-327 council question 3). Change the return type and its callers by hand.");
        }

        if (model.GetDeclaredSymbol(declaration) is not IMethodSymbol method)
        {
            return PatternResult.Manual(EPPlusReason.FlowMemberNotPrivate, $"method at {Where(node)} does not bind; migrate its ExcelWorksheet parameter by hand.");
        }

        if (Analyze(method, declaration, model, out IParameterSymbol? parameter, out WorkbookUses? uses) is PatternResult blocked)
        {
            return blocked;
        }

        foreach (InvocationExpressionSyntax call in Invocations(method, declaration.SyntaxTree.GetRoot(), model))
        {
            int position = method.Parameters.IndexOf(parameter!);
            if (call.ArgumentList.Arguments.Count <= position || IsWorksheet(model.GetTypeInfo(call.ArgumentList.Arguments[position].Expression).Type))
            {
                return PatternResult.Manual(EPPlusReason.FlowCallerNotMigrated, $"'{Shorten(call)}' ({Where(call)}) passes an EPPlus worksheet that no pattern migrated; "
                    + $"{method.Name} can only take IXLWorksheet when every caller passes one (council round 13, E18). Migrate the caller first.");
            }
        }

        Dictionary<SyntaxNode, SyntaxNode> replacements = uses!.Replacements;
        ParameterSyntax parameterSyntax = declaration.ParameterList.Parameters[method.Parameters.IndexOf(parameter!)];
        replacements[parameterSyntax.Type!] = SyntaxFactory.ParseTypeName("IXLWorksheet").WithTriviaFrom(parameterSyntax.Type!);
        SyntaxNode result = declaration.TrackNodes(replacements.Keys);
        var byCurrent = replacements.ToDictionary(r => result.GetCurrentNode(r.Key)!, r => r.Value);
        result = result.ReplaceNodes(byCurrent.Keys, (current, _) => byCurrent[current]);
        return PatternResult.Fixed(result, PackagePattern.AddedNamespace);
    }

    // The worksheet parameter PackagePattern may pass a migrated worksheet local to, or null when this pattern would not migrate the method.
    internal static IParameterSymbol? Migratable(IMethodSymbol method, SemanticModel model) =>
        method.DeclaringSyntaxReferences is [var reference] && reference.SyntaxTree == model.SyntaxTree
            && reference.GetSyntax() is MethodDeclarationSyntax declaration
            && Analyze(method, declaration, model, out IParameterSymbol? parameter, out _) is null ? parameter : null;

    private static PatternResult? Analyze(IMethodSymbol method, MethodDeclarationSyntax declaration, SemanticModel model, out IParameterSymbol? parameter, out WorkbookUses? uses)
    {
        parameter = null;
        uses = null;
        if (method is not { MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Private, IsVirtual: false, IsAbstract: false, IsOverride: false, IsExtern: false, PartialDefinitionPart: null, PartialImplementationPart: null }
            || method.ContainingType.DeclaringSyntaxReferences.Length != 1 || declaration.Body is null && declaration.ExpressionBody is null)
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

        SyntaxNode root = declaration.SyntaxTree.GetRoot();
        IEnumerable<IdentifierNameSyntax> references = root.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.ValueText == method.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol?.OriginalDefinition, method.OriginalDefinition));
        if (references.FirstOrDefault(r => CallOf(r) is null) is { } group)
        {
            return PatternResult.Manual(EPPlusReason.FlowUseNotMigrated, $"{method.Name} is used as '{Shorten(group.Parent ?? group)}' ({Where(group)}), not invoked: "
                + "a delegate type would name ExcelWorksheet. Change it by hand.");
        }

        var worksheetUses = new WorkbookUses(model, declaration, single);
        if (worksheetUses.Map() is PatternResult unmapped)
        {
            return unmapped;
        }

        parameter = single;
        uses = worksheetUses;
        return null;
    }

    private static IEnumerable<InvocationExpressionSyntax> Invocations(IMethodSymbol method, SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.ValueText == method.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol?.OriginalDefinition, method.OriginalDefinition))
            .Select(CallOf)
            .OfType<InvocationExpressionSyntax>();

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
