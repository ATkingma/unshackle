using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// AutoMapper's configuration checks and test mappers → removed once the profiles are hand-written methods:
/// a standalone config.AssertConfigurationIsValid(); a test method (or setup, or a test class's only constructor) left with nothing else;
/// and a local or private field built with new MapperConfiguration(cfg => cfg.AddProfile...) (or its CreateMapper()) that is only
/// checked or mapped with (the Map calls are the Map patterns'). Runs after those patterns, so their rewritten calls no longer count.
/// A configuration that does more than add profiles of the solution, a check with side effects, and a check while a CreateMap outside
/// a profile constructor (or an [AutoMap]) remains, are Manual. A profile-only configuration that escapes (passed on, returned) is left
/// alone: it keeps working with the emptied profiles.
/// </summary>
public class ConfigurationTestPattern : ISolutionPattern
{
    private const string AutoMapperAssembly = "AutoMapper";

    public string Id => "automapper.configuration-test";

    public string Description =>
        "AssertConfigurationIsValid() and test mappers built from profiles (new MapperConfiguration(cfg => cfg.AddProfile<P>()).CreateMapper()) → removed; "
        + "a configuration with its own maps or settings, or a check while maps outside profiles remain, is Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => ConfigurationSyntax.Find(root, model);

    // Without the solution only this project's sources are searched for maps that stay AutoMapper.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        return Problem(node, model) is string problem
            ? PatternResult.Manual(problem)
            : Remove(ChecksConfiguration(node, model)
                ? model.Compilation.SyntaxTrees.Select(t => UnmigratedMap(t.GetRoot(), model.Compilation.GetSemanticModel(t), t.FilePath)).FirstOrDefault(m => m is not null)
                : null);
    }

    // With the solution: a test project checks the profiles of the projects it references.
    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
        if (Problem(node, model) is string problem)
        {
            return PatternResult.Manual(problem);
        }

        return Remove(ChecksConfiguration(node, model) ? await UnmigratedMapAsync(solution, model, cancellationToken) : null);
    }

    // Why a reported configuration or check stays, or null for a node that can go.
    private static string? Problem(SyntaxNode node, SemanticModel model) => node switch
    {
        BaseObjectCreationExpressionSyntax creation => $"new MapperConfiguration stays: {ConfigurationSyntax.ConfigurationProblem(creation, model) ?? "it is used beyond building a test mapper"}. "
            + "Map calls on its mapper would no longer follow it; move its maps into a Profile or migrate it by hand.",
        InvocationExpressionSyntax invocation => $"AssertConfigurationIsValid stays: {ConfigurationSyntax.AssertProblem(invocation, model) ?? "it is not a statement of its own"}; remove the check by hand.",
        _ => null,
    };

    // unmigrated: where a map that stays AutoMapper is ("file:line"), or null.
    private static PatternResult Remove(string? unmigrated) => unmigrated is not null
        ? PatternResult.Manual($"AssertConfigurationIsValid still has maps to check: the map at {unmigrated} is not in a profile constructor this fix rewrites. "
            + "Migrate that map first, then run the fix again.")
        : PatternResult.Fixed([]);

    private static bool ChecksConfiguration(SyntaxNode node, SemanticModel model) =>
        node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(i => ConfigurationSyntax.IsAssert(i, model));

    // The first CreateMap (or [AutoMap]) in the calling project or one it depends on that no profile pattern rewrites.
    private static async Task<string?> UnmigratedMapAsync(Solution solution, SemanticModel model, CancellationToken cancellationToken)
    {
        IEnumerable<Project> projects = solution.GetDocument(model.SyntaxTree)?.Project is { } callProject
            ? solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(callProject.Id).Append(callProject.Id).Select(solution.GetProject).OfType<Project>()
            : solution.Projects;
        foreach (Document document in projects.SelectMany(p => p.Documents))
        {
            string text = (await document.GetTextAsync(cancellationToken)).ToString();
            if ((text.Contains("CreateMap", StringComparison.Ordinal) || text.Contains("CreateProjection", StringComparison.Ordinal) || text.Contains("AutoMap", StringComparison.Ordinal))
                && await document.GetSyntaxRootAsync(cancellationToken) is { } root && await document.GetSemanticModelAsync(cancellationToken) is { } documentModel
                && UnmigratedMap(root, documentModel, document.FilePath ?? document.Name) is string found)
            {
                return found;
            }
        }

        return null;
    }

    // Profile constructors are claimed by the profile patterns (all-or-nothing: Fixed means rewritten); a CreateMap anywhere else
    // (a MapperConfiguration lambda, an extension method on IProfileExpression, an IMapFrom.Mapping method) stays AutoMapper.
    private static string? UnmigratedMap(SyntaxNode root, SemanticModel model, string file)
    {
        SyntaxNode? map = root.DescendantNodes().FirstOrDefault(n => n switch
        {
            InvocationExpressionSyntax invocation => model.GetSymbolInfo(invocation) is var info
                && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "CreateMap" or "CreateProjection", ContainingAssembly.Name: AutoMapperAssembly }
                && !(invocation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() is { Parent: ClassDeclarationSyntax owner }
                    && model.GetDeclaredSymbol(owner) is INamedTypeSymbol type && CreateMapPattern.IsProfile(type)),
            AttributeSyntax attribute => model.GetTypeInfo(attribute).Type is { Name: "AutoMapAttribute", ContainingAssembly.Name: AutoMapperAssembly },
            _ => false,
        });
        return map is null ? null : $"{file}:{map.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
    }
}
