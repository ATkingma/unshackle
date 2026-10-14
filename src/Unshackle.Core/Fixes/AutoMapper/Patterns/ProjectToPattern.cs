using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// query.ProjectTo&lt;B&gt;(mapper.ConfigurationProvider) (and mapper.ProjectTo&lt;B&gt;(query)) on an IQueryable&lt;A&gt; →
/// query.Select(ProfileExtensions.AToBProjection): the Expression&lt;Func&lt;A, B&gt;&gt; that <see cref="CreateMapPattern"/> writes next
/// to ToB for a CreateMap&lt;A, B&gt; that such a call uses (see <see cref="Projections"/>). A call to ToB() cannot be translated by EF Core,
/// an expression with the same member assignments can. ProjectTo with parameters or members to expand, a generic destination and a query
/// that is no IQueryable&lt;A&gt; are Manual. The IMapper or configuration the call came from stays, like for <see cref="MapCallPattern"/>.
/// </summary>
public class ProjectToPattern : ISolutionPattern
{
    private const string AutoMapperAssembly = "AutoMapper";

    public string Id => "automapper.project-to";

    public string Description =>
        "query.ProjectTo<B>(mapper.ConfigurationProvider) → query.Select(ProfileExtensions.AToBProjection), an Expression<Func<A, B>> the profile rewrite writes next to ToB; parameters, members to expand and unknown maps are Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => IsProjectToCall(i, model));

    // Without the solution only a projection the code already has counts.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Check((InvocationExpressionSyntax)node, model, out ExpressionSyntax? query, out ITypeSymbol? source, out ITypeSymbol? destination) is string problem
            ? PatternResult.Manual(problem)
            : Existing((InvocationExpressionSyntax)node, model, query!, source!, destination!) ?? PatternResult.Manual(NoMap(source!, destination!));

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
        var invocation = (InvocationExpressionSyntax)node;
        if (Check(invocation, model, out ExpressionSyntax? query, out ITypeSymbol? source, out ITypeSymbol? destination) is string problem)
        {
            return PatternResult.Manual(problem);
        }

        if (Existing(invocation, model, query!, source!, destination!) is { } existing)
        {
            return existing;
        }

        List<(string Method, string? Namespace, bool IsInternal, ProjectId Project)> generated =
            await MapCallPattern.GeneratedAsync(solution, model, source!, destination!, cancellationToken);
        List<string> profiles = await ProfilesAsync(solution, model, source!, destination!, cancellationToken);
        if (generated.Count == 0 && await MapBlockers.ReasonAsync(solution, model, source!, destination!, cancellationToken) is { } blocker)
        {
            return PatternResult.Manual(blocker);
        }

        if (generated.Count == 0 || profiles.Count == 0)
        {
            return PatternResult.Manual(NoMap(source!, destination!));
        }

        if (generated.Count > 1 || profiles.Count > 1)
        {
            return PatternResult.Manual($"Several profiles map {source!.Name} to {destination!.Name}; choose the projection by hand.");
        }

        (_, string? ns, bool isInternal, ProjectId project) = generated[0];
        if (isInternal && solution.GetDocument(model.SyntaxTree)?.Project.Id != project)
        {
            return PatternResult.Manual($"The projection {Projections.Name(source!, destination!)} becomes internal in another project; write it by hand or make the types public.");
        }

