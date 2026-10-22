using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

/// <summary>
/// What the whole solution says about its MassTransit consumers (council round 11, T7): the first filter it registers or declares (a filter
/// may run around any consumer, so it makes every consumer Manual), and the consumers that have a ConsumerDefinition. Built once per solution.
/// </summary>
internal class ConsumerFacts
{
    private static readonly ConditionalWeakTable<Solution, Task<ConsumerFacts>> s_cache = [];

    private static readonly HashSet<string> s_filterCalls = new(
        ["UseFilter", "UseConsumeFilter", "UseSendFilter", "UsePublishFilter", "UseExecute", "UseExecuteAsync"],
        StringComparer.Ordinal);

    /// <summary>File:line of the first filter of the solution (a Use...Filter call or a class implementing IFilter&lt;T&gt;), or null.</summary>
    public required string? Filter { get; init; }

    /// <summary>The consumers (by fully qualified name) that a ConsumerDefinition&lt;T&gt; names, with the definition's file:line.</summary>
    public required IReadOnlyDictionary<string, string> Definitions { get; init; }

    public static Task<ConsumerFacts> ForAsync(Solution solution, CancellationToken cancellationToken) =>
        s_cache.GetValue(solution, s => BuildAsync(s, cancellationToken));

    internal static string Key(ITypeSymbol type) => type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static async Task<ConsumerFacts> BuildAsync(Solution solution, CancellationToken cancellationToken)
    {
        string? filter = null;
        Dictionary<string, string> definitions = new(StringComparer.Ordinal);
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains(MassTransitSyntax.Namespace, StringComparison.Ordinal)
                || await document.GetSyntaxRootAsync(cancellationToken) is not { } root || await document.GetSemanticModelAsync(cancellationToken) is not { } model)
            {
                continue;
            }

            filter ??= MassTransitSyntax.FindCalls(root, model, s_filterCalls).Select(Where).FirstOrDefault()
                ?? root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => IsFilter(c, model)).Select(Where).FirstOrDefault();
            foreach (ClassDeclarationSyntax definition in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(definition, cancellationToken) is INamedTypeSymbol type && Defined(type) is { } consumer)
                {
                    definitions.TryAdd(Key(consumer), Where(definition));
                }
            }
        }

        return new ConsumerFacts { Filter = filter, Definitions = definitions };
    }

    // The consumer a ConsumerDefinition<T> class (or a class deriving from one) names; else null.
    private static ITypeSymbol? Defined(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current is { Name: "ConsumerDefinition", TypeArguments: [{ } consumer] } && MassTransitSyntax.IsMassTransit(current))
            {
                return consumer;
            }
        }

        return null;
    }

    private static bool IsFilter(ClassDeclarationSyntax declaration, SemanticModel model) =>
        model.GetDeclaredSymbol(declaration) is INamedTypeSymbol type
        && type.AllInterfaces.Any(i => i is { Name: "IFilter", Arity: 1 } && MassTransitSyntax.IsMassTransit(i));

    private static string Where(SyntaxNode node)
    {
        FileLinePositionSpan span = node.GetLocation().GetLineSpan();
        return $"{Path.GetFileName(span.Path)}:{span.StartLinePosition.Line + 1}";
    }
}
