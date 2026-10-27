using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary>
/// <c>Given("...")</c>, <c>When</c>, <c>Then</c>, <c>And</c>, <c>But</c> called on a <c>Steps</c> base class: SpecFlow v4, and so Reqnroll,
/// removed these string step calls. Always Manual: the replacement (calling the step method, or a driver) is a design choice. Council round
/// 18, S4: when exactly one step definition of the project matches a literal step text, the reason names it as a possible match (a hint, no
/// rewrite); S1: the reason names <c>Reqnroll.SpecFlowCompatibility</c> as the other hand route.
/// </summary>
public class StringStepCallPattern : IPattern
{
    private static readonly HashSet<string> s_methods = new(["Given", "When", "Then", "And", "But"], StringComparer.Ordinal);

    private static readonly TimeSpan s_matchTimeout = TimeSpan.FromMilliseconds(100);

    public string Id => "specflow.string-step-call";

    public string Description => "Steps.Given(\"...\") and friends → Manual (removed in Reqnroll)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol method
                && s_methods.Contains(method.Name)
                && SpecFlowNames.FullName(method.ContainingType) == SpecFlowNames.OldRoot + ".Steps");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var call = (InvocationExpressionSyntax)node;
        string reason = "String step calls (Given/When/Then/And/But on a Steps base class) were removed in Reqnroll. Call the step " +
            "definition method directly or move the shared code to a driver class (or keep them through Reqnroll.SpecFlowCompatibility), " +
            "then run the fix again.";
        string keyword = ((IMethodSymbol)model.GetSymbolInfo(call).Symbol!).Name;
        if (call.ArgumentList.Arguments.FirstOrDefault()?.Expression is ExpressionSyntax text
            && model.GetConstantValue(text) is { HasValue: true, Value: string step }
            && Matches(model.Compilation, keyword, step) is [IMethodSymbol match])
        {
            reason += $" Possible match: {match.ContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{match.Name}.";
        }

        return PatternResult.Manual(reason);
    }

    // The step definitions of the compilation whose step text matches the whole literal (SpecFlow anchors a step regex at both ends).
    private static List<IMethodSymbol> Matches(Compilation compilation, string keyword, string step) =>
        [
            .. Types(compilation.Assembly.GlobalNamespace).SelectMany(t => t.GetMembers()).OfType<IMethodSymbol>()
                .Where(m => m.GetAttributes().Any(a => Kind(a.AttributeClass) is string kind
                    && (kind == "StepDefinition" || keyword is "And" or "But" || kind == keyword)
                    && a.ConstructorArguments.FirstOrDefault() is { Value: string pattern }
                    && FullMatch(pattern, step))),
        ];

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol ns) =>
        ns.GetTypeMembers().SelectMany(Nested).Concat(ns.GetNamespaceMembers().SelectMany(Types));

    private static IEnumerable<INamedTypeSymbol> Nested(INamedTypeSymbol type) => type.GetTypeMembers().SelectMany(Nested).Prepend(type);

    // Given/When/Then/StepDefinition for a SpecFlow step attribute (or a subclass of one); null otherwise.
    private static string? Kind(INamedTypeSymbol? type)
    {
        for (; type is not null; type = type.BaseType)
        {
            string fullName = SpecFlowNames.FullName(type);
            if (fullName is "TechTalk.SpecFlow.GivenAttribute" or "TechTalk.SpecFlow.WhenAttribute" or "TechTalk.SpecFlow.ThenAttribute"
                or "TechTalk.SpecFlow.StepDefinitionAttribute")
            {
                return SpecFlowNames.SimpleNameOf(fullName)[..^"Attribute".Length];
            }
        }

        return null;
    }

    private static bool FullMatch(string pattern, string step)
    {
        try
        {
            return Regex.IsMatch(step, "^(?:" + pattern.TrimStart('^').TrimEnd('$') + ")$", RegexOptions.CultureInvariant, s_matchTimeout);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
