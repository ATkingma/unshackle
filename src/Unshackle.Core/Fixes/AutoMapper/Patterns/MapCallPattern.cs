using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// mapper.Map&lt;B&gt;(a) and mapper.Map&lt;A, B&gt;(a) → a.ToB(): the extension method that already exists, or the one
/// <see cref="CreateMapPattern"/> (or a ForMember pattern) writes for a CreateMap&lt;A, B&gt; in a profile of the solution.
/// The IMapper the call came from stays: removing a constructor parameter changes every caller.
/// </summary>
public class MapCallPattern : ISolutionPattern
{
    private const string AutoMapperAssembly = "AutoMapper";

    // Rewrite is the same for every profile pattern; only Find differs (which one claims the profile).
    private readonly CreateMapPattern _profiles;

    public MapCallPattern()
    {
        _profiles = new CreateMapPattern();
    }

    public string Id => "automapper.map-call";

    public string Description =>
        "mapper.Map<B>(a) → a.ToB(), calling the extension method of CreateMap<A, B>; collections, unknown maps and a maybe-null source are Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => IsMapCall(i, model));

    // Without the solution only an extension method the code already has counts.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Check((InvocationExpressionSyntax)node, model, out ExpressionSyntax? source, out ITypeSymbol? sourceType, out ITypeSymbol? destination) is string problem
            ? PatternResult.Manual(problem)
            : Existing(node, model, sourceType!, destination!, source!) ?? PatternResult.Manual(NoMap(sourceType!, destination!));

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
        if (Check((InvocationExpressionSyntax)node, model, out ExpressionSyntax? source, out ITypeSymbol? sourceType, out ITypeSymbol? destination) is string problem)
        {
            return PatternResult.Manual(problem);
        }

        if (Existing(node, model, sourceType!, destination!, source!) is { } existing)
        {
            return existing;
        }

        List<(string Method, string? Namespace, bool IsInternal, ProjectId Project)> generated =
            await GeneratedAsync(solution, model, sourceType!, destination!, cancellationToken);
        if (generated.Count == 0)
        {
            return PatternResult.Manual(NoMap(sourceType!, destination!));
        }

        if (generated.Count > 1)
        {
            return PatternResult.Manual($"Several profiles map {sourceType!.Name} to {destination!.Name}; choose the mapping method by hand.");
        }

        (string method, string? ns, bool isInternal, ProjectId project) = generated[0];
        ProjectId? callProject = solution.GetDocument(model.SyntaxTree)?.Project.Id;
        if (isInternal && callProject != project)
        {
            return PatternResult.Manual($"The mapping method {method} becomes internal in another project; call it by hand or make the types public.");
        }

        if (model.LookupSymbols(node.SpanStart, sourceType, method).Any())
        {
            return PatternResult.Manual($"{sourceType!.Name} already has a member {method}; the extension method would not be called.");
        }

