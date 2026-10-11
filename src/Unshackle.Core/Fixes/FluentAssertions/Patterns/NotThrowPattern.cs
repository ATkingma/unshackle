using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class NotThrowPattern : IPattern
{
    public string Id => "fa.not-throw";

    public string Description => "act.Should().NotThrow() → Should.NotThrow(act)";

    // Invoking subjects belong to InvokingPattern, which calls Rewrite below.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        ThrowSyntax.FindNotThrow(root, model, t => !ThrowSyntax.IsInvoking(t, model));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var call = (InvocationExpressionSyntax)node;
        var name = (SimpleNameSyntax)((MemberAccessExpressionSyntax)call.Expression).Name;

        if (name.Identifier.ValueText == "NotThrowAfter")
        {
            return PatternResult.Manual("NotThrowAfter waits and polls; Shouldly has no equivalent.");
        }

        if (name is GenericNameSyntax)
        {
            return PatternResult.Manual("NotThrow<T>() only forbids exception type T; Shouldly's Should.NotThrow forbids every exception.");
        }

        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(call);
        if (subject is null)
        {
            return PatternResult.Manual("NotThrow() is not in the form x.Should().NotThrow(), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(call, model))
        {
            return PatternResult.Manual("The result of NotThrow() is used (.Which/.And or a value); Should.NotThrow is not chained like that.");
        }

        ExpressionSyntax action = subject;
        if (ThrowSyntax.IsInvoking(call, model))
        {
            if (!ThrowSyntax.TryGetAction(subject, model, out action, out _, out string reason))
            {
                return PatternResult.Manual(reason);
            }
        }
        else if (FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(subject).Symbol))
        {
            return PatternResult.Manual("The subject is built with FluentAssertions (e.g. FluentActions.Invoking): pass the delegate to Should.NotThrow by hand.");
        }

        if (model.GetTypeInfo(subject).Type is not INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod.Parameters.Length: 0 })
        {
            return PatternResult.Manual("The subject is not an parameterless Action or Func, the only delegates Should.NotThrow accepts.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(call, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because)
            || values.Count != 0)
        {
            return PatternResult.Manual("NotThrow() with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        ArgumentSyntax[] arguments = because is null
            ? [SyntaxFactory.Argument(action.WithoutTrivia())]
            : [SyntaxFactory.Argument(action.WithoutTrivia()), because.WithLeadingTrivia(SyntaxFactory.Space)];

        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Should"),
                SyntaxFactory.IdentifierName("NotThrow")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))));
    }
}
