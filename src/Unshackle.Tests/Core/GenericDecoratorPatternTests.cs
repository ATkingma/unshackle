using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class GenericDecoratorPatternTests
{
    [Fact]
    public void Find_DecoratorWithResponse_LeavesItToTheHandlerPatterns()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(
            "public class D<T, R> : IRequestHandler<T, R> where T : IRequest<R> { public D(IRequestHandler<T, R> inner) { } "
            + "public Task<R> Handle(T r, CancellationToken c) => throw null!; }");

        // Act
        List<SyntaxNode> found = [.. new GenericDecoratorPattern().Find(root, model)];

        // Assert
        found.ShouldBeEmpty();
    }

    [Fact]
    public void IsDecorator_OpenGenericHandlerWithoutDecoratedParameter_False()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(
            "public class H<T> : IRequestHandler<T> where T : IRequest { public Task Handle(T r, CancellationToken c) => Task.CompletedTask; }");
        var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(), TestContext.Current.CancellationToken)!;

        // Act
        bool decorator = GenericDecoratorPattern.IsDecorator(symbol);

        // Assert
        decorator.ShouldBeFalse();
    }

    private static (SyntaxNode Root, SemanticModel Model) Parse(string declaration)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System.Threading;\nusing System.Threading.Tasks;\nusing MediatR;\n" + declaration, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        return (tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}