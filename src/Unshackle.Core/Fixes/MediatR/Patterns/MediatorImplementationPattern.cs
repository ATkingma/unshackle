using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>
/// A class of the user's own that implements MediatR's <c>IMediator</c>, <c>ISender</c> or <c>IPublisher</c> (ardalis: a <c>NoOpMediator</c> for unit
/// tests) no longer compiles against Mediator's interfaces: other members (<c>ICommand</c>, <c>IQuery</c> and stream overloads) and
/// <c>ValueTask</c>. A no-op implementation, where every member only completes (<c>Task.CompletedTask</c>, <c>Task.FromResult(default)</c>,
/// <c>yield break</c>), gets Mediator's members as no-ops; an implementation with logic is Manual (TAAK-197).
/// </summary>
public class MediatorImplementationPattern : IPattern
{
    public string Id => "mediatr.mediator-implementation";

    public string Description => "No-op class : IMediator / ISender / IPublisher → Mediator 3.0.2's members (ValueTask, ICommand/IQuery/stream overloads) as no-ops";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => model.GetDeclaredSymbol(c) is INamedTypeSymbol type && type.Interfaces.Any(IsMediatorInterface));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var type = (ClassDeclarationSyntax)node;
        var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(type)!;
        bool sender = symbol.AllInterfaces.Any(i => MediatRSyntax.IsMediatR(i, "ISender"));
        bool publisher = symbol.AllInterfaces.Any(i => MediatRSyntax.IsMediatR(i, "IPublisher"));

        var implementations = new List<MethodDeclarationSyntax>();
        foreach (IMethodSymbol member in symbol.AllInterfaces.Where(i => MediatRSyntax.IsMediatR(i, "ISender", "IPublisher")).SelectMany(i => i.GetMembers().OfType<IMethodSymbol>()))
        {
            if (symbol.FindImplementationForInterfaceMember(member) is not IMethodSymbol implementation
                || !SymbolEqualityComparer.Default.Equals(implementation.ContainingType, symbol)
                || implementation.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax method
                || !type.Members.Contains(method))
            {
                return PatternResult.Manual($"{symbol.Name}.{member.Name} is implemented in a base class or another part of the class; rewrite it against Mediator's {member.ContainingType.Name} by hand.");
            }

            if (method.ExplicitInterfaceSpecifier is not null)
            {
                return PatternResult.Manual($"{symbol.Name} implements {member.ContainingType.Name}.{member.Name} explicitly; rewrite it against Mediator's interface by hand.");
            }

            if (!IsNoOp(method, model))
            {
                return PatternResult.Manual($"{symbol.Name}.{method.Identifier.ValueText} does more than complete; Mediator's {member.ContainingType.Name} has other members (ValueTask, ICommand/IQuery/stream overloads). Rewrite {symbol.Name} by hand.");
            }

            if (!implementations.Contains(method))
            {
                implementations.Add(method);
            }
        }

        MethodDeclarationSyntax first = type.Members.OfType<MethodDeclarationSyntax>().First(implementations.Contains);
        string indentation = string.Concat(first.GetLeadingTrivia().Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse());
        string endOfLine = first.GetTrailingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToFullString() is { Length: > 0 } eol ? eol : "\n";
        bool nullable = model.GetNullableContext(first.SpanStart).AnnotationsEnabled();
        List<MemberDeclarationSyntax> members = [.. Members(sender, publisher, nullable ? "object?" : "object", indentation, endOfLine)];

        // The first new member takes the place (and the leading trivia) of the first old one; the others follow after a blank line.
        members[0] = members[0].WithLeadingTrivia(first.GetLeadingTrivia());
        int index = type.Members.IndexOf(first);
        int insertAt = type.Members.Take(index).Count(m => m is not MethodDeclarationSyntax method || !implementations.Contains(method));
        SyntaxList<MemberDeclarationSyntax> kept = [.. type.Members.Where(m => m is not MethodDeclarationSyntax method || !implementations.Contains(method))];
        ClassDeclarationSyntax rewritten = type.WithMembers(kept.InsertRange(insertAt, members));
        // CreateStream needs IAsyncEnumerable, [EnumeratorCancellation] and Task; only an ISender has it.
        return sender
            ? PatternResult.Fixed(rewritten, "System.Collections.Generic", "System.Runtime.CompilerServices", "System.Threading", "System.Threading.Tasks")
            : PatternResult.Fixed(rewritten, "System.Threading", "System.Threading.Tasks");
    }

    private static bool IsMediatorInterface(INamedTypeSymbol type) => MediatRSyntax.IsMediatR(type, "IMediator", "ISender", "IPublisher");

    // Only completes: `return Task.CompletedTask;`, `return Task.FromResult(default);`, `await Task.CompletedTask;`, `yield break;`, or nothing.
    private static bool IsNoOp(MethodDeclarationSyntax method, SemanticModel model)
    {
        if (method.ExpressionBody is { Expression: var expression })
        {
            return Completes(expression, model);
        }

        return method.Body is { } body && body.Statements.All(s => s switch
        {
            ReturnStatementSyntax { Expression: { } returned } => Completes(returned, model),
            YieldStatementSyntax yield => yield.IsKind(SyntaxKind.YieldBreakStatement),
            ExpressionStatementSyntax { Expression: AwaitExpressionSyntax { Expression: var awaited } } => IsCompletedTask(awaited, model),
            _ => false,
        });
    }

    private static bool Completes(ExpressionSyntax expression, SemanticModel model) =>
        IsCompletedTask(expression, model)
        || expression is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: var argument }] } invocation
            && MediatRSyntax.IsTaskMember(invocation, model, "FromResult")
            && Unwrap(argument) is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression or (int)SyntaxKind.NullLiteralExpression } or DefaultExpressionSyntax;

    private static bool IsCompletedTask(ExpressionSyntax expression, SemanticModel model) =>
        expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CompletedTask" } && MediatRSyntax.IsTaskMember(expression, model, "CompletedTask");

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression) =>
        expression is PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression, Operand: var operand } ? operand : expression;

    // Mediator 3.0.2's ISender and IPublisher (parameter names and defaults as there), each completing without doing anything.
    private static IEnumerable<MemberDeclarationSyntax> Members(bool sender, bool publisher, string obj, string indentation, string endOfLine)
    {
        var members = new List<string>();
        if (sender)
        {
            foreach ((string message, string parameter) in new[] { ("IRequest", "request"), ("ICommand", "command"), ("IQuery", "query") })
            {
                members.Add($"public ValueTask<TResponse> Send<TResponse>({message}<TResponse> {parameter}, CancellationToken cancellationToken = default) => default;");
            }

            members.Add($"public ValueTask<{obj}> Send(object message, CancellationToken cancellationToken = default) => default;");
            foreach ((string message, string parameter) in new[] { ("IStreamRequest", "request"), ("IStreamCommand", "command"), ("IStreamQuery", "query") })
            {
                members.Add(Stream($"CreateStream<TResponse>({message}<TResponse> {parameter}", "TResponse"));
            }

            members.Add(Stream("CreateStream(object message", obj));
        }

        if (publisher)
        {
            members.Add($"public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default){endOfLine}{indentation}    where TNotification : INotification => default;");
            members.Add("public ValueTask Publish(object notification, CancellationToken cancellationToken = default) => default;");
        }

        for (int i = 0; i < members.Count; i++)
        {
            string leading = i == 0 ? indentation : endOfLine + indentation;
            yield return SyntaxFactory.ParseMemberDeclaration(leading + members[i] + endOfLine)!;
        }

        string Stream(string signature, string element) =>
            $"public async IAsyncEnumerable<{element}> {signature}, [EnumeratorCancellation] CancellationToken cancellationToken = default){endOfLine}"
            + $"{indentation}{{{endOfLine}{indentation}    await Task.CompletedTask;{endOfLine}{indentation}    yield break;{endOfLine}{indentation}}}";
    }
}
