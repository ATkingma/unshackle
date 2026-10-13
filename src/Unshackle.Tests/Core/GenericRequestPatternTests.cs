using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class GenericRequestPatternTests
{
    private const string Request =
        "using MediatR;\nnamespace Library;\npublic class Ping : IRequest<int> { }\n"
        + "public class Retried<T, R> : IRequest<R> where T : IRequest<R> { public Retried(T request) { Request = request; } public T Request { get; } }";

    [Fact]
    public async Task RewriteAsync_ClosedTypeOnlyUsedInOtherProject_WritesItsSubclass()
    {
        // Arrange
        (Solution solution, DocumentId declaration) = CreateSolution(
            "using MediatR;\nusing Library;\nclass Use { void M(ISender s) { s.Send(new Retried<Ping, int>(new Ping())); } }");
        (GenericRequestPattern pattern, SyntaxNode node, SemanticModel model) = await ArrangeAsync(solution, declaration);

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.NewNodes.Select(n => n.ToString()).ShouldContain(t => t.StartsWith("public class RetriedOfPingAndInt32 : Retried<Ping, int>", StringComparison.Ordinal));
        fixedResult.Warning.ShouldNotBeNull();
    }

    [Fact]
    public async Task RewriteAsync_ClosedTypeDeclaredInOtherProject_Manual()
    {
        // Arrange
        (Solution solution, DocumentId declaration) = CreateSolution(
            "using MediatR;\nusing Library;\npublic class Pong : IRequest<int> { }\nclass Use { void M(ISender s) { s.Send(new Retried<Pong, int>(new Pong())); } }");
        (GenericRequestPattern pattern, SyntaxNode node, SemanticModel model) = await ArrangeAsync(solution, declaration);

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("Pong is not a plain type visible from Library");
    }

    [Fact]
    public void Find_OnlyBaseOfConcreteSubclass_FindsTheDeclaration()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using MediatR;\npublic class Note<T> : INotification { }\npublic class PlacedNote : Note<int> { }", cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);

        // Act
        List<SyntaxNode> found = [.. new GenericRequestPattern().Find(tree.GetRoot(TestContext.Current.CancellationToken), model)];

        // Assert
        found.ShouldHaveSingleItem().ToString().ShouldStartWith("public class Note<T>");
    }

    private static (Solution Solution, DocumentId Declaration) CreateSolution(string use)
    {
        var library = ProjectId.CreateNewId();
        var app = ProjectId.CreateNewId();
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);
        var declaration = DocumentId.CreateNewId(library);
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(library, "Library", "Library", LanguageNames.CSharp).WithProjectCompilationOptions(library, options)
            .AddMetadataReferences(library, TestReferences.MediatR)
            .AddDocument(declaration, "Retried.cs", Request)
            .AddProject(app, "App", "App", LanguageNames.CSharp).WithProjectCompilationOptions(app, options)
            .AddMetadataReferences(app, TestReferences.MediatR)
            .AddProjectReference(app, new ProjectReference(library))
            .AddDocument(DocumentId.CreateNewId(app), "Use.cs", use);
        return (solution, declaration);
    }

    private static async Task<(GenericRequestPattern Pattern, SyntaxNode Node, SemanticModel Model)> ArrangeAsync(Solution solution, DocumentId declaration)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        Document document = solution.GetDocument(declaration)!;
        SemanticModel model = (await document.GetSemanticModelAsync(token))!;
        var pattern = new GenericRequestPattern();
        return (pattern, pattern.Find((await document.GetSyntaxRootAsync(token))!, model).Single(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax { Identifier.ValueText: "Retried" }), model);
    }
}