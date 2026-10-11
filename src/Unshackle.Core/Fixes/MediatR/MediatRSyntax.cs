using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.MediatR;

/// <summary>Shared Task → ValueTask steps for the MediatR handler patterns.</summary>
public static class MediatRSyntax
{
    /// <summary>The Handle methods in <paramref name="root"/> that implement the MediatR interface with this metadata name (e.g. <c>IRequestHandler`2</c>).</summary>
    public static IEnumerable<MethodDeclarationSyntax> FindHandleMethods(SyntaxNode root, SemanticModel model, string interfaceMetadataName)
    {
        foreach (TypeDeclarationSyntax type in root.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(type) is not INamedTypeSymbol symbol)
            {
                continue;
            }

            // AllInterfaces: a handler may implement MediatR only through its own marker interface (ICommandHandler<T> : IRequestHandler<T>).
            foreach (INamedTypeSymbol handler in symbol.AllInterfaces.Where(i => IsMediatRType(i, interfaceMetadataName)))
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

    /// <summary>Task → ValueTask and Task&lt;T&gt; → ValueTask&lt;T&gt;, also namespace-qualified; anything else → null.</summary>
    public static TypeSyntax? ToValueTask(TypeSyntax type) => type switch
    {
        GenericNameSyntax { Identifier.ValueText: "Task" } generic => generic.WithIdentifier(SyntaxFactory.Identifier("ValueTask")),
        IdentifierNameSyntax { Identifier.ValueText: "Task" } name => name.WithIdentifier(SyntaxFactory.Identifier("ValueTask")),
        QualifiedNameSyntax { Right: SimpleNameSyntax right } qualified when ToValueTask(right) is SimpleNameSyntax valueTask =>
            qualified.WithRight(valueTask),
        _ => null,
    };

    /// <summary>The expression body, or every return in the body except those of nested lambdas and local functions; a bare <c>return;</c> gives null.</summary>
    public static List<ExpressionSyntax?> ReturnedExpressions(MethodDeclarationSyntax method) =>
        method.ExpressionBody is not null
            ? [method.ExpressionBody.Expression]
            : [.. method.Body?.DescendantNodes(n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                .OfType<ReturnStatementSyntax>()
                .Select(r => r.Expression) ?? []];

    /// <summary>True for a member of <c>System.Threading.Tasks.Task</c> with this name, e.g. <c>Task.CompletedTask</c> or <c>Task.FromResult</c>.</summary>
    public static bool IsTaskMember(ExpressionSyntax? expression, SemanticModel model, string name) =>
        expression is not null
        && model.GetSymbolInfo(expression).Symbol is { } symbol
        && symbol.Name == name
        && symbol.ContainingType?.ToDisplayString() == "System.Threading.Tasks.Task";

    /// <summary><c>Task.X</c> → <c>ValueTask.X</c>, keeping trivia.</summary>
    public static MemberAccessExpressionSyntax OnValueTask(MemberAccessExpressionSyntax taskMember) =>
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("ValueTask"),
            taskMember.Name).WithTriviaFrom(taskMember);

    private static bool IsMediatRType(INamedTypeSymbol type, string metadataName) =>
        type.OriginalDefinition.MetadataName == metadataName
        && type.ContainingNamespace.ToDisplayString() == "MediatR";
}
