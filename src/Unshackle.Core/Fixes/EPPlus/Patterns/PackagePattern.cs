using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-309 (queue row 2): <c>new ExcelPackage()</c> in a using local → <c>new XLWorkbook()</c>, only where every use of the local is one
/// the cell-compare harness proves: <c>p.Workbook.Worksheets.Add("name");</c> as a statement (→ <c>p.Worksheets.Add("name");</c>) and
/// <c>p.SaveAs(new FileInfo(path));</c> (→ <c>p.SaveAs(path);</c>). Any other use, a constructor argument, or a package outside a using
/// local is Manual with the first use that blocks it; later queue rows widen the set of uses.
/// </summary>
public class PackagePattern : IPattern
{
    public const string AddedNamespace = "ClosedXML.Excel";

    // Excel's sheet-name rules; a name breaking them throws in both libraries, but not provably with the same exception.
    private static readonly System.Buffers.SearchValues<char> s_invalidSheetNameCharacters = System.Buffers.SearchValues.Create(@"[]:*?/\");

    public string Id => "epplus.package";

    public string Description => "using var p = new ExcelPackage() with Worksheets.Add/SaveAs(FileInfo) uses only → new XLWorkbook()";

    // The scope of the package local (the using statement, or the block of a using declaration) when it holds just this package;
    // otherwise the creation itself, which is then Manual.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        List<BaseObjectCreationExpressionSyntax> creations = [.. root.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>().Where(c => IsPackageCreation(c, model))];
        return creations.Select(c => Scope(c) is { } scope && creations.Count(other => scope.Span.Contains(other.Span)) == 1 ? scope : (SyntaxNode)c);
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is not BaseObjectCreationExpressionSyntax creation)
        {
            creation = node.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>().First(c => IsPackageCreation(c, model));
        }
        else if (Scope(creation) is not null)
        {
            return PatternResult.Manual("Several ExcelPackage locals in one scope (nested or in the same block); not mapped yet (epplus-queue).");
        }

        if (creation.ArgumentList is { Arguments.Count: > 0 })
        {
            return PatternResult.Manual("new ExcelPackage(...) with arguments (a file, stream or password) opens or decrypts a workbook; not mapped yet (epplus-queue).");
        }

        if (creation.Parent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator }
            || declarator.Parent is not VariableDeclarationSyntax { Variables.Count: 1 } declaration
            || !IsUsingLocal(declaration, out SyntaxNode? scope)
            || model.GetDeclaredSymbol(declarator) is not ILocalSymbol local)
        {
            return PatternResult.Manual("ExcelPackage that is not a using local (field, argument, return value or a local without using); not mapped yet (epplus-queue).");
        }

        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
        foreach (IdentifierNameSyntax use in scope.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol, local)))
        {
            if (Replacement(use, model) is not (SyntaxNode old, SyntaxNode replacement))
            {
                int line = use.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                return PatternResult.Manual($"ExcelPackage local {local.Name} is used as '{Shorten(use.Parent ?? use)}' (line {line}), which is not mapped yet (epplus-queue).");
            }

            replacements[old] = replacement;
        }

        TypeSyntax workbook = SyntaxFactory.ParseTypeName("XLWorkbook");
        replacements[creation] = creation is ObjectCreationExpressionSyntax explicitCreation
            ? explicitCreation.WithType(workbook.WithTriviaFrom(explicitCreation.Type))
            : creation;
        if (!declaration.Type.IsVar)
        {
            replacements[declaration.Type] = workbook.WithTriviaFrom(declaration.Type);
        }

        return PatternResult.Fixed(scope.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]), AddedNamespace);
    }

    private static bool IsPackageCreation(BaseObjectCreationExpressionSyntax creation, SemanticModel model) => creation switch
    {
        ObjectCreationExpressionSyntax { Type: var type } => EPPlusSyntax.IsPackageType(type, model),
        _ => EPPlusSyntax.IsPackageType(model.GetTypeInfo(creation).Type),
    };

    private static SyntaxNode? Scope(BaseObjectCreationExpressionSyntax creation) =>
        creation.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } }
            && IsUsingLocal(declaration, out SyntaxNode scope) ? scope : null;

    // using (var p = new ExcelPackage()) { ... } → the using statement; using var p = new ExcelPackage(); → the block it is declared in.
    private static bool IsUsingLocal(VariableDeclarationSyntax declaration, out SyntaxNode scope)
    {
        scope = declaration;
        switch (declaration.Parent)
        {
            case UsingStatementSyntax statement:
                scope = statement;
                return true;
            case LocalDeclarationStatementSyntax { UsingKeyword.RawKind: (int)SyntaxKind.UsingKeyword, Parent: BlockSyntax block }:
                scope = block;
                return true;
            default:
                return false;
        }
    }

    // The node of one use of the package local and what replaces it, or null when the use is not mapped.
    private static (SyntaxNode Old, SyntaxNode New)? Replacement(IdentifierNameSyntax use, SemanticModel model)
    {
        // p.Workbook.Worksheets.Add("name"); → p.Worksheets.Add("name"); — the result unused (a used worksheet is a later queue row).
        if (use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Workbook" } workbook
            && workbook.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Worksheets" } worksheets
            && worksheets.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add" } add
            && add.Parent is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax name }], Parent: ExpressionStatementSyntax }
            && name.IsKind(SyntaxKind.StringLiteralExpression) && IsValidSheetName(name.Token.ValueText))
        {
            return (worksheets, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, use, worksheets.Name));
        }

        // p.SaveAs(new FileInfo(path)); → p.SaveAs(path);
        if (use.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "SaveAs" } saveAs
            && saveAs.Parent is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var path] } file }], Parent: ExpressionStatementSyntax } invocation
            && model.GetTypeInfo(file).Type is INamedTypeSymbol { Name: "FileInfo", ContainingNamespace: { Name: "IO", ContainingNamespace.Name: "System" } }
            && model.GetTypeInfo(path.Expression).Type is { SpecialType: SpecialType.System_String })
        {
            return (invocation.ArgumentList, SyntaxFactory.ArgumentList([path.WithoutTrivia()]).WithTriviaFrom(invocation.ArgumentList));
        }

        return null;
    }

    private static bool IsValidSheetName(string name) =>
        name.Length is > 0 and <= 31 && name.IndexOfAny(s_invalidSheetNameCharacters) < 0 && name[0] != '\'' && name[^1] != '\'';

    private static string Shorten(SyntaxNode node)
    {
        string text = node.ToString().ReplaceLineEndings(" ");
        return text.Length <= 60 ? text : text[..57] + "...";
    }
}
