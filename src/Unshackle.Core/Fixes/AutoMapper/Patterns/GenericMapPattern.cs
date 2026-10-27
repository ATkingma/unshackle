using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Generics;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// mapper.Map&lt;TDto&gt;(entity) where the source or destination is a type parameter of the method or type around it (a generic repository
/// or service base) → a dispatch over the closed set (council round 13, A11): typeof(TDto) == typeof(OrderDto) ? (TDto)(object)entity.ToOrderDto() : …
/// The set comes from <see cref="TypeArgumentClosure"/> (callers, uses and derived types, depth 3). The rewrite is total over that set; the
/// final throw is only reached by a type argument outside it, for which AutoMapper throws a missing-type-map error as well.
/// Otherwise Manual GenericMapOpenSet, naming the unresolved caller or the pair without a map.
/// </summary>
public class GenericMapPattern : ISolutionPattern
{
    private readonly IProjectProperties? _projectProperties;

    // Without project properties (tests, a host without MSBuild) packability is guessed from the compilation (TypeArgumentClosure).
    public GenericMapPattern()
        : this(null)
    {
    }

    // TAAK-324: IsPackable as MSBuild sees it decides whether a public generic member closes its type arguments (council round 10, R1).
    public GenericMapPattern(IProjectProperties? projectProperties)
    {
        _projectProperties = projectProperties;
    }

    public string Id => "automapper.generic-map";

    public string Description =>
        "mapper.Map<T>(x) on a type parameter → typeof(T) == typeof(B) ? (T)(object)x.ToB() : … over the closed type arguments of every caller; open sets are Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => IsGenericCall(i, model));

    // Map<B>(source) or Map<A, B>(source) of AutoMapper whose source or destination type contains a type parameter.
    internal static bool IsGenericCall(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (!MapCallPattern.IsMapCall(invocation, model) || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        ITypeSymbol? source = method.TypeArguments.Length == 2 ? method.TypeArguments[0] : model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression).Type;
        return GenericUses.ContainsTypeParameter(method.TypeArguments[^1]) || (source is not null && GenericUses.ContainsTypeParameter(source));
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Open("the closed type arguments of a type parameter are only known with the whole solution");

    // A call the plan keeps on IMapper (TAAK-272) stays Kept, so the rest may still migrate; it keeps the code of its own open set.
    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        PatternResult own = await DispatchAsync((InvocationExpressionSyntax)node, model, solution, _projectProperties, cancellationToken);
        return await MapOwners.KeptCallAsync(node, model, solution, _projectProperties, cancellationToken) switch
        {
            ManualResult { Code: null } kept when own is ManualResult { Code: nameof(GenericMapCode.GenericMapOpenSet) } => kept with { Code = nameof(GenericMapCode.GenericMapOpenSet) },
            { } kept => kept,
            null => own,
        };
    }

