using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-309 (queue rows 2 and 3, E9): <c>new ExcelPackage()</c> in a local (using or plain) → <c>new XLWorkbook()</c>, only where every use
/// of the local is one the cell-compare harness proves (<see cref="WorkbookUses"/>): <c>Worksheets.Add("name")</c>, a lookup that provably
/// finds an added sheet (<c>Worksheets["name"]</c>; <c>Worksheets[i]</c> for EPPlus 5+ only, E3), a worksheet's <c>Name</c>, a worksheet local
/// used only for its <c>Name</c>, and <c>SaveAs(new FileInfo(path))</c>. Anything else is Manual with the first use that blocks it.
/// </summary>
public class PackagePattern : ISolutionPattern
{
    private readonly IProjectProperties? _projectProperties;
    private readonly IConfigKeySearch? _configSearch;

    public PackagePattern()
    {
    }

    // Council round 11, E10/E11: packability and the config files decide whether a 4.x integer worksheet index is provable.
    public PackagePattern(IProjectProperties? projectProperties, IConfigKeySearch? configSearch)
    {
        _projectProperties = projectProperties;
        _configSearch = configSearch;
    }

    public const string AddedNamespace = "ClosedXML.Excel";

    public string Id => "epplus.package";

    public string Description => "new ExcelPackage() local with Worksheets.Add/lookup/Name and SaveAs(FileInfo) uses only → new XLWorkbook()";

    // The scope of the package local (the using statement, or the block of a using declaration) when it holds just this package;
    // otherwise the creation itself, which is then Manual.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        List<BaseObjectCreationExpressionSyntax> creations = [.. root.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>().Where(c => IsPackageCreation(c, model))];
        return creations.Select(c => Scope(c) is { } scope && creations.Count(other => scope.Span.Contains(other.Span)) == 1 ? scope : (SyntaxNode)c);
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Rewrite(node, model, WorksheetIndexBase.Unknown($"line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}"));

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        WorksheetIndexBase indexBase = await WorksheetIndexBase.ForAsync(model, solution, _projectProperties, _configSearch,
            $"{node.SyntaxTree.FilePath}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}", cancellationToken);
        return Rewrite(node, model, indexBase);
    }

    private static PatternResult Rewrite(SyntaxNode node, SemanticModel model, WorksheetIndexBase indexBase)
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
            return PatternResult.Manual(EPPlusReason.CellReadDiffers, "new ExcelPackage(...) with arguments opens (or decrypts) an existing workbook. Council round 12, E12: that is Fixed only when every read in the method is Fixed, "
                + "and no cell read is (GetValue<T> differs from ClosedXML for every T on an empty cell, GetValueMatrixTests). Migrate the reads by hand.");
        }

        if (creation.Parent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator }
            || declarator.Parent is not VariableDeclarationSyntax { Variables.Count: 1 } declaration
            || !IsLocal(declaration, out SyntaxNode? scope)
            || model.GetDeclaredSymbol(declarator) is not ILocalSymbol local)
        {
            return PatternResult.Manual("ExcelPackage that is not a local (field, argument, return value); not mapped yet (epplus-queue).");
        }

        var uses = new WorkbookUses(model, scope, local, indexBase);
        if (uses.Map() is PatternResult blocked)
        {
            return blocked;
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

        // E7: ClosedXML accepts SixLabors.Fonts [1.0.0, 3.0.0); 2.x is under the Six Labors Split License.
        if (FontsLicence.Resolved(model) is { Major: >= 2 } fonts)
        {
            return PatternResult.Manual(EPPlusReason.LicensedDependency, $"this project already resolves SixLabors.Fonts {fonts.ToString(3)}, which is under the Six Labors Split License; "
                + "ClosedXML would use it. Pin SixLabors.Fonts below 2.0 (or license it) and run the fix again.");
        }

        SyntaxNode result = scope.TrackNodes(replacements.Keys.Concat(uses.Insertions.Keys));
        var byCurrent = replacements.ToDictionary(r => result.GetCurrentNode(r.Key)!, r => r.Value);
        result = result.ReplaceNodes(byCurrent.Keys, (current, _) => byCurrent[current]);

        foreach ((StatementSyntax statement, List<StatementSyntax> before) in uses.Insertions)
        {
            SyntaxNode current = result.GetCurrentNode(statement)!;
            result = result.InsertNodesBefore(current, before.Select(b => b.WithLeadingTrivia(current.GetLeadingTrivia()).WithTrailingTrivia(current.GetTrailingTrivia())));
        }

        return new FixedResult([result], uses.UsesSystemIO ? [AddedNamespace, "System.IO"] : [AddedNamespace])
        {
            Warning = FontsLicence.Warning,
            Info = uses.UsesIndex ? WorksheetIndexBase.Checked : null,
        };
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
