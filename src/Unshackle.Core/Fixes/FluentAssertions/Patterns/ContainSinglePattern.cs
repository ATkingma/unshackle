using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ContainSinglePattern : IPattern
{
    public string Id => "fa.contain-single-which";

    public string Description => "x.Should().ContainSingle() → x.ShouldHaveSingleItem(), ContainSingle(p) → x.ShouldContain(p, 1), var y = ….Which → var y = x.Where(p).ShouldHaveSingleItem()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "ContainSingle")
            .Select<InvocationExpressionSyntax, SyntaxNode>(i => i.Parent is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Which",
                Parent: EqualsValueClauseSyntax,
            } which
                ? which
                : i);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        // `var item = x.Should().ContainSingle().Which;`: ShouldHaveSingleItem returns the item itself.
        var which = node as MemberAccessExpressionSyntax;
        var assertion = (InvocationExpressionSyntax)(which?.Expression ?? node);
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual("ContainSingle(...) is not in the form x.Should().ContainSingle(...), e.g. after .And.");
        }

        if (which is null
            ? !FluentAssertionsSyntax.IsResultDiscarded(assertion, model)
            : which.Parent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Variables.Count: 1 } } })
        {
            return PatternResult.Manual("The result of ContainSingle(...) is used beyond `var x = ….Which;` (.And/.Which chain): rewrite it by hand.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual("ContainSingle(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        if (model.GetTypeInfo(subject).Type is not { } subjectType || !subjectType.AllInterfaces.Prepend(subjectType).Any(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T))
        {
            return PatternResult.Manual("ContainSingle(...) on a non-generic collection: Shouldly needs IEnumerable<T>.");
        }

        ExpressionSyntax receiver = FluentAssertionsSyntax.NotNull(subject, model).WithoutTrailingTrivia();
        ArgumentSyntax[] becauseArgument = because is null ? [] : [because];
        if (values.Count == 0)
        {
            return PatternResult.Fixed(Call(receiver, "ShouldHaveSingleItem", becauseArgument));
        }

        ArgumentSyntax predicate = values[0];
        if (which is null)
        {
            // ShouldContain(predicate, 1) takes the same Expression<Func<T, bool>> and also fails on zero or several matches.
            ArgumentSyntax once = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
            return PatternResult.Fixed(Call(receiver, "ShouldContain", [predicate, once, .. becauseArgument]));
        }

        if (predicate.Expression is not AnonymousFunctionExpressionSyntax)
        {
            return PatternResult.Manual("ContainSingle(predicate).Which with an Expression variable: Where needs a Func; rewrite it by hand.");
        }

        return PatternResult.Fixed(Call(Call(receiver, "Where", [predicate]), "ShouldHaveSingleItem", becauseArgument), "System.Linq");
    }

    private static InvocationExpressionSyntax Call(ExpressionSyntax receiver, string name, ArgumentSyntax[] arguments) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(name)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                arguments.Select((a, i) => i == 0 ? a.WithoutTrivia() : a.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space)))));
}
