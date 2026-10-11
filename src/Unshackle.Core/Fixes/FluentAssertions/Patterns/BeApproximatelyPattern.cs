using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeApproximatelyPattern : IPattern
{
    // Types for which Shouldly has ShouldBe(actual, expected, tolerance); FluentAssertions and Shouldly both pass at |a - e| == tolerance.
    private static readonly SpecialType[] s_numbers = [SpecialType.System_Double, SpecialType.System_Single, SpecialType.System_Decimal];

    private static readonly string[] s_times = ["System.DateTime", "System.DateTimeOffset", "System.TimeSpan"];

    public string Id => "fa.be-approximately";

    public string Description => "x.Should().BeApproximately(y, tol) / BeCloseTo(t, span) → x.ShouldBe(y, tol); NotBe… → ShouldNotBe or Math.Abs(x - y).ShouldBeGreaterThan(tol)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeApproximately", "NotBeApproximately", "BeCloseTo", "NotBeCloseTo");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var assertion = (InvocationExpressionSyntax)node;
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        bool negated = name.StartsWith("Not", StringComparison.Ordinal);
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}(...) is not in the form x.Should().{name}(...), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}(...) is used (.And or a value); Shouldly returns nothing.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because)
            || values.Count != 2)
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
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

        ExpressionSyntax receiver = HaveCountPattern.IsSafeReceiver(subject) ? subject.WithoutTrailingTrivia() : SyntaxFactory.ParenthesizedExpression(subject.WithoutTrivia());
        if (nullable)
        {
            // ShouldBe(tolerance) takes the non-nullable value; FluentAssertions fails on null too.
            receiver = Invoke(receiver, "ShouldNotBeNull", []);
        }

        IEnumerable<ArgumentSyntax> message = because is null ? [] : [because];
        if (negated && number)
        {
            // Shouldly has no ShouldNotBe(tolerance) for numbers; |x - y| > tolerance is FluentAssertions' NotBeApproximately.
            ExpressionSyntax difference = SyntaxFactory.ParseExpression(
                $"System.Math.Abs({Operand(receiver)} - {Operand(values[0].Expression.WithoutTrivia())})");
            return PatternResult.Fixed(Invoke(difference, "ShouldBeGreaterThan", [values[1], .. message]));
        }

        return PatternResult.Fixed(Invoke(receiver, negated ? "ShouldNotBe" : "ShouldBe", [.. values, .. message]));
    }

    private static ExpressionSyntax Operand(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax || HaveCountPattern.IsSafeReceiver(expression)
            ? expression
            : SyntaxFactory.ParenthesizedExpression(expression);

    private static InvocationExpressionSyntax Invoke(ExpressionSyntax receiver, string name, IReadOnlyList<ArgumentSyntax> arguments) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(name)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                arguments.Select((a, i) => i == 0 ? a.WithoutTrivia() : a.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space)))));
}
