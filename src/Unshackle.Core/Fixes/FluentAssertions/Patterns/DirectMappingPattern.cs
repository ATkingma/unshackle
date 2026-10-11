using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Assertions with a Shouldly method of the same meaning (probed: equal, unequal, null, empty, records vs classes).
// ContainInOrder has none and is reported with the code to write instead.
public class DirectMappingPattern : FluentAssertionPattern
{
    private static readonly Dictionary<string, string> s_methods = new(StringComparer.Ordinal)
    {
        ["BeOneOf"] = "ShouldBeOneOf",
        ["BeSubsetOf"] = "ShouldBeSubsetOf",
        ["OnlyHaveUniqueItems"] = "ShouldBeUnique",
        ["HaveValue"] = "ShouldNotBeNull",
        ["NotHaveValue"] = "ShouldBeNull",
        ["ContainInOrder"] = "",
    };

    public override string Id => "fa.direct-mapping";

    public override string Description => "BeOneOf → ShouldBeOneOf, BeSubsetOf → ShouldBeSubsetOf, OnlyHaveUniqueItems → ShouldBeUnique, HaveValue/NotHaveValue → ShouldNotBeNull/ShouldBeNull";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_methods.Keys]);

    // Before the argument split: BeOneOf(params T[]) and ContainInOrder(params T[]) would be reported as becauseArgs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context)
    {
        if (context.Name == "ContainInOrder")
        {
            return PatternResult.Manual("ContainInOrder(...) has no Shouldly equivalent. Check the subsequence by hand, e.g. " +
                "`var rest = x.SkipWhile(i => !Equals(i, a)).ToList(); rest.ShouldNotBeEmpty(); rest.Skip(1).SkipWhile(i => !Equals(i, b)).ShouldNotBeEmpty();`.");
        }

        if (context.Name != "BeOneOf"
            || context.Model.GetOperation(context.Assertion) is not IInvocationOperation { Arguments: [{ ArgumentKind: ArgumentKind.ParamArray, Value: IArrayCreationOperation { Initializer: { } initializer } }] })
        {
            return null;
        }

        // BeOneOf(params T[]) has no because; ShouldBeOneOf<T>(T, params T[]) infers T from both sides, like ShouldBe.
        ITypeSymbol? type = context.Model.GetTypeInfo(context.Subject).Type;
        if (type is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject is unknown.");
        }

        var values = new List<ArgumentSyntax>();
        foreach (ExpressionSyntax value in initializer.ElementValues.Select(e => (ExpressionSyntax)e.Syntax))
        {
            if (FluentAssertionsSyntax.ConvertExpected(value, type, context.Model, context.Assertion.SpanStart) is not { } converted)
            {
                return PatternResult.Manual($"Value type {context.Model.GetTypeInfo(value).Type} differs from subject type {type}; ShouldBeOneOf cannot infer one type.");
            }

            values.Add(SyntaxFactory.Argument(converted));
        }

        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrivia(), "ShouldBeOneOf", values));
    }

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        string name = context.Name;
        ITypeSymbol? type = context.Model.GetTypeInfo(context.Subject).Type;
        int expected = name == "BeSubsetOf" ? 1 : 0;
        if (name == "BeOneOf" || context.Values.Count != expected)
        {
            return PatternResult.Manual($"{name}(...) with a collection, comparer or key selector: rewrite it by hand.");
        }

        if (name.EndsWith("HaveValue", StringComparison.Ordinal) && type?.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
        {
            return PatternResult.Manual($"{name}() on a non-Nullable<T> subject: rewrite it by hand.");
        }

        // A null collection fails both; ShouldNotBeNull keeps that without a nullable warning on Shouldly's receiver.
        ExpressionSyntax receiver = expected == 1 || name == "OnlyHaveUniqueItems" ? FluentAssertionsSyntax.NotNull(context.Subject, context.Model) : context.Subject;
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(receiver.WithoutTrivia(), s_methods[name], [.. context.Values, context.Because]));
    }
}
