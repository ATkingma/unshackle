using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Both fail on any exception, AggregateException included (probed), so the aggregate check of ThrowAsync is not needed.
public class NotThrowAsyncPattern : IPattern
{
    public string Id => "fa.not-throw-async";

    public string Description => "await act.Should().NotThrowAsync() → await Should.NotThrowAsync(act)";

    // The node is the `await` around the call (a `.ConfigureAwait(...)` in between is kept); without an await the call itself.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "NotThrowAsync", "NotThrowAfterAsync")
            .Select(i => ThrowSyntax.Follow(i, "ConfigureAwait"))
            .Select(i => i.Parent is AwaitExpressionSyntax await ? (SyntaxNode)await : i);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        if (node is not AwaitExpressionSyntax { Parent: ExpressionStatementSyntax } awaitExpression)
        {
            return PatternResult.Manual("NotThrowAsync() is not awaited as a statement (no await, or its result is used); Should.NotThrowAsync is not chained like that.");
        }

        var call = (InvocationExpressionSyntax)awaitExpression.Expression;
        InvocationExpressionSyntax? configureAwait = null;
        if (call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait", Expression: InvocationExpressionSyntax configured })
        {
            (configureAwait, call) = (call, configured);
        }

        var name = (SimpleNameSyntax)((MemberAccessExpressionSyntax)call.Expression).Name;
        if (name.Identifier.ValueText == "NotThrowAfterAsync")
        {
            return PatternResult.Manual("NotThrowAfterAsync waits and polls; Shouldly has no equivalent.");
        }

        if (name is GenericNameSyntax)
        {
            return PatternResult.Manual("NotThrowAsync<T>() only forbids exception type T; Shouldly's Should.NotThrowAsync forbids every exception.");
        }

        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(call);
        if (subject is null)
        {
            return PatternResult.Manual("NotThrowAsync() is not in the form act.Should().NotThrowAsync(), e.g. after .And.");
        }

        if (FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(subject).Symbol))
        {
            return PatternResult.Manual("The subject is built with FluentAssertions (e.g. FluentActions.Awaiting): pass the delegate to Should.NotThrowAsync by hand.");
        }

        // Should.NotThrowAsync takes a Func<Task>; Func<Task<T>> converts to it only when T is a reference type.
        if (model.GetTypeInfo(subject).Type is not INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { Parameters.Length: 0 } invoke }
            || invoke.ReturnType is not INamedTypeSymbol { Name: "Task", ContainingNamespace.Name: "Tasks" } task
            || task.TypeArguments is [{ IsReferenceType: false }])
        {
            return PatternResult.Manual("The subject is not a Func<Task> or a Func<Task<T>> with T a reference type, the only delegates Should.NotThrowAsync accepts.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(call, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because)
            || values.Count != 0)
        {
            return PatternResult.Manual("NotThrowAsync() with named arguments, or becauseArgs whose because is no constant format string: rewrite it by hand.");
        }

        ExpressionSyntax notThrow = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Should"),
                SyntaxFactory.IdentifierName("NotThrowAsync")),
            FluentAssertionsSyntax.Arguments(SyntaxFactory.Argument(subject.WithoutTrivia()), because));
        if (configureAwait is not null)
        {
            notThrow = configureAwait.WithExpression(((MemberAccessExpressionSyntax)configureAwait.Expression).WithExpression(notThrow));
        }

        return PatternResult.Fixed(SyntaxFactory.AwaitExpression(SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space), notThrow));
    }
}
