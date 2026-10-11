using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ThrowPattern : IPattern
{
    public string Id => "fa.throw";

    public string Description => "act.Should().Throw<T>()[.WithMessage(p)] → Should.Throw<T>(act)[.Message.Should...]";

    // The node is the Throw call, or the whole `.WithMessage(...)` call when that directly follows it.
    // ThrowExactly and Invoking subjects belong to ThrowExactlyPattern and InvokingPattern, which call Rewrite below.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        Find(root, model, t => !ThrowExactlyPattern.IsExactly(t) && !InvokingPattern.IsInvoking(t, model));

    internal static IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model, Func<InvocationExpressionSyntax, bool> owns) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Throw", "ThrowExactly")
            .Where(owns)
            .Select(t => t.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WithMessage" } access
                && access.Parent is InvocationExpressionSyntax { } withMessage
                && withMessage.Expression == access
                    ? withMessage
                    : t);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var outer = (InvocationExpressionSyntax)node;
        InvocationExpressionSyntax throwCall = outer.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WithMessage", Expression: InvocationExpressionSyntax inner }
            ? inner
            : outer;
        bool hasMessage = throwCall != outer;
        var name = (SimpleNameSyntax)((MemberAccessExpressionSyntax)throwCall.Expression).Name;

        bool exactly = name.Identifier.ValueText == "ThrowExactly";
        if (name is not GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic)
        {
            return PatternResult.Manual("Throw without exactly one type argument.");
        }

        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(throwCall);
        if (subject is null)
        {
            return PatternResult.Manual("Throw<T>(...) is not in the form x.Should().Throw<T>(), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(outer, model))
        {
            return PatternResult.Manual("The result of Throw<T>() is used (.Which/.And/.WithInnerException/.Where or a value); Shouldly's Should.Throw is not chained like that.");
        }

        ExpressionSyntax action = subject;
        ExpressionSyntax inspected = subject;
        if (InvokingPattern.IsInvoking(throwCall, model))
        {
            if (!InvokingPattern.TryGetAction(subject, model, out action, out AnonymousFunctionExpressionSyntax lambda, out string reason))
            {
                return PatternResult.Manual(reason);
            }

            inspected = lambda;
        }
        else if (FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(subject).Symbol))
        {
            return PatternResult.Manual("The subject is built with FluentAssertions (e.g. FluentActions.Invoking): pass the delegate to Should.Throw by hand.");
        }

        if (model.GetTypeInfo(subject).Type is not INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { Parameters.Length: 0 } invoke }
            || !invoke.ReturnsVoid && !invoke.ReturnType.IsReferenceType)
        {
            return PatternResult.Manual("The subject is not an Action or a Func returning a reference type, the only delegates Should.Throw accepts.");
        }

        // ThrowExactly does not unwrap AggregateException, just like Shouldly.
        if (!exactly && ThrowMessage.MayThrowAggregate(inspected, generic.TypeArgumentList.Arguments[0], model))
        {
            return PatternResult.Manual(ThrowMessage.AggregateReason);
        }

        if (!FluentAssertionsSyntax.TryGetArguments(throwCall, model, out IReadOnlyList<ArgumentSyntax> throwValues, out ArgumentSyntax? throwBecause)
            || throwValues.Count != 0)
        {
            return PatternResult.Manual("Throw<T>() with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        ExpressionSyntax result = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Should"),
                SyntaxFactory.GenericName(SyntaxFactory.Identifier("Throw"), generic.TypeArgumentList)),
            ThrowMessage.Arguments([SyntaxFactory.Argument(action.WithoutTrivia())], throwBecause));
        if (exactly)
        {
            result = ThrowMessage.OfType(result, generic);
        }

        if (!hasMessage)
        {
            return PatternResult.Fixed(result);
        }

        if (!FluentAssertionsSyntax.TryGetArguments(outer, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because)
            || values.Count != 1)
        {
            return PatternResult.Manual("WithMessage(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        if (model.GetConstantValue(values[0].Expression) is not { Value: string pattern })
        {
            return PatternResult.Manual("WithMessage(...) pattern is not a string constant, so its wildcards cannot be translated.");
        }

        return ThrowMessage.Check(result, pattern, because);
    }
}
