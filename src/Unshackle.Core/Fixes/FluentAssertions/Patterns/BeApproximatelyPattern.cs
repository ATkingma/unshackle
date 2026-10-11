using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeApproximatelyPattern : FluentAssertionPattern
{
    // Types for which Shouldly has ShouldBe(actual, expected, tolerance); FluentAssertions and Shouldly both pass at |a - e| == tolerance.
    private static readonly SpecialType[] s_numbers = [SpecialType.System_Double, SpecialType.System_Single, SpecialType.System_Decimal];

    private static readonly string[] s_times = ["System.DateTime", "System.DateTimeOffset", "System.TimeSpan"];

    public override string Id => "fa.be-approximately";

    public override string Description => "x.Should().BeApproximately(y, tol) / BeCloseTo(t, span) → x.ShouldBe(y, tol); NotBe… → ShouldNotBe or Math.Abs(x - y).ShouldBeGreaterThan(tol)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeApproximately", "NotBeApproximately", "BeCloseTo", "NotBeCloseTo");

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        string name = context.Name;
        bool negated = name.StartsWith("Not", StringComparison.Ordinal);
        ExpressionSyntax subject = context.Subject;
        IReadOnlyList<ArgumentSyntax> values = context.Values;
        if (values.Count != 2)
        {
            return PatternResult.Manual($"{name}(...) with an unexpected argument list.");
        }

        ITypeSymbol? subjectType = model.GetTypeInfo(subject).Type;
        ITypeSymbol? expectedType = model.GetTypeInfo(values[0].Expression).ConvertedType;
        bool nullable = subjectType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
        ITypeSymbol? valueType = nullable ? ((INamedTypeSymbol)subjectType!).TypeArguments[0] : subjectType;
        bool number = valueType is not null && s_numbers.Contains(valueType.SpecialType);
        if (!number && !s_times.Contains(valueType?.ToDisplayString()))
        {
            return PatternResult.Manual($"{name}(...) on {subjectType}: Shouldly has a tolerance only for double, float, decimal, DateTime, DateTimeOffset and TimeSpan.");
        }

        if (!SymbolEqualityComparer.Default.Equals(expectedType, valueType))
        {
            return PatternResult.Manual($"{name}(...) with a nullable or differently typed expected value: rewrite by hand.");
        }

        if (nullable && negated)
        {
            return PatternResult.Manual($"{name}(...) on a nullable subject: decide by hand what null should do.");
        }

        ExpressionSyntax receiver = FluentAssertionsSyntax.IsSafeReceiver(subject) ? subject.WithoutTrailingTrivia() : SyntaxFactory.ParenthesizedExpression(subject.WithoutTrivia());
        if (nullable)
        {
            // ShouldBe(tolerance) takes the non-nullable value; FluentAssertions fails on null too.
            receiver = FluentAssertionsSyntax.Invoke(receiver, "ShouldNotBeNull");
        }

        if (negated && number)
        {
            // Shouldly has no ShouldNotBe(tolerance) for numbers; |x - y| > tolerance is FluentAssertions' NotBeApproximately.
            ExpressionSyntax difference = SyntaxFactory.ParseExpression(
                $"System.Math.Abs({Operand(receiver)} - {Operand(values[0].Expression.WithoutTrivia())})");
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(difference, "ShouldBeGreaterThan", values[1], context.Because));
        }

        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(receiver, negated ? "ShouldNotBe" : "ShouldBe", values[0], values[1], context.Because));
    }

    private static ExpressionSyntax Operand(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax || FluentAssertionsSyntax.IsSafeReceiver(expression)
            ? expression
            : SyntaxFactory.ParenthesizedExpression(expression);
}
