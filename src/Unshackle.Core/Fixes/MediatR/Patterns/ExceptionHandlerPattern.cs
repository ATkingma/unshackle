using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class ExceptionHandlerPattern : IPattern
{
    private const string InterfaceName = "IRequestExceptionHandler`3";

    public string Id => "mediatr.exception-handler";

    public string Description =>
        "IRequestExceptionHandler<TReq, TRes, TEx>: Task Handle(request, exception, state, ct) with state.SetHandled(x); return Task.CompletedTask → MessageExceptionHandler<TReq, TRes, TEx> with return new(Handled(x)) / new(NotHandled)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c =>
            model.GetDeclaredSymbol(c) is { } symbol && MediatRSyntax.DeclaredInterface(symbol, InterfaceName) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var type = (ClassDeclarationSyntax)node;
        INamedTypeSymbol symbol = model.GetDeclaredSymbol(type)!;
        INamedTypeSymbol handler = MediatRSyntax.DeclaredInterface(symbol, InterfaceName)!;
        MethodDeclarationSyntax? method = symbol.FindImplementationForInterfaceMember(handler.GetMembers("Handle")[0])?.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax()).OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Parent == type);
        if (symbol.IsAbstract || method is null)
        {
            return PatternResult.Manual("Abstract exception handler or Handle not declared in this class; rewrite it by hand as a MessageExceptionHandler.");
        }

        if (MediatRSyntax.RejectExplicit(method, "rewrite the exception handler by hand.") is { } explicitHandle)
        {
            return explicitHandle;
        }

        if (method.Modifiers.Any(SyntaxKind.AsyncKeyword) || method.Body is null || method.ParameterList.Parameters.Count != 4)
        {
            return PatternResult.Manual("Handle is async or has no statement body; write it as a MessageExceptionHandler returning Handled(x) or NotHandled by hand.");
        }

        // Every use of state must be "state.SetHandled(x);" directly followed by "return Task.CompletedTask;"; any other return is NotHandled.
        IParameterSymbol state = model.GetDeclaredSymbol(method.ParameterList.Parameters[2])!;
        List<IdentifierNameSyntax> stateUses =
        [
            .. method.Body.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, state)),
        ];
        List<(ExpressionStatementSyntax SetHandled, ReturnStatementSyntax Return)> handled = [];
        foreach (IdentifierNameSyntax use in stateUses)
        {
            if (use.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "SetHandled", Parent: InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1, Parent: ExpressionStatementSyntax statement } }
                || statement.Parent is not BlockSyntax block
                || block.Statements.ElementAtOrDefault(block.Statements.IndexOf(statement) + 1) is not ReturnStatementSyntax returned)
            {
                return PatternResult.Manual("state is used other than as state.SetHandled(x) followed by return Task.CompletedTask; return Handled(x) or NotHandled by hand.");
            }

            handled.Add((statement, returned));
        }

        List<ReturnStatementSyntax> returns = [.. method.Body.DescendantNodes(n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax).OfType<ReturnStatementSyntax>()];
        if (returns.Any(r => r.Expression is not MemberAccessExpressionSyntax || !MediatRSyntax.IsTaskMember(r.Expression, model, "CompletedTask")))
        {
            return PatternResult.Manual("Handle returns a Task that is not Task.CompletedTask; return Handled(x) or NotHandled by hand.");
        }

        var removed = new SyntaxAnnotation();
        BlockSyntax body = method.Body.ReplaceNodes(
            returns.Cast<SyntaxNode>().Concat(handled.Select(h => h.SetHandled)),
            (original, current) => original switch
            {
                // The return takes over the leading trivia (blank line, comments) of the SetHandled call that goes.
                ReturnStatementSyntax r when handled.FirstOrDefault(h => h.Return == r).SetHandled is { } setHandled =>
                    ((ReturnStatementSyntax)current).WithExpression(Result(setHandled).WithTriviaFrom(r.Expression!)).WithLeadingTrivia(setHandled.GetLeadingTrivia()),
                ReturnStatementSyntax r => ((ReturnStatementSyntax)current).WithExpression(Result(null).WithTriviaFrom(r.Expression!)),
                _ => current.WithAdditionalAnnotations(removed),
            });
        body = body.RemoveNodes(body.GetAnnotatedNodes(removed), SyntaxRemoveOptions.KeepNoTrivia)!;

        TypeSyntax response = SyntaxFactory.ParseTypeName(handler.TypeArguments[1].ToMinimalDisplayString(model, type.SpanStart));
        TypeSyntax returnType = SyntaxFactory.ParseTypeName($"ValueTask<ExceptionHandlingResult<{response}>>");
        MethodDeclarationSyntax rewritten = MediatRSyntax.AsHandleOverride(method.WithBody(body), returnType);
        rewritten = rewritten.WithParameterList(rewritten.ParameterList.WithParameters(rewritten.ParameterList.Parameters.RemoveAt(2)));

        GenericNameSyntax baseClass = SyntaxFactory.GenericName(SyntaxFactory.Identifier("MessageExceptionHandler"))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(MediatRSyntax.CommaList(
                [.. handler.TypeArguments.Select(t => SyntaxFactory.ParseTypeName(t.ToMinimalDisplayString(model, type.SpanStart)))])));
        ClassDeclarationSyntax result = type.ReplaceNode(method, rewritten);
        if (MediatRSyntax.WithBaseClass(type, result, handler, baseClass, model) is not ClassDeclarationSyntax withBase)
        {
            return PatternResult.Manual("The exception handler already has a base class; Mediator's MessageExceptionHandler must be it. Rewrite it by hand.");
        }

        return PatternResult.Fixed(
            MediatRSyntax.WithConstraint(withBase, handler.TypeArguments[0], "IMessage", t => MediatRSyntax.IsMediatR(t, "IBaseRequest", "INotification")),
            "System.Threading.Tasks");
    }

    private static ExpressionSyntax Result(ExpressionStatementSyntax? setHandled) =>
        SyntaxFactory.ParseExpression(setHandled?.Expression is InvocationExpressionSyntax call
            ? $"new(Handled({call.ArgumentList.Arguments[0]}))"
            : "new(NotHandled)");
}
