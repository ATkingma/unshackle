using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// `.And` chains of comparisons are split by AndChainPattern, which hands each link to this pattern.
public class BeGreaterLessPattern : FluentAssertionPattern
{
    // FluentAssertions name → (Shouldly name, number of value arguments).
    private static readonly Dictionary<string, (string Should, int Values)> s_methods = new(StringComparer.Ordinal)
    {
        ["BeGreaterThan"] = ("ShouldBeGreaterThan", 1),
        ["BeGreaterThanOrEqualTo"] = ("ShouldBeGreaterThanOrEqualTo", 1),
        ["BeGreaterOrEqualTo"] = ("ShouldBeGreaterThanOrEqualTo", 1),
        ["BeLessThan"] = ("ShouldBeLessThan", 1),
        ["BeLessThanOrEqualTo"] = ("ShouldBeLessThanOrEqualTo", 1),
        ["BeLessOrEqualTo"] = ("ShouldBeLessThanOrEqualTo", 1),
        ["BeInRange"] = ("ShouldBeInRange", 2),
        ["NotBeInRange"] = ("ShouldNotBeInRange", 2),
        ["BePositive"] = ("ShouldBePositive", 0),
        ["BeNegative"] = ("ShouldBeNegative", 0),
        // Date and time comparisons: FluentAssertions uses CompareTo, as Shouldly does (probed: equal, ±1 tick, Kind, offset).
        ["BeAfter"] = ("ShouldBeGreaterThan", 1),
        ["BeOnOrAfter"] = ("ShouldBeGreaterThanOrEqualTo", 1),
        ["BeBefore"] = ("ShouldBeLessThan", 1),
        ["BeOnOrBefore"] = ("ShouldBeLessThanOrEqualTo", 1),
    };

    // The numeric types Shouldly has ShouldBePositive/ShouldBeNegative for.
    private static readonly SpecialType[] s_signTypes =
        [SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_Decimal, SpecialType.System_Double, SpecialType.System_Single];

    public override string Id => "fa.be-greater-less";

    public override string Description => "x.Should().BeGreaterThan(n) → x.ShouldBeGreaterThan(n), BeInRange(a, b) → ShouldBeInRange(a, b), BeAfter(d) → ShouldBeGreaterThan(d)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_methods.Keys]);

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        ITypeSymbol? type = model.GetTypeInfo(context.Subject).Type;
        if (type is null or IErrorTypeSymbol || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return PatternResult.Manual("The subject is nullable or has an unknown type: Shouldly's comparisons need a non-nullable IComparable<T>.");
        }

        INamedTypeSymbol? comparable = model.Compilation.GetTypeByMetadataName("System.IComparable`1");
        if (!type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, comparable)
                && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type)))
        {
            return PatternResult.Manual("The subject type does not implement IComparable<T> of itself, which Shouldly requires.");
        }

        (string shouldName, int count) = s_methods[context.Name];
        if (context.Values.Count != count)
        {
            return PatternResult.Manual($"{context.Name}(...) with an unexpected argument list.");
        }

        if (count == 0 && !s_signTypes.Contains(type.SpecialType))
        {
            return PatternResult.Manual($"{shouldName} only exists for int, long, short, decimal, double and float.");
        }

        // Probed (FA 7.2.2, Shouldly 4.3.0): double/float NaN fails FluentAssertions' BeNegative but passes ShouldBeNegative and
        // ShouldBeLessThan(0) (CompareTo orders NaN below everything). `x < 0` is false for NaN and for -0.0, like FluentAssertions.
        // BePositive is identical in both libraries (NaN, -0.0, infinities probed), so only BeNegative needs this.
        if (context.Name == "BeNegative" && type.SpecialType is SpecialType.System_Double or SpecialType.System_Single)
        {
            // Plain `0`: it converts implicitly to double and float, so the comparison stays in the subject's type.
            ExpressionSyntax zero = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
            ExpressionSyntax left = context.Subject.WithoutTrivia() is IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax or ParenthesizedExpressionSyntax
                ? context.Subject.WithoutTrivia()
                : SyntaxFactory.ParenthesizedExpression(context.Subject.WithoutTrivia());
            ExpressionSyntax comparison = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.LessThanExpression,
                    left,
                    SyntaxFactory.Token(SyntaxKind.LessThanToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
                    zero));
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(comparison, "ShouldBeTrue", context.Because));
        }

        // ShouldBeGreaterThan<T>(this T, T) infers T from both sides: `b.ShouldBeGreaterThan(1)` on a byte binds T = int and fails.
        var values = new List<ArgumentSyntax>();
        foreach (ArgumentSyntax value in context.Values)
        {
            if (FluentAssertionsSyntax.ConvertExpected(value.Expression, type, model, context.Assertion.SpanStart) is not { } converted)
            {
                return PatternResult.Manual($"Value type {model.GetTypeInfo(value.Expression).Type} differs from subject type {type}; {shouldName} cannot infer one type.");
            }

            values.Add(value.WithExpression(converted));
        }

        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrivia(), shouldName, [.. values, context.Because]));
    }
}
