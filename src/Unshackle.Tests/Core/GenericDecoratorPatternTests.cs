using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
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

    [Fact]
    public void IsDecorator_RewrittenVoidDecoratorInFileWithoutUsingMediatR_True()
    {
        // Arrange: an earlier pattern run wrote IRequestHandler<T, Unit>, which does not bind before "using Mediator" is added (TAAK-210).
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System.Threading;\nusing System.Threading.Tasks;\n"
            + "public interface ICommand : MediatR.IRequest { }\n"
            + "public interface ICommandHandler<in T> : MediatR.IRequestHandler<T> where T : ICommand { }\n"
            + "public class D<T> : ICommandHandler<T> where T : ICommand { public D(IRequestHandler<T, Unit> inner) { } "
            + "public Task Handle(T r, CancellationToken c) => Task.CompletedTask; }",
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        ClassDeclarationSyntax decorator = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(decorator, TestContext.Current.CancellationToken)!;

        // Act
        bool isDecorator = GenericDecoratorPattern.IsDecorator(symbol);
        List<SyntaxNode> unsupported = [.. new UnsupportedPattern().Find(tree.GetRoot(TestContext.Current.CancellationToken), model)];

        // Assert
        isDecorator.ShouldBeTrue();
        unsupported.ShouldBeEmpty();
    }

    [Fact]
    public void IsDecorator_UnboundHandlerWithOtherResponse_False()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System.Threading;\nusing System.Threading.Tasks;\n"
            + "public interface ICommand : MediatR.IRequest { }\n"
            + "public interface ICommandHandler<in T> : MediatR.IRequestHandler<T> where T : ICommand { }\n"
            + "public class D<T> : ICommandHandler<T> where T : ICommand { public D(IRequestHandler<T, int> inner) { } "
            + "public Task Handle(T r, CancellationToken c) => Task.CompletedTask; }",
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        ClassDeclarationSyntax decorator = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(decorator, TestContext.Current.CancellationToken)!;

        // Act
        bool isDecorator = GenericDecoratorPattern.IsDecorator(symbol);

        // Assert
        isDecorator.ShouldBeFalse();
    }

    // Council round 4 (Q4): only a Decorate that resolves to Scrutor (or does not resolve at all) is named as Scrutor's registration.
    [Theory]
    [InlineData("namespace Scrutor { public static class S { public static IServiceCollection Decorate(this IServiceCollection s, Type a, Type b) => s; } }", true)]
    [InlineData("namespace Own { public static class S { public static IServiceCollection Decorate(this IServiceCollection s, Type a, Type b) => s; } }", false)]
    [InlineData("", true)]
    public async Task RewriteAsync_DecorateCall_NamesScrutorOnlyWhenItResolvesThereOrNotAtAll(string extension, bool scrutor)
    {
        // Arrange
        string code = "using System;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing MediatR;\nusing Microsoft.Extensions.DependencyInjection;\n"
            + "using Scrutor;\nusing Own;\n"
            + "public class D<T> : INotificationHandler<T> where T : INotification { private readonly INotificationHandler<T> _d; "
            + "public D(INotificationHandler<T> d) { _d = d; } public Task Handle(T n, CancellationToken c) => _d.Handle(n, c); }\n"
            + "public static class R { public static IServiceCollection Add(IServiceCollection s) => s.Decorate(typeof(INotificationHandler<>), typeof(D<>)); }\n"
            + "namespace Scrutor { public class Marker { } }\nnamespace Own { public class Marker { } }\n" + extension;
        var project = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(project);
        Solution solution = new AdhocWorkspace().CurrentSolution.AddProject(project, "P", "P", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(project, TestReferences.MediatR)
            .AddDocument(documentId, "R.cs", code, filePath: "R.cs");
        Document document = solution.GetDocument(documentId)!;
        SyntaxNode root = (await document.GetSyntaxRootAsync(TestContext.Current.CancellationToken))!;
        SemanticModel model = (await document.GetSemanticModelAsync(TestContext.Current.CancellationToken))!;
        SyntaxNode node = new GenericDecoratorPattern().Find(root, model).Single();

        // Act
        PatternResult result = await new GenericDecoratorPattern().RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);

        // Assert
        string reason = result.ShouldBeOfType<ManualResult>().Reason;
        reason.Contains("Scrutor's Decorate", StringComparison.Ordinal).ShouldBe(scrutor, reason);
    }

    private static (SyntaxNode Root, SemanticModel Model) Parse(string declaration)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System.Threading;\nusing System.Threading.Tasks;\nusing MediatR;\n" + declaration, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        return (tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}