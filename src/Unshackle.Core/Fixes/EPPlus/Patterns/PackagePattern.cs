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
            || !IsLocal(declaration, out SyntaxNode? scope)
            || model.GetDeclaredSymbol(declarator) is not ILocalSymbol local)
        {
            return PatternResult.Manual("ExcelPackage that is not a local (field, argument, return value); not mapped yet (epplus-queue).");
        }

        var uses = new WorkbookUses(model, scope, local);
        if (uses.Map() is string blocked)
        {
            return PatternResult.Manual(blocked);
        }

        Dictionary<SyntaxNode, SyntaxNode> replacements = uses.Replacements;
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
            && IsLocal(declaration, out SyntaxNode scope) ? scope : null;

    // using (var p = new ExcelPackage()) { ... } → the using statement; a using or plain local declaration (E9) → the block it is declared in.
    private static bool IsLocal(VariableDeclarationSyntax declaration, out SyntaxNode scope)
    {
        scope = declaration;
        switch (declaration.Parent)
        {
            case UsingStatementSyntax statement:
                scope = statement;
                return true;
            case LocalDeclarationStatementSyntax { Parent: BlockSyntax block }:
                scope = block;
                return true;
            default:
                return false;
        }
    }
}