    private static async Task<PatternResult> DispatchAsync(InvocationExpressionSyntax invocation, SemanticModel model, Solution solution, IProjectProperties? projectProperties, CancellationToken cancellationToken)
    {
        if (MapCallPattern.Check(invocation, model, out ExpressionSyntax? source, out ITypeSymbol? sourceType, out ITypeSymbol? destination, out MapCallKind kind) is string problem)
        {
            return PatternResult.Manual(problem);
        }

        if (kind == MapCallKind.Collection)
        {
            return Open($"Map to {destination!.ToDisplayString()} maps a collection over a type parameter; write the Select by hand");
        }

        if (invocation.Parent is ExpressionStatementSyntax)
        {
            return Open("the result of Map is not used; a dispatch over the type arguments is no statement of its own");
        }

        if ((sourceType is not ITypeParameterSymbol && GenericUses.ContainsTypeParameter(sourceType!))
            || (destination is not ITypeParameterSymbol && GenericUses.ContainsTypeParameter(destination!)))
        {
            return Open($"Map<{destination!.ToDisplayString()}> from {sourceType!.ToDisplayString()} has a type parameter inside another generic type; only a plain type parameter is closed");
        }

        List<ITypeParameterSymbol> parameters = [.. new[] { sourceType, destination }.OfType<ITypeParameterSymbol>().Distinct(SymbolEqualityComparer.Default).OfType<ITypeParameterSymbol>()];
        IReadOnlyList<Compilation> compilations = await GenericUses.CompilationsAsync(solution, cancellationToken);
        ClosedSets closed = new TypeArgumentClosure(compilations, solution, projectProperties).Close(parameters);
        string call = $"Map<{destination!.ToDisplayString()}> from {sourceType!.ToDisplayString()}";
        if (closed.Unresolved is string why)
        {
            return Open($"{call}: {why}");
        }

        int position = invocation.SpanStart;
        HashSet<string> namespaces = new(StringComparer.Ordinal);
        List<(string Condition, string Value)> branches = [];
        foreach (IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> set in closed.Sets)
        {
            Dictionary<ITypeParameterSymbol, ITypeSymbol> local = new(SymbolEqualityComparer.Default);
            foreach ((ITypeParameterSymbol parameter, ITypeSymbol argument) in set)
            {
                if (Local(argument, model.Compilation) is not ITypeSymbol mapped)
                {
                    return Open($"{call}: {parameter.Name} is closed with {argument.ToDisplayString()}, which is not a plain type visible from {model.Compilation.AssemblyName}");
                }

                local[parameter] = mapped;
            }

            ITypeSymbol closedSource = sourceType is ITypeParameterSymbol s ? local[s] : sourceType;
            ITypeSymbol closedDestination = destination is ITypeParameterSymbol d ? local[d] : destination;
            if (kind != MapCallKind.Plain && closedDestination.IsValueType)
            {
                return Open($"{call}: the source may be null and {closedDestination.Name} is a value type; check for null and migrate it by hand");
            }

            ExpressionSyntax bare = source!.WithoutTrivia();
            ExpressionSyntax receiver = sourceType is ITypeParameterSymbol
                ? SyntaxFactory.ParseExpression($"({closedSource.ToMinimalDisplayString(model, position)})(object){(MapCallPattern.NeedsParentheses(bare) ? $"({bare})" : bare.ToString())}")
                : bare;
            PatternResult result = await MapCallPattern.SingleAsync(invocation, model, solution, receiver, closedSource, closedDestination, kind, projectProperties, cancellationToken);
            if (result is not FixedResult { NewNodes: [ExpressionSyntax mappedCall] } fixedResult)
            {
                return Open($"{call}: for {closedSource.Name} → {closedDestination.Name}: {(result as ManualResult)?.Reason ?? "no generated map"}");
            }

            namespaces.UnionWith(fixedResult.RequiredNamespaces);
            string value = mappedCall.WithoutTrivia().ToString();
            if (destination is ITypeParameterSymbol target)
            {
                value = $"({target.Name})(object){(mappedCall is InvocationExpressionSyntax or ParenthesizedExpressionSyntax ? value : $"({value})")}";
            }

            branches.Add((string.Join(" && ", parameters.Select(p => $"typeof({p.Name}) == typeof({local[p].ToMinimalDisplayString(model, position)})")), value));
        }

        string message = $"$\"No generated map for {string.Join(", ", parameters.Select(p => $"{{typeof({p.Name})}}"))}.\"";
        // One branch per line, indented one level below the line of the call.
        string line = invocation.SyntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(position).ToString();
        string eol = invocation.SyntaxTree.GetText(cancellationToken).ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string next = eol + new string([.. line.TakeWhile(char.IsWhiteSpace)]) + "    : ";
        string text = string.Join(next, branches.Select(b => $"{b.Condition} ? {b.Value}")) + next + $"throw new global::System.InvalidOperationException({message})";
        ExpressionSyntax rewritten = SyntaxFactory.ParseExpression(text);
        if (invocation.Parent is not (ArgumentSyntax or EqualsValueClauseSyntax or ArrowExpressionClauseSyntax or ReturnStatementSyntax or AssignmentExpressionSyntax))
        {
            rewritten = SyntaxFactory.ParenthesizedExpression(rewritten);
        }

        return PatternResult.Fixed(rewritten, [.. namespaces.Order(StringComparer.Ordinal)]);
    }

    // The closed type as the compilation of the Map call sees it, or null when it is not a plain (non-generic) type visible there.
    private static ITypeSymbol? Local(ITypeSymbol type, Compilation compilation)
    {
        if (type.SpecialType != SpecialType.None && type is INamedTypeSymbol { IsGenericType: false })
        {
            return compilation.GetSpecialType(type.SpecialType);
        }

        return type is INamedTypeSymbol { IsGenericType: false, TypeKind: not TypeKind.Error } named
            && compilation.GetTypeByMetadataName(GenericUses.MetadataKey(named)) is INamedTypeSymbol local
            && compilation.IsSymbolAccessibleWithin(local, compilation.Assembly)
                ? local
                : null;
    }

    private static PatternResult Open(string reason) =>
        PatternResult.Manual(GenericMapCode.GenericMapOpenSet, reason.EndsWith('.') ? reason : reason + ".");
}
