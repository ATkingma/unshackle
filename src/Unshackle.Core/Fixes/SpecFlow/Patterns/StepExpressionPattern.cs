using System.Buffers;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary>
/// A step text that SpecFlow reads as a regular expression but Reqnroll 3.3.4 reads as a Cucumber expression (its
/// <c>CucumberExpressionDetector</c>), when the text holds a character whose meaning differs between the two: the step can match other
/// text, or bind other parameters. Manual (conservative, TAAK-340 council question 12); plain text matches the same under both.
/// </summary>
public partial class StepExpressionPattern : IPattern
{
    // Characters with a meaning in a regular expression or a Cucumber expression; text without them matches literally under both.
    private static readonly SearchValues<char> s_special = SearchValues.Create(@"()[]{}.*+?|\/^$");

    public string Id => "specflow.step-expression";

    public string Description => "step text Reqnroll reads as a Cucumber expression → Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<AttributeSyntax>()
            .Where(a => a.ArgumentList?.Arguments.FirstOrDefault() is { NameEquals: null, NameColon: null } first
                && IsStepAttribute(a, model)
                && model.GetConstantValue(first.Expression).Value is string text
                && ChangesMeaning(text));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("Reqnroll reads this step text as a Cucumber expression, not a regular expression, so it can match other steps " +
            "or bind other parameters; add ExpressionType = ExpressionType.RegularExpression to the attribute after the migration, " +
            "or anchor the text with ^ and $, by hand.");

    // Mirrors Reqnroll 3.3.4 CucumberExpressionDetector.IsCucumberExpression, plus: a character with a regex or Cucumber meaning.
    internal static bool ChangesMeaning(string text) =>
        !text.StartsWith('^') && !text.EndsWith('$')
        && (ParameterPlaceholder().IsMatch(text) || (!CommonRegex().IsMatch(text) && !ExtendedRegex().IsMatch(text)))
        && text.AsSpan().IndexOfAny(s_special) >= 0;

    private static bool IsStepAttribute(AttributeSyntax attribute, SemanticModel model)
    {
        for (INamedTypeSymbol? type = (model.GetSymbolInfo(attribute).Symbol as IMethodSymbol)?.ContainingType; type is not null; type = type.BaseType)
        {
            if (SpecFlowNames.FullName(type) == SpecFlowNames.OldRoot + ".StepDefinitionBaseAttribute")
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"{\w*}")]
    private static partial Regex ParameterPlaceholder();

    [GeneratedRegex(@"(\([^\)]+[\*\+]\)|\.\*)")]
    private static partial Regex CommonRegex();

    [GeneratedRegex(@"(\\\.|\\d\+)")]
    private static partial Regex ExtendedRegex();
}
