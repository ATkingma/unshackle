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
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Throw", "ThrowExactly")
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

        if (name.Identifier.ValueText == "ThrowExactly")
        {
            return PatternResult.Manual("ThrowExactly<T> requires the exact exception type; Shouldly's Throw<T> also accepts derived types.");
        }

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

        if (FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(subject).Symbol))
        {
            return PatternResult.Manual("The subject is built with Invoking(...): pass the delegate to Should.Throw by hand.");
        }

        if (model.GetTypeInfo(subject).Type is not INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { Parameters.Length: 0 } invoke }
            || !invoke.ReturnsVoid && !invoke.ReturnType.IsReferenceType)
        {
            return PatternResult.Manual("The subject is not an Action or a Func returning a reference type, the only delegates Should.Throw accepts.");
        }

        if (ThrowMessage.MayThrowAggregate(subject, generic.TypeArgumentList.Arguments[0], model))
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
            ThrowMessage.Arguments([SyntaxFactory.Argument(subject.WithoutTrivia())], throwBecause));

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