        string? callNamespace = model.GetEnclosingSymbol(node.SpanStart, cancellationToken)?.ContainingNamespace is { IsGlobalNamespace: false } current ? current.ToDisplayString() : null;
        bool imported = ns is null || (callNamespace is not null && (callNamespace == ns || callNamespace.StartsWith(ns + ".", StringComparison.Ordinal)));
        return Fixed(invocation, query!, profiles[0] + "." + Projections.Name(source!, destination!), imported ? ["System.Linq"] : ["System.Linq", ns!]);
    }

    // Every AutoMapper ProjectTo: the extension on IQueryable and IMapper.ProjectTo. CreateMapPattern leaves them to this pattern.
    internal static bool IsProjectToCall(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation) is var info
        && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "ProjectTo" } method
        && method.ContainingAssembly?.Name == AutoMapperAssembly;

    // Every map a rewritable ProjectTo call in the solution uses (MapIntoPattern.Key): those maps get a projection next to ToB.
    internal static async Task<IReadOnlySet<string>> TargetsAsync(Solution solution, CancellationToken cancellationToken)
    {
        HashSet<string> keys = new(StringComparer.Ordinal);
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains("ProjectTo", StringComparison.Ordinal)
                || await document.GetSyntaxRootAsync(cancellationToken) is not { } root
                || await document.GetSemanticModelAsync(cancellationToken) is not { } model)
            {
                continue;
            }

            foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => IsProjectToCall(i, model)))
            {
                if (Check(call, model, out _, out ITypeSymbol? source, out ITypeSymbol? destination) is null)
                {
                    keys.Add(MapIntoPattern.Key(source!, destination!));
                }
            }
        }

        return keys;
    }

    // Why the call cannot become query.Select(projection), or null with the query and the types of the map.
    private static string? Check(InvocationExpressionSyntax invocation, SemanticModel model, out ExpressionSyntax? query, out ITypeSymbol? source, out ITypeSymbol? destination)
    {
        query = null;
        source = null;
        destination = null;
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { TypeArguments: [{ } target] } method
            || invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver })
        {
            return "ProjectTo does not bind to one AutoMapper method called as query.ProjectTo<B>(...); migrate it by hand.";
        }

        // Parameters fill the captured variables of MapFrom lambdas per call, membersToExpand picks the ExplicitExpansion members
        // per call; one fixed projection can do neither.
        if (invocation.ArgumentList.Arguments.Count != 1)
        {
            return "ProjectTo with parameters or members to expand fills in or leaves out members per call; write the projection by hand.";
        }

        ExpressionSyntax dropped;
        if (method.ReducedFrom is not null)
        {
            query = receiver;
            dropped = invocation.ArgumentList.Arguments[0].Expression;
        }
        else
        {
            query = invocation.ArgumentList.Arguments[0].Expression;
            dropped = receiver;
        }

        if (!MapCallPattern.HasNoSideEffects(dropped))
        {
            return "The configuration or mapper of ProjectTo is not a plain field, property or variable and would be dropped; migrate it by hand.";
        }

        if (target is ITypeParameterSymbol or { TypeKind: TypeKind.Error or TypeKind.Dynamic } || target.SpecialType == SpecialType.System_Object)
        {
            // A generic helper (ProjectTo<TResult>(configuration)) projects to whatever the caller asks; no single map is known.
            return $"ProjectTo<{target.Name}> has no destination type of its own (a type parameter, object or dynamic); migrate it by hand.";
        }

        destination = target;
        source = Element(model.GetTypeInfo(query).Type);
        if (source is null || source is ITypeParameterSymbol or { TypeKind: TypeKind.Error or TypeKind.Dynamic } || source.SpecialType == SpecialType.System_Object)
        {
            return $"The query of ProjectTo<{target.Name}> is no IQueryable<T> of a type of its own; AutoMapper picks the map at run time.";
        }

        return null;
    }

    private static ITypeSymbol? Element(ITypeSymbol? type) =>
        type is null ? null : new[] { type }.Concat(type.AllInterfaces)
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault(t => t is { Name: "IQueryable", TypeArguments: [_] } && t.ContainingNamespace?.ToDisplayString() == "System.Linq")
            ?.TypeArguments[0];

    // A static Expression<Func<A, B>> AToBProjection the code already has (also one the profile patterns just wrote in this file).
    private static PatternResult? Existing(InvocationExpressionSyntax node, SemanticModel model, ExpressionSyntax query, ITypeSymbol source, ITypeSymbol destination)
    {
        string name = Projections.Name(source, destination);
        INamedTypeSymbol? within = model.GetEnclosingSymbol(node.SpanStart)?.ContainingType;
        List<IPropertySymbol> declared = [.. model.Compilation.GetSymbolsWithName(name, SymbolFilter.Member).OfType<IPropertySymbol>()
            .Where(p => p.IsStatic && IsProjection(p.Type, source, destination)
                && (within is null ? p.DeclaredAccessibility != Accessibility.Private : model.Compilation.IsSymbolAccessibleWithin(p, within)))];
        if (declared is not [{ ContainingType: { } type }])
        {
            return null;
        }

        string? ns = type.ContainingNamespace is { IsGlobalNamespace: false } typeNamespace ? typeNamespace.ToDisplayString() : null;
        bool visible = model.LookupNamespacesAndTypes(node.SpanStart, name: type.Name).Any(t => SymbolEqualityComparer.Default.Equals(t, type));
        return Fixed(node, query, type.Name + "." + name, visible || ns is null ? ["System.Linq"] : ["System.Linq", ns]);
    }

    private static bool IsProjection(ITypeSymbol type, ITypeSymbol source, ITypeSymbol destination) =>
        type is INamedTypeSymbol { Name: "Expression", TypeArguments: [INamedTypeSymbol { Name: "Func", TypeArguments: [{ } from, { } to] }] }
        && SymbolEqualityComparer.Default.Equals(from, source)
        && SymbolEqualityComparer.Default.Equals(to, destination);

    // The extension class names of the profiles (in the calling project or one it depends on) whose constructor has a CreateMap<A, B>.
    private static async Task<List<string>> ProfilesAsync(Solution solution, SemanticModel model, ITypeSymbol source, ITypeSymbol destination, CancellationToken cancellationToken)
    {
        string key = MapIntoPattern.Key(source, destination);
        IEnumerable<Project> projects = solution.GetDocument(model.SyntaxTree)?.Project is { } callProject
            ? solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(callProject.Id).Append(callProject.Id).Select(solution.GetProject).OfType<Project>()
            : solution.Projects;
        List<string> found = [];
        foreach (Document document in projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains("CreateMap", StringComparison.Ordinal)
                || await document.GetSyntaxRootAsync(cancellationToken) is not { } root
                || await document.GetSemanticModelAsync(cancellationToken) is not { } profileModel)
            {
                continue;
            }

            foreach (ClassDeclarationSyntax profile in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => CreateMapPattern.IsProfileWithMaps(c, profileModel)))
            {
                if (profile.Members.OfType<ConstructorDeclarationSyntax>().SelectMany(c => c.DescendantNodes()).OfType<InvocationExpressionSyntax>()
                    .Any(c => c.Expression is GenericNameSyntax { Identifier.ValueText: "CreateMap" }
                        && profileModel.GetSymbolInfo(c, cancellationToken).Symbol is IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly, TypeArguments: [{ } from, { } to] }
                        && MapIntoPattern.Key(from, to) == key))
                {
                    found.Add(CreateMapPattern.ExtensionsName(profile));
                }
            }
        }

        return found;
    }

    // query.ProjectTo<B>(configuration) → query.Select(projection), keeping the line break and indentation before the dot of a chain.
    private static PatternResult Fixed(InvocationExpressionSyntax node, ExpressionSyntax query, string projection, IReadOnlyList<string> namespaces)
    {
        var access = (MemberAccessExpressionSyntax)node.Expression;
        MemberAccessExpressionSyntax select = access.Expression == query
            ? access.WithName(SyntaxFactory.IdentifierName("Select").WithTriviaFrom(access.Name))
            : SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                MapCallPattern.NeedsParentheses(query.WithoutTrivia()) ? SyntaxFactory.ParenthesizedExpression(query.WithoutTrivia()) : query.WithoutTrivia(),
                SyntaxFactory.IdentifierName("Select"));
        return PatternResult.Fixed(
            node.WithExpression(select)
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.ParseExpression(projection))))
                    .WithTriviaFrom(node.ArgumentList)),
            namespaces);
    }

    private static string NoMap(ITypeSymbol source, ITypeSymbol destination) =>
        $"No CreateMap<{source.Name}, {destination.Name}> in a profile this fix rewrites, and no {Projections.Name(source, destination)} exists; migrate the ProjectTo call by hand.";
}
