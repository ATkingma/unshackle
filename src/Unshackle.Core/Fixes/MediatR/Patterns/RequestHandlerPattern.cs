using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class RequestHandlerPattern : IPattern
{
    public string Id => "mediatr.request-handler";

    public string Description => "IRequestHandler<TReq, TRes>: Task<TRes> Handle(...) → ValueTask<TRes> Handle(...), Task.FromResult → ValueTask.FromResult";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(type) is not INamedTypeSymbol symbol)
            {
                continue;
            }

            foreach (INamedTypeSymbol handler in symbol.Interfaces.Where(IsRequestHandler))
            {
                ISymbol? handle = handler.GetMembers("Handle").FirstOrDefault();
                ISymbol? implementation = handle is null ? null : symbol.FindImplementationForInterfaceMember(handle);
                MethodDeclarationSyntax? declaration = implementation?.DeclaringSyntaxReferences
                    .Select(r => r.GetSyntax())
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.SyntaxTree == root.SyntaxTree && type.Members.Contains(m));
                if (declaration is not null)
                {
                    yield return declaration;
                }
            }
        }
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var method = (MethodDeclarationSyntax)node;
        if (method.ExplicitInterfaceSpecifier is not null)
        {
            return PatternResult.Manual("Handle is an explicit interface implementation; rename the interface and return type by hand.");
        }

        TypeSyntax? returnType = ToValueTask(method.ReturnType);
        if (returnType is null)
        {
            return PatternResult.Manual("Handle does not return Task<T> in the form Task<T>; change it to ValueTask<T> by hand.");
        }

        if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            // await and return x work the same in an async ValueTask<T> method.
            return PatternResult.Fixed(method.WithReturnType(returnType.WithTriviaFrom(method.ReturnType)));
        }

        List<ExpressionSyntax> returned = ReturnedExpressions(method);
        if (returned.Any(e => !IsTaskFromResult(e, model)))
        {
            return PatternResult.Manual("Handle returns a Task that is not Task.FromResult(...); Mediator needs a ValueTask<T>, convert it by hand (e.g. make the method async).");
        }

        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            returned.Select(e => ((InvocationExpressionSyntax)e).Expression),
            (original, _) => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("ValueTask"),
                ((MemberAccessExpressionSyntax)original).Name).WithTriviaFrom(original));
        return PatternResult.Fixed(rewritten.WithReturnType(returnType.WithTriviaFrom(method.ReturnType)), "System.Threading.Tasks");
    }

    private static bool IsRequestHandler(INamedTypeSymbol type) =>
        type.OriginalDefinition.MetadataName == "IRequestHandler`2"
        && type.ContainingNamespace.ToDisplayString() == "MediatR";

    // Task<T> or System.Threading.Tasks.Task<T> → ValueTask<T>; anything else → null.
    private static TypeSyntax? ToValueTask(TypeSyntax type) => type switch
    {
        GenericNameSyntax { Identifier.ValueText: "Task" } generic =>
            generic.WithIdentifier(SyntaxFactory.Identifier("ValueTask")),
        QualifiedNameSyntax { Right: GenericNameSyntax { Identifier.ValueText: "Task" } right } qualified =>
            qualified.WithRight(right.WithIdentifier(SyntaxFactory.Identifier("ValueTask"))),
        _ => null,
    };

    // The expression body, or every return in the body except those of nested lambdas and local functions.
    private static List<ExpressionSyntax> ReturnedExpressions(MethodDeclarationSyntax method)
    {
        if (method.ExpressionBody is not null)
        {
            return [method.ExpressionBody.Expression];
        }

        List<ExpressionSyntax> returned = [];
        foreach (ReturnStatementSyntax statement in method.Body?.DescendantNodes(n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
            .OfType<ReturnStatementSyntax>() ?? [])
        {
            // A bare "return;" cannot occur in a Task<T> method; treat it as unknown just in case.
            returned.Add(statement.Expression ?? SyntaxFactory.IdentifierName("_"));
        }

        return returned;
    }

    private static bool IsTaskFromResult(ExpressionSyntax expression, SemanticModel model) =>
        expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax }
        && model.GetSymbolInfo(expression).Symbol is IMethodSymbol { Name: "FromResult" } symbol
        && symbol.ContainingType.ToDisplayString() == "System.Threading.Tasks.Task";
}
