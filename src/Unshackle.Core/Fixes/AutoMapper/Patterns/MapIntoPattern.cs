using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// mapper.Map(a, b) (Map&lt;A, B&gt;(A source, B destination)) → a.MapInto(b): the extension method that already exists, or the
/// one <see cref="CreateMapPattern"/> writes next to ToB for a CreateMap&lt;A, B&gt; that such a call uses. MapInto assigns the
/// same members as ToB to the existing object and returns it, like Map does.
/// </summary>
public class MapIntoPattern : ISolutionPattern
{
    private const string AutoMapperAssembly = "AutoMapper";
    private const string MethodName = "MapInto";

    public string Id => "automapper.map-into";

    public string Description =>
        "mapper.Map(a, b) → a.MapInto(b), calling the MapInto that the profile rewrite writes for CreateMap<A, B>; collections, unknown maps and a maybe-null source or destination are Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => IsMapIntoCall(i, model));

    // Without the solution only a MapInto the code already has counts.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Check((InvocationExpressionSyntax)node, model, out ITypeSymbol? sourceType, out ITypeSymbol? destination) is string problem
            ? PatternResult.Manual(problem)
            : Existing((InvocationExpressionSyntax)node, model, sourceType!, destination!) ?? PatternResult.Manual(NoMap(sourceType!, destination!));

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
        var invocation = (InvocationExpressionSyntax)node;
        if (Check(invocation, model, out ITypeSymbol? sourceType, out ITypeSymbol? destination) is string problem)
        {
            return PatternResult.Manual(problem);
        }

        if (Existing(invocation, model, sourceType!, destination!) is { } existing)
        {
            return existing;
        }

        List<(string Method, string? Namespace, bool IsInternal, ProjectId Project)> generated =
            await MapCallPattern.GeneratedAsync(solution, model, sourceType!, destination!, cancellationToken);
        if (generated.Count == 0)
        {
            return PatternResult.Manual(NoMap(sourceType!, destination!));
        }

        if (generated.Count > 1)
        {
            return PatternResult.Manual($"Several profiles map {sourceType!.Name} to {destination!.Name}; choose the mapping method by hand.");
        }

        (_, string? ns, bool isInternal, ProjectId project) = generated[0];
        if (isInternal && solution.GetDocument(model.SyntaxTree)?.Project.Id != project)
        {
            return PatternResult.Manual($"The mapping method {MethodName} becomes internal in another project; call it by hand or make the types public.");
        }

        if (model.LookupSymbols(node.SpanStart, sourceType, MethodName).Any())
        {
            return PatternResult.Manual($"{sourceType!.Name} already has a member {MethodName}; the extension method would not be called.");
        }

