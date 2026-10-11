using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ThrowAsyncPattern : IPattern
{
    public string Id => "fa.throw-async";

    public string Description => "await act.Should().ThrowAsync<T>()[.WithMessage(p)] → await Should.ThrowAsync<T>(act)[.Message.Should...]";

    // The node is the `await` around the ThrowAsync call (or around the `.WithMessage(...)` call that directly follows it);
    // without an await it is the invocation itself. A `.ConfigureAwait(...)` between the call and the await is kept.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "ThrowAsync", "ThrowExactlyAsync")
            .Select(t => Follow(t, "WithMessage"))
            .Select(i => Follow(i, "ConfigureAwait"))
            .Select(i => i.Parent is AwaitExpressionSyntax await ? (SyntaxNode)await : i);

    // `call.method(...)` when that directly follows call, else call.
    private static InvocationExpressionSyntax Follow(InvocationExpressionSyntax call, string method) =>
        call.Parent is MemberAccessExpressionSyntax access
        && access.Name.Identifier.ValueText == method
        && access.Parent is InvocationExpressionSyntax next
        && next.Expression == access
            ? next
            : call;

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var awaitExpression = node as AwaitExpressionSyntax;
        var outer = (InvocationExpressionSyntax)(awaitExpression?.Expression ?? node);
        InvocationExpressionSyntax? configureAwait = null;
        if (outer.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait", Expression: InvocationExpressionSyntax configured })
        {
            (configureAwait, outer) = (outer, configured);
        }

        InvocationExpressionSyntax throwCall = outer.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WithMessage", Expression: InvocationExpressionSyntax inner }
            ? inner
            : outer;
        bool hasMessage = throwCall != outer;
        var name = (SimpleNameSyntax)((MemberAccessExpressionSyntax)throwCall.Expression).Name;

        if (name.Identifier.ValueText == "ThrowExactlyAsync")
        {
            return PatternResult.Manual("ThrowExactlyAsync<T> requires the exact exception type; Shouldly's ThrowAsync<T> also accepts derived types.");
        }

        if (name is not GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic)
        {
            return PatternResult.Manual("ThrowAsync without exactly one type argument.");
        }

        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(throwCall);
        if (subject is null)
        {
            return PatternResult.Manual("ThrowAsync<T>(...) is not in the form x.Should().ThrowAsync<T>(), e.g. after .And.");
        }

        if (awaitExpression is null)
        {
            return PatternResult.Manual("ThrowAsync<T>() is not directly awaited (no await, or followed by .Where/.WithInnerException): Shouldly's Should.ThrowAsync is not chained like that.");
        }

        if (awaitExpression.Parent is not ExpressionStatementSyntax)
        {
            return PatternResult.Manual("The result of the awaited ThrowAsync<T>() is used (.Which/.And/.WithInnerException/.Where or a value); Shouldly's Should.ThrowAsync is not chained like that.");
        }

        if (FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(subject).Symbol))
        {
            return PatternResult.Manual("The subject is built with Invoking(...) or Awaiting(...): pass the delegate to Should.ThrowAsync by hand.");
        }

        // Should.ThrowAsync takes a Func<Task>; Func<Task<T>> converts to it only when T is a reference type.
        if (model.GetTypeInfo(subject).Type is not INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { Parameters.Length: 0 } invoke }
            || invoke.ReturnType is not INamedTypeSymbol { Name: "Task", ContainingNamespace: { Name: "Tasks" } } task
            || task.TypeArguments is [{ IsReferenceType: false }])
        {
            return PatternResult.Manual("The subject is not a Func<Task> or a Func<Task<T>> with T a reference type, the only delegates Should.ThrowAsync accepts.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(throwCall, model, out IReadOnlyList<ArgumentSyntax> throwValues, out ArgumentSyntax? throwBecause)
            || throwValues.Count != 0)
        {
            return PatternResult.Manual("ThrowAsync<T>() with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        ExpressionSyntax shouldThrow = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Should"),
                SyntaxFactory.GenericName(SyntaxFactory.Identifier("ThrowAsync"), generic.TypeArgumentList)),
            ThrowMessage.Arguments([SyntaxFactory.Argument(subject.WithoutTrivia())], throwBecause));
        if (configureAwait is not null)
        {
            // Shouldly's ThrowAsync returns Task<TException>, so the same ConfigureAwait applies.
            shouldThrow = configureAwait.WithExpression(((MemberAccessExpressionSyntax)configureAwait.Expression).WithExpression(shouldThrow));
        }

        ExpressionSyntax thrown = SyntaxFactory.AwaitExpression(SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space), shouldThrow);

        if (!hasMessage)
        {
            return PatternResult.Fixed(thrown);
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

        return ThrowMessage.Check(SyntaxFactory.ParenthesizedExpression(thrown), pattern, because);
    }
}
