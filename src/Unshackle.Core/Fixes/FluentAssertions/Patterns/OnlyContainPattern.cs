using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class OnlyContainPattern : IPattern
{
    public string Id => "fa.only-contain";

    public string Description => "x.Should().OnlyContain(pred) → x.ShouldNotBeEmpty(); x.ShouldAllBe(pred); (FluentAssertions fails on an empty collection)";

    // The node is the whole statement when it can be split into two statements, otherwise the invocation.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "OnlyContain")
            .Select<InvocationExpressionSyntax, SyntaxNode>(i =>
                i.Parent is ExpressionStatementSyntax { Parent: BlockSyntax or SwitchSectionSyntax } statement ? statement : i);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var statement = node as ExpressionStatementSyntax;
        var assertion = (InvocationExpressionSyntax)(statement?.Expression ?? node);
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual("OnlyContain(...) is not in the form x.Should().OnlyContain(...), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual("The result of OnlyContain(...) is used (.And/.Which or a value); Shouldly's ShouldAllBe returns nothing.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual("OnlyContain(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        ITypeSymbol? type = model.GetTypeInfo(subject).Type;
        if (values.Count != 1 || type is null or IErrorTypeSymbol
            || !type.AllInterfaces.Prepend(type).Any(t => t.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T))
        {
            return PatternResult.Manual("The subject is not a generic IEnumerable<T> or the argument list is unexpected.");
        }

        // FluentAssertions fails on an empty collection, ShouldAllBe passes: a ShouldNotBeEmpty statement keeps the behaviour.
        if (statement is null)
        {
            return PatternResult.Manual("OnlyContain(...) fails on an empty collection and ShouldAllBe does not: add ShouldNotBeEmpty by hand, the assertion is not a statement directly in a block (e.g. a lambda body) so it cannot hold two statements.");
        }

        if (!IsSimple(subject))
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

    // Names, `this` and member accesses on those: safe to evaluate twice (shortcut: property getters are assumed pure).
    private static bool IsSimple(ExpressionSyntax e) => e switch
    {
        IdentifierNameSyntax or ThisExpressionSyntax => true,
        MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Name: IdentifierNameSyntax } access => IsSimple(access.Expression),
        _ => false,
    };
}
