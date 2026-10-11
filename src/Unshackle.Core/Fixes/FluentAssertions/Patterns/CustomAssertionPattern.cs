using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// A method of the user (or another library) on a FluentAssertions assertions type, such as `result.Should().BeFailure()`.
// Its body uses Execute.Assertion, which has no generic Shouldly translation, so every call is Manual. The reason is the
// same for every call of one method: the report and console group on it, one fix per extension instead of per call.
public class CustomAssertionPattern : IPattern
{
    public const string PatternId = "fa.custom-assertion";

    // Literal for check-pattern-docs.sh.
    public string Id => "fa.custom-assertion";

    public string Description => "x.Should().MyOwnAssertion() → Manual, grouped per custom assertion";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is MemberAccessExpressionSyntax && IsCustomAssertion(model.GetSymbolInfo(i).Symbol as IMethodSymbol));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var method = (IMethodSymbol)model.GetSymbolInfo(node).Symbol!;
        IMethodSymbol definition = (method.ReducedFrom ?? method).OriginalDefinition;
        string where = definition.Locations.FirstOrDefault(l => l.IsInSource) is { } location
            ? Path.GetFileName(location.SourceTree!.FilePath)
            : definition.ContainingAssembly.Name;
        return PatternResult.Manual(
            $"Custom assertion `{definition.ContainingType.Name}.{definition.Name}` ({where}): "
            + "rewrite this FluentAssertions extension to Shouldly once; its calls can stay.");
    }

    // The Manual findings of this pattern, one group per custom assertion (the reason names it), in order of first call.
    public static IEnumerable<IGrouping<string, Finding>> Group(IEnumerable<Finding> findings) =>
        findings
            .Where(f => f.PatternId == PatternId && f.Result is ManualResult)
            .GroupBy(f => ((ManualResult)f.Result).Reason, StringComparer.Ordinal);

    private static bool IsCustomAssertion(IMethodSymbol? method)
    {
        if (method is null || FluentAssertionsSyntax.IsFluentAssertions(method))
        {
            return false;
        }

        for (ITypeSymbol? type = method.ReducedFrom is null ? method.ContainingType : method.ReceiverType; type is not null; type = type.BaseType)
        {
            if (FluentAssertionsSyntax.IsFluentAssertions(type) && type.Name.EndsWith("Assertions", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