        string? callNamespace = model.GetEnclosingSymbol(node.SpanStart, cancellationToken)?.ContainingNamespace is { IsGlobalNamespace: false } current ? current.ToDisplayString() : null;
        bool imported = ns is null || (callNamespace is not null && (callNamespace == ns || callNamespace.StartsWith(ns + ".", StringComparison.Ordinal)));
        return Fixed(node, source!, method, imported ? [] : [ns!]);
    }

    // Map<B>(source) or Map<A, B>(source) of AutoMapper; Map(source, destination), Map with options and ProjectTo stay with CreateMapPattern.
    internal static bool IsMapCall(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.ArgumentList.Arguments.Count == 1
        && model.GetSymbolInfo(invocation) is var info
        && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "Map", TypeArguments.Length: 1 or 2, Parameters.Length: 1 } method
        && method.ContainingAssembly?.Name == AutoMapperAssembly;

    // Why the call cannot become source.ToB(), or null with the source expression and the types of the map.
    private static string? Check(InvocationExpressionSyntax invocation, SemanticModel model, out ExpressionSyntax? source, out ITypeSymbol? sourceType,
        out ITypeSymbol? destination)
    {
        source = invocation.ArgumentList.Arguments[0].Expression;
        sourceType = null;
        destination = null;
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return "Map does not bind to one AutoMapper method; migrate it by hand.";
        }

        destination = method.TypeArguments[^1];
        sourceType = method.TypeArguments.Length == 2 ? method.TypeArguments[0] : model.GetTypeInfo(source).Type;
        if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || !HasNoSideEffects(receiver))
        {
            return "Map is not called as mapper.Map on a plain field, property or variable; migrate it by hand.";
        }

        if (sourceType is null or { TypeKind: TypeKind.Error or TypeKind.Dynamic } || sourceType.SpecialType == SpecialType.System_Object)
        {
            // Map<B>(object) maps the run-time type; the static type does not say which CreateMap runs.
            return "The source of Map has no type of its own (object or dynamic); AutoMapper picks the map at run time.";
        }

        if (IsCollection(sourceType) || IsCollection(destination))
        {
            return $"Map to {destination.ToDisplayString()} maps a collection (AutoMapper maps each element into a new one); write it by hand, e.g. with Select(x => x.To…()).";
        }

        if (model.GetTypeInfo(source).Nullability.FlowState == NullableFlowState.MaybeNull)
        {
            return $"The source of Map may be null: AutoMapper returns null, {sourceType.Name}.To{destination.Name}() throws; check for null and migrate it by hand.";
        }

        return null;
    }

    // An extension method To<B>() on A returning B: one the calling code already sees, or one in this project's source (also the class
    // the profile patterns just wrote, when the profile is in the same file) with a using for its namespace.
    private static PatternResult? Existing(SyntaxNode node, SemanticModel model, ITypeSymbol sourceType, ITypeSymbol destination, ExpressionSyntax source)
    {
        string name = "To" + destination.Name;
        List<IMethodSymbol> visible = [.. model.LookupSymbols(node.SpanStart, sourceType, name, includeReducedExtensionMethods: true)
            .OfType<IMethodSymbol>().Where(m => m.Parameters.Length == 0 && SymbolEqualityComparer.Default.Equals(m.ReturnType, destination))];
        if (visible is [{ ReducedFrom: not null }])
        {
            return Fixed(node, source, name, []);
        }

        if (visible.Count > 0 || model.LookupSymbols(node.SpanStart, sourceType, name).Any())
        {
            return null;
        }

        INamedTypeSymbol? within = model.GetEnclosingSymbol(node.SpanStart)?.ContainingType;
        List<IMethodSymbol> declared = [.. model.Compilation.GetSymbolsWithName(name, SymbolFilter.Member).OfType<IMethodSymbol>()
            .Where(m => m is { IsExtensionMethod: true, Parameters: [{ } self] } && SymbolEqualityComparer.Default.Equals(self.Type, sourceType)
                && SymbolEqualityComparer.Default.Equals(m.ReturnType, destination)
                && (within is null ? m.DeclaredAccessibility != Accessibility.Private : model.Compilation.IsSymbolAccessibleWithin(m, within)))];
        return declared is [{ ContainingNamespace: { IsGlobalNamespace: false } ns }]
            ? Fixed(node, source, name, [ns.ToDisplayString()])
            : declared is [_] ? Fixed(node, source, name, []) : null;
    }

    // Every CreateMap<A, B> in a profile the profile patterns rewrite, in the calling project or one it depends on: the generated method,
    // the namespace of its class (null: global) and whether it becomes internal.
    private async Task<List<(string Method, string? Namespace, bool IsInternal, ProjectId Project)>> GeneratedAsync(Solution solution, SemanticModel model,
        ITypeSymbol sourceType, ITypeSymbol destination, CancellationToken cancellationToken)
    {
        string sourceName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string destinationName = destination.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        IEnumerable<Project> projects = solution.GetDocument(model.SyntaxTree)?.Project is { } callProject
            ? solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(callProject.Id).Append(callProject.Id).Select(solution.GetProject).OfType<Project>()
            : solution.Projects;
        List<(string Method, string? Namespace, bool IsInternal, ProjectId Project)> found = [];
        foreach (Project project in projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            foreach (Document document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains("CreateMap", StringComparison.Ordinal) || await document.GetSyntaxRootAsync(cancellationToken) is not { } root
                    || await document.GetSemanticModelAsync(cancellationToken) is not { } profileModel)
                {
                    continue;
                }

                foreach (ClassDeclarationSyntax profile in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => CreateMapPattern.IsProfileWithMaps(c, profileModel)))
                {
                    if (_profiles.Rewrite(profile, profileModel) is not FixedResult { NewNodes: [ClassDeclarationSyntax rewritten, ClassDeclarationSyntax extensions] }
                        || rewritten.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault()?.Body is null)
                    {
                        continue;
                    }

                    // One generated method per CreateMap statement, in the same order.
                    List<MethodDeclarationSyntax> methods = [.. extensions.Members.OfType<MethodDeclarationSyntax>()];
                    List<StatementSyntax> statements = [.. profile.Members.OfType<ConstructorDeclarationSyntax>().Single().Body!.Statements];
                    for (int i = 0; i < statements.Count && i < methods.Count; i++)
                    {
                        InvocationExpressionSyntax? createMap = statements[i].DescendantNodes().OfType<InvocationExpressionSyntax>()
                            .LastOrDefault(c => c.Expression is GenericNameSyntax { Identifier.ValueText: "CreateMap" });
                        if (createMap is not null && profileModel.GetSymbolInfo(createMap, cancellationToken).Symbol is IMethodSymbol { TypeArguments: [{ } from, { } to] }
                            && from.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceName
                            && to.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == destinationName)
                        {
                            INamespaceSymbol? ns = profileModel.GetDeclaredSymbol(profile, cancellationToken)?.ContainingNamespace;
                            found.Add((methods[i].Identifier.ValueText, ns is null or { IsGlobalNamespace: true } ? null : ns.ToDisplayString(),
                                extensions.Modifiers.Any(SyntaxKind.InternalKeyword), project.Id));
                        }
                    }
                }
            }
        }

        return found;
    }

    private static PatternResult Fixed(SyntaxNode node, ExpressionSyntax source, string method, IReadOnlyList<string> namespaces)
    {
        ExpressionSyntax receiver = source.WithoutTrivia() is var bare && NeedsParentheses(bare) ? SyntaxFactory.ParenthesizedExpression(bare) : bare;
        return PatternResult.Fixed(
            SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(method))),
            namespaces);
    }

    private static string NoMap(ITypeSymbol source, ITypeSymbol destination) =>
        $"No CreateMap<{source.Name}, {destination.Name}> in a profile this fix rewrites, and no {source.Name}.To{destination.Name}() exists; migrate the call by hand.";

    // The receiver is dropped: only a name, this.name or a chain of names may go.
    private static bool HasNoSideEffects(ExpressionSyntax receiver) => receiver switch
    {
        IdentifierNameSyntax or ThisExpressionSyntax or BaseExpressionSyntax or PredefinedTypeSyntax => true,
        MemberAccessExpressionSyntax { Expression: var inner } => HasNoSideEffects(inner),
        ParenthesizedExpressionSyntax { Expression: var inner } => HasNoSideEffects(inner),
        _ => false,
    };

    private static bool NeedsParentheses(ExpressionSyntax expression) => expression is not (IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
        or ElementAccessExpressionSyntax or ParenthesizedExpressionSyntax or ThisExpressionSyntax or LiteralExpressionSyntax or ObjectCreationExpressionSyntax
        or GenericNameSyntax or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression });

    private static bool IsCollection(ITypeSymbol type) =>
        type.SpecialType != SpecialType.System_String && (type is IArrayTypeSymbol || type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable));
}
