using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Shouldly has no NotContainNulls/AllBe: both become a predicate. FluentAssertions 7.2.2 has AllBe only on string collections
// (ordinal equality, so `i == v`); the lambda parameter gets a name nothing else in scope has.
public class NotContainNullsPattern : FluentAssertionPattern
{
    private static readonly string[] s_parameterNames = ["i", "item", "x", "e", "element", "entry"];

    public override string Id => "fa.not-contain-nulls";

    public override string Description => "x.Should().NotContainNulls() → x.ShouldNotContain(i => i == null); x.Should().AllBe(v) → x.ShouldAllBe(i => i == v) (value-like elements only)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "NotContainNulls", "AllBe");

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        ITypeSymbol? subjectType = model.GetTypeInfo(context.Subject).Type;
        ITypeSymbol? element = subjectType is null or IErrorTypeSymbol || subjectType.SpecialType == SpecialType.System_String
            ? null
            : subjectType.AllInterfaces.Prepend(subjectType)
                .FirstOrDefault(t => t.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T) is INamedTypeSymbol enumerable
                ? enumerable.TypeArguments[0]
                : null;
        if (element is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The subject is not a generic IEnumerable<T>.");
        }

        string parameter = FreeParameterName(context);
        return context.Name == "NotContainNulls" ? RewriteNotContainNulls(context, element, parameter) : RewriteAllBe(context, element, parameter);
    }

    private static PatternResult RewriteNotContainNulls(AssertionContext context, ITypeSymbol element, string parameter)
    {
        if (context.Values.Count != 0)
        {
            return PatternResult.Manual("NotContainNulls(selector) checks a property of each item: use ShouldNotContain(i => i.Property == null) by hand.");
        }

        if (element.IsValueType && element.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
        {
            return PatternResult.Manual("The elements are a non-nullable value type: `i == null` does not compile cleanly; the assertion is always true.");
        }

        ArgumentSyntax predicate = SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"{parameter} => {parameter} == null"));
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
            FluentAssertionsSyntax.NotNull(context.Subject, context.Model).WithoutTrailingTrivia(), "ShouldNotContain", predicate, context.Because));
    }

    private static PatternResult RewriteAllBe(AssertionContext context, ITypeSymbol element, string parameter)
    {
        SemanticModel model = context.Model;
        if (context.Values.Count != 1)
        {
            return PatternResult.Manual("AllBe(..., config): equivalency options have no Shouldly equivalent.");
        }

        ExpressionSyntax expected = context.Values[0].Expression;
        ITypeSymbol? expectedType = model.GetTypeInfo(expected).Type;
        if (element.SpecialType != SpecialType.System_String || expectedType?.SpecialType != SpecialType.System_String)
        {
            return PatternResult.Manual("AllBe(...) on something other than a string collection with a string value: no plain `==` equivalent.");
        }

        // The lambda becomes an expression tree that evaluates `expected` for every element.
        if (!FluentAssertionsSyntax.IsSafeToEvaluateTwice(expected, model) && !model.GetConstantValue(expected).HasValue)
        {
            return PatternResult.Manual("The expected value is computed: inside the predicate it would be evaluated per element. Put it in a variable first.");
        }

        ExpressionSyntax operand = expected is LiteralExpressionSyntax or IdentifierNameSyntax or MemberAccessExpressionSyntax
            ? expected.WithoutTrivia()
            : SyntaxFactory.ParenthesizedExpression(expected.WithoutTrivia());
        ArgumentSyntax predicate = SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"{parameter} => {parameter} == {operand}"));
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
            FluentAssertionsSyntax.NotNull(context.Subject, model).WithoutTrailingTrivia(), "ShouldAllBe", predicate, context.Because));
    }

    private static string FreeParameterName(AssertionContext context)
    {
        int position = context.Assertion.SpanStart;
        string name = s_parameterNames.FirstOrDefault(n => context.Model.LookupSymbols(position, name: n).IsEmpty) ?? "";
        for (int i = 1; name == ""; i++)
        {
            string candidate = "item" + i;
            name = context.Model.LookupSymbols(position, name: candidate).IsEmpty ? candidate : "";
        }

        return name;
    }
}
