using System.Buffers;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary>
/// A step text that SpecFlow reads as a regular expression but Reqnroll 3.3.4 reads as a Cucumber expression (its
/// <c>CucumberExpressionDetector</c>), when the text holds a character whose meaning differs between the two. Council round 23, S13:
/// a Given/When/Then/StepDefinition attribute with a compile-time text gets <c>ExpressionType = ExpressionType.RegularExpression</c>
/// (Fixed + Info), so the text keeps its SpecFlow meaning. Manual: a text ending in an escaped <c>$</c> (Reqnroll leaves the end
/// unanchored), a text without a compile-time value, and a step attribute of another type.
/// TAAK-377: a project with the CucumberExpressions.SpecFlow plugin already reads texts as Cucumber expressions under SpecFlow, so
/// nothing is pinned; only a text the plugin and Reqnroll detect differently (Reqnroll's extra <c>\.</c>/<c>\d+</c> rule) is Manual.
/// </summary>
public partial class StepExpressionPattern : IPattern
{
    // Characters with a meaning in a regular expression or a Cucumber expression; text without them matches literally under both.
    private static readonly SearchValues<char> s_special = SearchValues.Create(@"()[]{}.*+?|\/^$");

    private static readonly HashSet<string> s_pinnable = ["Given", "When", "Then", "StepDefinition"];

    public string Id => "specflow.step-expression";

    public string Description => "step text Reqnroll reads as a Cucumber expression → ExpressionType.RegularExpression";

    // The assembly of the CucumberExpressions.SpecFlow.3-x plugin packages (Reqnroll's detector was ported from it).
    internal const string CucumberPluginAssembly = "CucumberExpressions.SpecFlow.SpecFlowPlugin";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        bool plugin = model.Compilation.ReferencedAssemblyNames.Any(a => a.Name == CucumberPluginAssembly);
        return root.DescendantNodes().OfType<AttributeSyntax>()
            .Where(a => a.ArgumentList?.Arguments.FirstOrDefault() is { NameEquals: null, NameColon: null } first
                && StepAttributeName(a, model) is not null
                && Text(first.Expression, model) is var text
                && (EndsWithEscapedDollar(text ?? "") || (plugin ? text is not null && PluginDiffers(text) : text is null || ChangesMeaning(text))));
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var attribute = (AttributeSyntax)node;
        if (Text(attribute.ArgumentList!.Arguments[0].Expression, model) is not string text)
        {
            return PatternResult.Manual("The step text has no compile-time value (interpolated or unreadable), so Unshackle cannot tell whether " +
                "Reqnroll reads it as a Cucumber expression; check it and add ExpressionType = ExpressionType.RegularExpression by hand if needed.");
        }

        if (EndsWithEscapedDollar(text))
        {
            return PatternResult.Manual("The step text ends in an escaped $: SpecFlow anchors its end, Reqnroll does not, so the step can match " +
                "longer text; append $ to the text by hand.");
        }

        if (model.Compilation.ReferencedAssemblyNames.Any(a => a.Name == CucumberPluginAssembly))
        {
            return PatternResult.Manual("The CucumberExpressions.SpecFlow plugin reads this step text as a Cucumber expression, Reqnroll reads it " +
                "as a regular expression (it holds \\. or \\d+); rewrite it as a Cucumber expression or a regular expression by hand.");
        }

        if (!s_pinnable.Contains(StepAttributeName(attribute, model)!)
            || attribute.ArgumentList.Arguments.Any(a => a.NameEquals?.Name.Identifier.ValueText == "ExpressionType"))
        {
            return PatternResult.Manual("Reqnroll reads this step text as a Cucumber expression, not a regular expression, so it can match other steps " +
                "or bind other parameters; add ExpressionType = ExpressionType.RegularExpression to the attribute after the migration, " +
                "or anchor the text with ^ and $, by hand.");
        }

        // A qualified attribute name has no using to rename, so the enum is qualified too.
        string type = attribute.Name is QualifiedNameSyntax or AliasQualifiedNameSyntax ? "global::Reqnroll.ExpressionType" : "ExpressionType";
        AttributeArgumentSyntax pin = SyntaxFactory.ParseAttributeArgumentList($"(x, ExpressionType = {type}.RegularExpression)")!.Arguments[1];
        AttributeArgumentListSyntax list = attribute.ArgumentList!;
        AttributeSyntax pinned = attribute.WithArgumentList(list.WithArguments(SyntaxFactory.SeparatedList([.. list.Arguments, pin],
            [.. list.Arguments.GetSeparators(), SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space)])));
        return (FixedResult)PatternResult.Fixed(pinned) with { Info = $"Step text \"{text}\" kept as regex via ExpressionType.RegularExpression." };
    }

    // Council S13: interpolated text stays Manual even when constant; otherwise the compile-time value, null when there is none.
    private static string? Text(ExpressionSyntax expression, SemanticModel model) =>
        expression is InterpolatedStringExpressionSyntax ? null : model.GetConstantValue(expression).Value as string;

    // Council S13: an odd backslash run before the final $ on the cooked value (the literal $ SpecFlow still anchors after).
    internal static bool EndsWithEscapedDollar(string text)
    {
        if (!text.EndsWith('$'))
        {
            return false;
        }

        int backslashes = text.Length - 1 - text.AsSpan(0, text.Length - 1).TrimEnd('\\').Length;
        return backslashes % 2 == 1;
    }

    // Mirrors Reqnroll 3.3.4 CucumberExpressionDetector.IsCucumberExpression, plus: a character with a regex or Cucumber meaning.
    internal static bool ChangesMeaning(string text) =>
        !text.StartsWith('^') && !text.EndsWith('$')
        && (ParameterPlaceholder().IsMatch(text) || (!CommonRegex().IsMatch(text) && !ExtendedRegex().IsMatch(text)))
        && text.AsSpan().IndexOfAny(s_special) >= 0;

    // The plugin's IsCucumberExpression lacks Reqnroll's ExtendedRegex rule; the texts it reads as Cucumber and Reqnroll as regex.
    internal static bool PluginDiffers(string text) =>
        !text.StartsWith('^') && !text.EndsWith('$') && !ParameterPlaceholder().IsMatch(text) && !CommonRegex().IsMatch(text)
        && ExtendedRegex().IsMatch(text);

    // The short name of the attribute type ("Given", "MyStep"), when it derives from SpecFlow's StepDefinitionBaseAttribute; null otherwise.
    private static string? StepAttributeName(AttributeSyntax attribute, SemanticModel model)
    {
        INamedTypeSymbol? attributeType = (model.GetSymbolInfo(attribute).Symbol as IMethodSymbol)?.ContainingType;
        for (INamedTypeSymbol? type = attributeType; type is not null; type = type.BaseType)
        {
            if (SpecFlowNames.FullName(type) == SpecFlowNames.OldRoot + ".StepDefinitionBaseAttribute")
            {
                string name = attributeType!.Name;
                return SpecFlowNames.FullName(attributeType) == SpecFlowNames.OldRoot + "." + name && name.EndsWith("Attribute", StringComparison.Ordinal)
                    ? name[..^"Attribute".Length]
                    : name;
            }
        }

        return null;
    }

    [GeneratedRegex(@"{\w*}")]
    private static partial Regex ParameterPlaceholder();

    [GeneratedRegex(@"(\([^\)]+[\*\+]\)|\.\*)")]
    private static partial Regex CommonRegex();

    [GeneratedRegex(@"(\\\.|\\d\+)")]
    private static partial Regex ExtendedRegex();
}
