using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class OnlyContainPattern : FluentAssertionPattern
{
    public override string Id => "fa.only-contain";

    public override string Description => "x.Should().OnlyContain(pred) → x.ShouldNotBeEmpty(); x.ShouldAllBe(pred); (FluentAssertions fails on an empty collection)";

    // The node is the whole statement (it is replaced by two statements), or the bare invocation when it is no statement.
    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "OnlyContain").Select(FluentAssertionsSyntax.WithStatement);

    protected override InvocationExpressionSyntax GetAssertion(SyntaxNode node) => FluentAssertionsSyntax.AssertionOfStatement(node);

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        ExpressionSyntax subject = context.Subject;
        IReadOnlyList<ArgumentSyntax> values = context.Values;
        ArgumentSyntax? because = context.Because;
        ITypeSymbol? type = context.Model.GetTypeInfo(subject).Type;
        if (values.Count != 1 || type is null or IErrorTypeSymbol
            || !type.AllInterfaces.Prepend(type).Any(t => t.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T))
        {
            return PatternResult.Manual("The subject is not a generic IEnumerable<T> or the argument list is unexpected.");
        }

        // FluentAssertions fails on an empty collection, ShouldAllBe passes: a ShouldNotBeEmpty statement keeps the behaviour.
        if (!FluentAssertionsSyntax.TryGetSplittableStatement(context.Node, out _))
        {
            return PatternResult.Manual("OnlyContain(...) fails on an empty collection and ShouldAllBe does not, so a ShouldNotBeEmpty is needed. " + FluentAssertionsSyntax.NotSplittableReason);
        }

        if (!FluentAssertionsSyntax.IsSafeToEvaluateTwice(subject, context.Model))
        {
            return PatternResult.Manual("OnlyContain(...) fails on an empty collection and ShouldAllBe does not: splitting into two statements would evaluate the subject twice.");
        }

        ArgumentSyntax[] emptyArguments = because is null ? [] : [because.WithoutTrivia()];
        ArgumentSyntax[] allArguments = [values[0].WithoutTrivia(), .. emptyArguments.Select(a => a.WithLeadingTrivia(SyntaxFactory.Space))];
        return PatternResult.Fixed(new[]
        {
            SyntaxFactory.ExpressionStatement(Call(subject, "ShouldNotBeEmpty", emptyArguments)),
            SyntaxFactory.ExpressionStatement(Call(subject, "ShouldAllBe", allArguments)),
        });
    }

    private static InvocationExpressionSyntax Call(ExpressionSyntax subject, string name, ArgumentSyntax[] arguments) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, subject.WithoutTrivia(), SyntaxFactory.IdentifierName(name)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
}