        string? callNamespace = model.GetEnclosingSymbol(node.SpanStart, cancellationToken)?.ContainingNamespace is { IsGlobalNamespace: false } current ? current.ToDisplayString() : null;
        bool imported = ns is null || (callNamespace is not null && (callNamespace == ns || callNamespace.StartsWith(ns + ".", StringComparison.Ordinal)));
        return Fixed(invocation, imported ? [] : [ns!]);
    }

    // Map<A, B>(A source, B destination) of AutoMapper; Map<A, B>(source, opts => ...) has a lambda as second parameter.
    internal static bool IsMapIntoCall(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.ArgumentList.Arguments.Count == 2
        && model.GetSymbolInfo(invocation) is var info
        && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "Map", TypeArguments: [_, { } destination], Parameters: [_, { } second] } method
        && SymbolEqualityComparer.Default.Equals(second.Type, destination)
        && method.ContainingAssembly?.Name == AutoMapperAssembly;

    // The key of a map in the set CreateMapPattern gets from TargetsAsync.
    internal static string Key(ITypeSymbol source, ITypeSymbol destination) =>
        source.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "|" + destination.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // Every map a Map(source, destination) call in the solution uses: those maps get a MapInto.
    internal static async Task<IReadOnlySet<string>> TargetsAsync(Solution solution, CancellationToken cancellationToken)
    {
        HashSet<string> keys = new(StringComparer.Ordinal);
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains("Map", StringComparison.Ordinal)
                || await document.GetSyntaxRootAsync(cancellationToken) is not { } root
                || await document.GetSemanticModelAsync(cancellationToken) is not { } model)
            {
                continue;
            }

            foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => IsMapIntoCall(i, model)))
            {
                if (model.GetSymbolInfo(call, cancellationToken).Symbol is IMethodSymbol { TypeArguments: [{ } source, { } destination] })
                {
                    keys.Add(Key(source, destination));
                }
            }
        }

        return keys;
    }

    // Why the call cannot become source.MapInto(destination), or null with the types of the map.
    private static string? Check(InvocationExpressionSyntax invocation, SemanticModel model, out ITypeSymbol? sourceType, out ITypeSymbol? destination)
    {
        sourceType = null;
        destination = null;
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return "Map does not bind to one AutoMapper method; migrate it by hand.";
        }

        sourceType = method.TypeArguments[0];
        destination = method.TypeArguments[1];
        if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || !MapCallPattern.HasNoSideEffects(receiver))
        {
            return "Map is not called as mapper.Map on a plain field, property or variable; migrate it by hand.";
        }

        if (IsUntyped(sourceType) || IsUntyped(destination))
        {
            // Map(object, object) maps the run-time types; the static types do not say which CreateMap runs.
            return "The source or destination of Map has no type of its own (object or dynamic); AutoMapper picks the map at run time.";
        }

        if (MapCallPattern.IsCollection(sourceType) || MapCallPattern.IsCollection(destination))
        {
            return $"Map into {destination.ToDisplayString()} maps a collection (AutoMapper maps each element and fills the existing one); write it by hand.";
        }

        if (model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression).Nullability.FlowState == NullableFlowState.MaybeNull)
        {
            return $"The source of Map may be null: AutoMapper then returns null, {sourceType.Name}.{MethodName}() throws; check for null and migrate it by hand.";
        }

        if (model.GetTypeInfo(invocation.ArgumentList.Arguments[1].Expression).Nullability.FlowState == NullableFlowState.MaybeNull)
        {
            return $"The destination of Map may be null: AutoMapper then creates a new {destination.Name}, {MethodName} throws; check for null and migrate it by hand.";
        }

        return null;
    }

    // A MapInto(this A, B) returning B: one the calling code already sees, or one in this project's source (also the class the profile
    // patterns just wrote, when the profile is in the same file) with a using for its namespace.
    private static PatternResult? Existing(InvocationExpressionSyntax node, SemanticModel model, ITypeSymbol sourceType, ITypeSymbol destination)
    {
        List<ISymbol> visible = [.. model.LookupSymbols(node.SpanStart, sourceType, MethodName, includeReducedExtensionMethods: true)];
        List<IMethodSymbol> matching = [.. visible.OfType<IMethodSymbol>().Where(m => IsMapInto(m.ReducedFrom ?? m, sourceType, destination))];
        if (matching is [{ ReducedFrom: not null }] && model.LookupSymbols(node.SpanStart, sourceType, MethodName).IsEmpty)
        {
            return Fixed(node, []);
        }

        if (matching.Count > 0 || model.LookupSymbols(node.SpanStart, sourceType, MethodName).Any())
        {
            return null;
        }

        INamedTypeSymbol? within = model.GetEnclosingSymbol(node.SpanStart)?.ContainingType;
        List<IMethodSymbol> declared = [.. model.Compilation.GetSymbolsWithName(MethodName, SymbolFilter.Member).OfType<IMethodSymbol>()
            .Where(m => IsMapInto(m, sourceType, destination)
                && (within is null ? m.DeclaredAccessibility != Accessibility.Private : model.Compilation.IsSymbolAccessibleWithin(m, within)))];
        return declared is [{ ContainingNamespace: { IsGlobalNamespace: false } ns }]
            ? Fixed(node, [ns.ToDisplayString()])
            : declared is [_] ? Fixed(node, []) : null;
    }

    private static bool IsMapInto(IMethodSymbol method, ITypeSymbol source, ITypeSymbol destination) =>
        method is { IsExtensionMethod: true, Parameters: [{ } self, { } target] }
        && SymbolEqualityComparer.Default.Equals(self.Type, source)
        && SymbolEqualityComparer.Default.Equals(target.Type, destination)
        && SymbolEqualityComparer.Default.Equals(method.ReturnType, destination);

    private static PatternResult Fixed(InvocationExpressionSyntax node, IReadOnlyList<string> namespaces)
    {
        ExpressionSyntax source = node.ArgumentList.Arguments[0].Expression.WithoutTrivia();
        ExpressionSyntax receiver = MapCallPattern.NeedsParentheses(source) ? SyntaxFactory.ParenthesizedExpression(source) : source;
        return PatternResult.Fixed(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(MethodName)),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(node.ArgumentList.Arguments[1].WithoutTrivia()))),
            namespaces);
    }

    private static string NoMap(ITypeSymbol source, ITypeSymbol destination) =>
        $"No CreateMap<{source.Name}, {destination.Name}> in a profile this fix rewrites, and no {source.Name}.{MethodName}({destination.Name}) exists; migrate the call by hand.";

    private static bool IsUntyped(ITypeSymbol type) =>
        type.TypeKind is TypeKind.Error or TypeKind.Dynamic || type.SpecialType == SpecialType.System_Object;
}
