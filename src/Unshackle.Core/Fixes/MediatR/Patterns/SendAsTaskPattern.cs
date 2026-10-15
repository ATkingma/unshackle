using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class SendAsTaskPattern : IPattern
{
    private const string SetupManual =
        "Send/Publish is set up on an NSubstitute mock; after the swap it returns a ValueTask, so the configured value must become one "
        + "(ValueTask.FromResult(x), ValueTask.CompletedTask). Only Task.FromResult(x) and Task.CompletedTask on Publish are rewritten; change this setup by hand.";

    // NSubstitute's setup calls on a call's result; .AsTask() in between configures nothing (NS1000) (TAAK-211).
    private static readonly HashSet<string> s_returns = new(["Returns", "ReturnsForAnyArgs"], StringComparer.Ordinal);

    // Throw from the call itself; they work the same on a ValueTask-returning member.
    private static readonly HashSet<string> s_throws = new(["Throws", "ThrowsForAnyArgs", "ThrowsAsync", "ThrowsAsyncForAnyArgs"], StringComparer.Ordinal);

    // mock.Received().Send(x); checks a call, its result is not used.
    private static readonly HashSet<string> s_received = new(["Received", "DidNotReceive", "ReceivedWithAnyArgs", "DidNotReceiveWithAnyArgs"], StringComparer.Ordinal);

    public string Id => "mediatr.send-as-task";

    public string Description =>
        "Send/Publish, or a direct handler.Handle(...), whose result is used as a Task (not awaited directly) → .AsTask(); Mediator returns ValueTask. "
        + "An NSubstitute setup mock.Send(...).Returns(Task.FromResult(x)) → .Returns(ValueTask.FromResult(x))";

    // CreateStream returns IAsyncEnumerable in both libraries, so only Send and Publish change.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (IsHandlerHandle(invocation, model))
            {
                // handler.Handle(...) called directly (a unit test): the handler pattern makes it return a ValueTask too (TAAK-268 round 3).
                if (!IsAwaited(invocation) && Setup(invocation, model) is null && !IsReceivedCheck(invocation, model))
                {
                    yield return invocation;
                }

                continue;
            }

            if (invocation.Expression is not (MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Send" or "Publish" } or MemberBindingExpressionSyntax { Name.Identifier.ValueText: "Send" or "Publish" })
                || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingAssembly.Name: "MediatR" }
                || IsAwaited(invocation) || IsReceivedCheck(invocation, model))
            {
                continue;
            }

            if (Setup(invocation, model) is InvocationExpressionSyntax setup)
            {
                if (s_returns.Contains(SetupName(setup)))
                {
                    yield return setup;
                }

                continue;
            }

            yield return invocation;
        }
    }

    // A ValueTask may be awaited once and has no ContinueWith/Wait/WhenAll; .AsTask() keeps every Task use working.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var invocation = (InvocationExpressionSyntax)node;
        if (invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name } && s_returns.Contains(name))
        {
            return RewriteReturns(invocation, model);
        }

        if (invocation.Expression is MemberBindingExpressionSyntax)
        {
            // mediator?.Send(x) is a ValueTask? after the swap; .AsTask() needs a null check the caller has to choose.
            return PatternResult.Manual("Send/Publish is called with ?. ; Mediator returns a ValueTask, so handle the null case and convert with .AsTask() by hand.");
        }

        return PatternResult.Fixed(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    invocation.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("AsTask"))).WithTriviaFrom(invocation));
    }

    // Handle of an own request or notification handler that returns Task or Task<T>: the handler patterns turn it into ValueTask.
    private static bool IsHandlerHandle(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Handle" }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ReturnType: INamedTypeSymbol { Name: "Task" } returnType } method
        && returnType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
        && method.ContainingType.Locations.Any(l => l.IsInSource)
        && method.ContainingType.AllInterfaces.Any(i =>
            i.MetadataName is "IRequestHandler`1" or "IRequestHandler`2" or "INotificationHandler`1"
            && i.ContainingNamespace.ToDisplayString() == "MediatR"
            && i.GetMembers("Handle").Any(m => SymbolEqualityComparer.Default.Equals(method.ContainingType.FindImplementationForInterfaceMember(m), method.OriginalDefinition)));

    // await x and await x.ConfigureAwait(...) work the same on a ValueTask.
    private static bool IsAwaited(InvocationExpressionSyntax invocation) => invocation.Parent switch
    {
        AwaitExpressionSyntax => true,
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait", Parent: InvocationExpressionSyntax { Parent: AwaitExpressionSyntax } } => true,
        ParenthesizedExpressionSyntax parenthesized => parenthesized.Parent is AwaitExpressionSyntax,
        _ => false,
    };

    private static bool IsReceivedCheck(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Parent is ExpressionStatementSyntax
        && invocation.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var check } } received }
        && s_received.Contains(check)
        && IsNSubstitute(received, model);

    // mock.Send(...).Returns(...) or one of the other setup calls on the call's result; null when it is no NSubstitute setup.
    private static InvocationExpressionSyntax? Setup(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name, Parent: InvocationExpressionSyntax setup } member
        && member.Expression == invocation
        && (s_returns.Contains(name) || s_throws.Contains(name))
        && IsNSubstitute(setup, model)
            ? setup
            : null;

    private static string SetupName(InvocationExpressionSyntax setup) => ((MemberAccessExpressionSyntax)setup.Expression).Name.Identifier.ValueText;

    private static bool IsNSubstitute(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
        && method.ContainingNamespace.ToDisplayString().StartsWith("NSubstitute", StringComparison.Ordinal);

    // The configured values follow the call's new return type: Task.FromResult(x) → ValueTask.FromResult(x), Task.CompletedTask → ValueTask.CompletedTask (Publish only;
    // a void Send returns ValueTask<Unit> in Mediator).
    private static PatternResult RewriteReturns(InvocationExpressionSyntax setup, SemanticModel model)
    {
        var call = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)setup.Expression).Expression;
        bool generic = model.GetSymbolInfo(call).Symbol is IMethodSymbol { ReturnType: INamedTypeSymbol { IsGenericType: true } };
        var values = new Dictionary<ExpressionSyntax, ExpressionSyntax>();
        foreach (ArgumentSyntax argument in setup.ArgumentList.Arguments)
        {
            if (argument.NameColon is not null || ValueTaskOf(argument.Expression, generic, model) is not ExpressionSyntax value)
            {
                return PatternResult.Manual(SetupManual);
            }

            values[argument.Expression] = value;
        }

        if (values.Count == 0)
        {
            return PatternResult.Manual(SetupManual);
        }

        return PatternResult.Fixed(setup.ReplaceNodes(values.Keys, (original, _) => values[original]));
    }

    private static ExpressionSyntax? ValueTaskOf(ExpressionSyntax expression, bool generic, SemanticModel model)
    {
        switch (expression)
        {
            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "FromResult" } fromResult } when generic && IsTask(fromResult.Expression, model):
                return expression.ReplaceNode(fromResult.Expression, ValueTask(fromResult.Expression));
            case MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CompletedTask" } completed when !generic && IsTask(completed.Expression, model):
                return expression.ReplaceNode(completed.Expression, ValueTask(completed.Expression));
            default:
                return null;
        }
    }

    private static bool IsTask(ExpressionSyntax expression, SemanticModel model) =>
        expression is IdentifierNameSyntax or QualifiedNameSyntax or AliasQualifiedNameSyntax or MemberAccessExpressionSyntax
        && model.GetSymbolInfo(expression).Symbol is INamedTypeSymbol { Name: "Task", Arity: 0 } task
        && task.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";

    // Task → ValueTask, keeping a qualifier the code wrote (System.Threading.Tasks.Task).
    private static ExpressionSyntax ValueTask(ExpressionSyntax task) => task switch
    {
        MemberAccessExpressionSyntax member => member.WithName(SyntaxFactory.IdentifierName("ValueTask").WithTriviaFrom(member.Name)),
        QualifiedNameSyntax qualified => qualified.WithRight(SyntaxFactory.IdentifierName("ValueTask").WithTriviaFrom(qualified.Right)),
        AliasQualifiedNameSyntax alias => alias.WithName(SyntaxFactory.IdentifierName("ValueTask").WithTriviaFrom(alias.Name)),
        _ => SyntaxFactory.IdentifierName("ValueTask").WithTriviaFrom(task),
    };
}
