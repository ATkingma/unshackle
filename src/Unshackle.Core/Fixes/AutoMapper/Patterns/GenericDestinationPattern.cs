using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Generics;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// query.ProjectTo&lt;TResult&gt;(configuration) where TResult is a type parameter of the method or type around it (a generic paging or query helper)
/// → query.Select(selector), where selector is chosen by typeof before the query is built (council round 13, A12):
/// typeof(TResult) == typeof(OrderDto) ? (Expression&lt;Func&lt;T, TResult&gt;&gt;)(object)OrderMapping.OrderToOrderDtoProjection : … : throw.
/// The conditional is evaluated in C# and hands Queryable.Select the same expression tree the closed ProjectTo rewrite does; it never enters the
/// IQueryable pipeline. The set comes from <see cref="TypeArgumentClosure"/> (as for <see cref="GenericMapPattern"/>, A11); every closed pair needs a
/// Fixed projection of <see cref="ProjectToPattern"/> (one the code has, or one the profile writes for a non-generic ProjectTo of the pair).
/// Otherwise Manual GenericProjectionOpenSet.
/// </summary>
public class GenericDestinationPattern : ISolutionPattern
{
    private readonly IProjectProperties? _projectProperties;

    // Without project properties (tests, a host without MSBuild) packability is guessed from the compilation (TypeArgumentClosure).
    public GenericDestinationPattern()
        : this(null)
    {
    }

    // IsPackable as MSBuild sees it decides whether a public generic member closes its type arguments (council round 10, R1).
    public GenericDestinationPattern(IProjectProperties? projectProperties)
    {
        _projectProperties = projectProperties;
    }

    public string Id => "automapper.generic-destination";

    public string Description =>
        "query.ProjectTo<T>(configuration) on a type parameter → query.Select(typeof(T) == typeof(B) ? (Expression<Func<A, T>>)(object)AToBProjection : …) over the closed type arguments of every caller; open sets are Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => IsGenericCall(i, model));

    // ProjectTo<T> of AutoMapper whose destination is a type parameter; every other ProjectTo is ProjectToPattern's.
    internal static bool IsGenericCall(InvocationExpressionSyntax invocation, SemanticModel model) =>
        ProjectToPattern.IsProjectToCall(invocation, model)
        && model.GetSymbolInfo(invocation) is var info
        && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { TypeArguments: [ITypeParameterSymbol] };

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Open("the closed type arguments of a type parameter are only known with the whole solution");

