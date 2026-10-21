using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// services.AddAutoMapper(...); → statement removed, once nothing in the solution asks DI for a mapper any more.
/// An IMapper that is still injected (MapCallPattern leaves it) would fail to resolve at run time, so that is Manual.
/// </summary>
public class AddAutoMapperPattern : ISolutionPattern
{
    private const string AutoMapperAssembly = "AutoMapper";

    // The services AddAutoMapper registers; any of them still named in code needs the registration.
    private static readonly string[] s_services =
        ["AutoMapper.IMapper", "AutoMapper.IMapperBase", "AutoMapper.IRuntimeMapper", "AutoMapper.IConfigurationProvider"];

    public string Id => "automapper.add-automapper";

    public string Description =>
        "services.AddAutoMapper(...); → removed when no IMapper (or IConfigurationProvider) is used anywhere; otherwise, or when the result is used, Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i =>
            model.GetSymbolInfo(i) is var info
            && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "AddAutoMapper" } method
            && method.ContainingAssembly?.Name == AutoMapperAssembly)
            // A top-level statement goes with its GlobalStatement: the ExpressionStatement alone is not an element of a list.
            .Select(i => i.Parent is ExpressionStatementSyntax statement ? statement.Parent is GlobalStatementSyntax global ? global : statement : (SyntaxNode)i);

    // Without the solution only this project's sources are checked for a mapper that is still used.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        string? user = model.Compilation.SyntaxTrees
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(n => s_services.Any(s => s.EndsWith("." + n.Identifier.ValueText, StringComparison.Ordinal)))
                .Where(n => IsService(model.Compilation.GetSemanticModel(t).GetSymbolInfo(n).Symbol))
                .Select(n => Location(t.FilePath, n.GetLocation())))
            .FirstOrDefault();
        return Rewrite(node, model, user);
    }

    // With the solution every project counts: a test resolving IMapper from AddSampleShop() needs the registration too.
    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        // A use in code that UnusedMapperPattern removes (TAAK-289) does not count.
        MapperCleanup cleanup = await MapperCleanup.ForAsync(solution, model, cancellationToken);
        foreach (INamedTypeSymbol service in s_services.Select(model.Compilation.GetTypeByMetadataName).OfType<INamedTypeSymbol>())
        {
            IEnumerable<ReferencedSymbol> references = await SymbolFinder.FindReferencesAsync(service, solution, cancellationToken);
            foreach (ReferenceLocation location in references.SelectMany(r => r.Locations))
            {
                SyntaxNode root = await location.Document.GetSyntaxRootAsync(cancellationToken) ?? throw new InvalidOperationException("No syntax root.");
                SemanticModel locationModel = await location.Document.GetSemanticModelAsync(cancellationToken) ?? throw new InvalidOperationException("No semantic model.");
                if (!cleanup.Removes(root.FindNode(location.Location.SourceSpan, getInnermostNodeForTie: true), locationModel))
                {
                    return Rewrite(node, model, Location(location.Document.FilePath ?? location.Document.Name, location.Location));
                }
            }
        }

        return Rewrite(node, model, null);
    }

    // user: where a mapper is still used ("file:line"), or null.
    private static PatternResult Rewrite(SyntaxNode node, SemanticModel model, string? user)
    {
        if ((node is GlobalStatementSyntax global ? global.Statement : node) is not ExpressionStatementSyntax statement)
        {
            return PatternResult.KeptManual(KeptReason.RegistrationResultUsed, "the result of AddAutoMapper is used (not a statement on its own), so the registration stays; remove it by hand.");
        }

        if (user is not null)
        {
            // Safe to keep (TAAK-272): the registration still works with trimmed or emptied profiles.
            return PatternResult.KeptManual(KeptReason.MapperStillUsed,
                $"IMapper is still used ({user}), so AddAutoMapper stays (without it IMapper no longer resolves from DI). "
                + "To remove it, remove the IMapper fields, constructor parameters and lookups (and their arguments at the callers), then run the fix again.");
        }

        // "_ = p;" keeps the signature and its callers intact when a parameter was only passed to AddAutoMapper (IDE0060).
        return PatternResult.Fixed([.. UnusedParameters((InvocationExpressionSyntax)statement.Expression, model)
            .Select(p => SyntaxFactory.ParseStatement($"_ = {p};"))]);
    }

    private static List<string> UnusedParameters(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (invocation.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>() is not { } method)
        {
            return [];
        }

        List<IdentifierNameSyntax> references = [.. method.DescendantNodes().OfType<IdentifierNameSyntax>()];
        return [.. invocation.ArgumentList.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Select(i => model.GetSymbolInfo(i).Symbol)
            .OfType<IParameterSymbol>()
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<IParameterSymbol>()
            .Where(p => references.All(r => invocation.ArgumentList.Span.Contains(r.Span)
                || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(r).Symbol, p)))
            .Select(p => p.Name)];
    }

    private static bool IsService(ISymbol? symbol) =>
        symbol is INamedTypeSymbol type && type.ContainingAssembly?.Name == AutoMapperAssembly && s_services.Contains(type.ToDisplayString(), StringComparer.Ordinal);

    private static string Location(string file, Location location) => $"{file}:{location.GetLineSpan().StartLinePosition.Line + 1}";
}