    // A call the plan keeps on IMapper (TAAK-272) stays Kept; it keeps the code of its own open set.
    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        PatternResult own = await DispatchAsync((InvocationExpressionSyntax)node, model, solution, _projectProperties, cancellationToken);
        return await MapOwners.KeptCallAsync(node, model, solution, _projectProperties, cancellationToken) switch
        {
            ManualResult { Code: null } kept when own is ManualResult { Code: nameof(GenericDestinationCode.GenericProjectionOpenSet) } =>
                kept with { Code = nameof(GenericDestinationCode.GenericProjectionOpenSet) },
            { } kept => kept,
            null => own,
        };
    }

    private static async Task<PatternResult> DispatchAsync(InvocationExpressionSyntax invocation, SemanticModel model, Solution solution, IProjectProperties? projectProperties,
        CancellationToken cancellationToken)
    {
        if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol { TypeArguments: [ITypeParameterSymbol destination] } method
            || invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver })
        {
            return PatternResult.Manual("ProjectTo does not bind to one AutoMapper method called as query.ProjectTo<T>(...); migrate it by hand.");
        }

        // Parameters and members to expand change the projection per call; over a type parameter that is not measured (conservative, TAAK-325).
        if (invocation.ArgumentList.Arguments.Count != 1)
        {
            return Open($"ProjectTo<{destination.Name}> with parameters or members to expand fills in or leaves out members per call; write the projection by hand");
        }

        (ExpressionSyntax query, ExpressionSyntax dropped) = method.ReducedFrom is not null
            ? (receiver, invocation.ArgumentList.Arguments[0].Expression)
            : (invocation.ArgumentList.Arguments[0].Expression, receiver);
        if (!MapCallPattern.HasNoSideEffects(dropped))
        {
            return PatternResult.Manual("The configuration or mapper of ProjectTo is not a plain field, property or variable and would be dropped; migrate it by hand.");
        }

        ITypeSymbol? source = ProjectToPattern.Element(model.GetTypeInfo(query, cancellationToken).Type);
        string call = $"ProjectTo<{destination.Name}> from {source?.ToDisplayString() ?? "?"}";
        if (source is null || source.TypeKind is TypeKind.Error or TypeKind.Dynamic || source.SpecialType == SpecialType.System_Object
            || (source is not ITypeParameterSymbol && GenericUses.ContainsTypeParameter(source)))
        {
            return Open($"{call}: the query is no IQueryable<T> of a plain type or type parameter; AutoMapper picks the map at run time");
        }

        List<ITypeParameterSymbol> parameters = [.. new ITypeSymbol[] { source, destination }.OfType<ITypeParameterSymbol>().Distinct(SymbolEqualityComparer.Default).OfType<ITypeParameterSymbol>()];
        IReadOnlyList<Compilation> compilations = await GenericUses.CompilationsAsync(solution, cancellationToken);
        ClosedSets closed = new TypeArgumentClosure(compilations, solution, projectProperties).Close(parameters);
        if (closed.Unresolved is string why)
        {
            return Open($"{call}: {why}");
        }

        int position = invocation.SpanStart;
        string expressionType = $"Expression<Func<{source.ToMinimalDisplayString(model, position)}, {destination.Name}>>";
        HashSet<string> namespaces = new(StringComparer.Ordinal) { "System", "System.Linq", "System.Linq.Expressions" };
        List<(string Condition, string Value)> branches = [];
        foreach (IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> set in closed.Sets)
        {
            Dictionary<ITypeParameterSymbol, ITypeSymbol> local = new(SymbolEqualityComparer.Default);
            foreach ((ITypeParameterSymbol parameter, ITypeSymbol argument) in set)
            {
                if (GenericMapPattern.Local(argument, model.Compilation) is not ITypeSymbol mapped)
                {
                    return Open($"{call}: {parameter.Name} is closed with {argument.ToDisplayString()}, which is not a plain type visible from {model.Compilation.AssemblyName}");
                }

                local[parameter] = mapped;
            }

            ITypeSymbol closedSource = source is ITypeParameterSymbol s ? local[s] : source;
            ITypeSymbol closedDestination = local[destination];
            (string? problem, string projection, IReadOnlyList<string> needed) =
                await ProjectToPattern.ProjectionAsync(invocation, model, solution, closedSource, closedDestination, [], requireTarget: true, projectProperties, cancellationToken);
            if (problem is not null)
            {
                return Open($"{call}: for {closedSource.Name} → {closedDestination.Name}: {problem.TrimEnd('.')}");
            }

            namespaces.UnionWith(needed);
            branches.Add((string.Join(" && ", parameters.Select(p => $"typeof({p.Name}) == typeof({local[p].ToMinimalDisplayString(model, position)})")), $"({expressionType})(object){projection}"));
        }

        string message = $"$\"No generated projection for {string.Join(", ", parameters.Select(p => $"{{typeof({p.Name})}}"))}.\"";
        // One branch per line, indented one level below the line of the call.
        string line = invocation.SyntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(invocation.ArgumentList.SpanStart).ToString();
        string eol = invocation.SyntaxTree.GetText(cancellationToken).ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string next = eol + new string([.. line.TakeWhile(char.IsWhiteSpace)]) + "    : ";
        string selector = string.Join(next, branches.Select(b => $"{b.Condition} ? {b.Value}")) + next + $"throw new global::System.InvalidOperationException({message})";
        return ProjectToPattern.Fixed(invocation, query, selector, [.. namespaces.Order(StringComparer.Ordinal)]);
    }

    private static PatternResult Open(string reason) =>
        PatternResult.Manual(GenericDestinationCode.GenericProjectionOpenSet, reason.EndsWith('.') ? reason : reason + ".");
}
