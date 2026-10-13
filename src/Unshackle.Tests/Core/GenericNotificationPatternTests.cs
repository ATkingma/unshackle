using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class GenericNotificationPatternTests
{
    private const string Notification =
        "using MediatR;\nnamespace Library;\npublic abstract class Event { }\npublic class Placed : Event { }\n"
        + "public class Note<T> : INotification where T : Event { public Note(T e) { E = e; } public T E { get; } }";

    [Fact]
    public async Task RewriteAsync_ClosedTypeOnlyUsedInOtherProject_WritesItsSubclass()
    {
        // Arrange
        (Solution solution, DocumentId declaration) = CreateSolution(
            "using MediatR;\nusing Library;\nclass Use { void M(IPublisher p) { p.Publish(new Note<Placed>(new Placed())); } }");
        (GenericNotificationPattern pattern, SyntaxNode node, SemanticModel model) = await ArrangeAsync(solution, declaration);

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.NewNodes.Select(n => n.ToString()).ShouldContain(t => t.StartsWith("public class NoteOfPlaced : Note<Placed>", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RewriteAsync_ClosedTypeDeclaredInOtherProject_Manual()
    {
        // Arrange
        (Solution solution, DocumentId declaration) = CreateSolution(
            "using MediatR;\nusing Library;\npublic class Shipped : Event { }\nclass Use { void M(IPublisher p) { p.Publish(new Note<Shipped>(new Shipped())); } }");
        (GenericNotificationPattern pattern, SyntaxNode node, SemanticModel model) = await ArrangeAsync(solution, declaration);

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("Shipped is not visible from Library");
    }

    [Fact]
    public async Task RewriteAsync_MakeGenericTypeInOtherProject_ClosesEveryConstrainedType()
    {
        // Arrange
        (Solution solution, DocumentId declaration) = CreateSolution(
            "using System;\nusing Library;\nclass Use { Type M(Event e) => typeof(Note<>).MakeGenericType(e.GetType()); }");
        (GenericNotificationPattern pattern, SyntaxNode node, SemanticModel model) = await ArrangeAsync(solution, declaration);

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);

        // Assert
        List<string> texts = [.. result.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToString())];
        texts.ShouldContain(t => t.StartsWith("public class NoteOfPlaced : Note<Placed>", StringComparison.Ordinal));
        texts.ShouldContain(t => t.StartsWith("public static class NoteTypes", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RewriteAsync_ExistingSubclass_Manual()
    {
        // Arrange
        (Solution solution, DocumentId declaration) = CreateSolution(
            "using Library;\npublic class PlacedNote : Note<Placed> { public PlacedNote(Placed e) : base(e) { } }");
        (GenericNotificationPattern pattern, SyntaxNode node, SemanticModel model) = await ArrangeAsync(solution, declaration);

        // Act
        PatternResult result = await pattern.RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("already has a subclass (PlacedNote)");
    }

    [Fact]
    public void Find_SubclassedInSameProject_LeavesItToTheSubclassPattern()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            Notification + "\npublic class PlacedNote : Note<Placed> { public PlacedNote(Placed e) : base(e) { } }", cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);

        // Act
        List<SyntaxNode> found = [.. new GenericNotificationPattern().Find(tree.GetRoot(TestContext.Current.CancellationToken), model)];

        // Assert
        found.ShouldBeEmpty();
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
            .AddDocument(declaration, "Note.cs", Notification)
            .AddProject(app, "App", "App", LanguageNames.CSharp).WithProjectCompilationOptions(app, options)
            .AddMetadataReferences(app, TestReferences.MediatR)
            .AddProjectReference(app, new ProjectReference(library))
            .AddDocument(DocumentId.CreateNewId(app), "Use.cs", use);
        return (solution, declaration);
    }

    private static async Task<(GenericNotificationPattern Pattern, SyntaxNode Node, SemanticModel Model)> ArrangeAsync(Solution solution, DocumentId declaration)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        Document document = solution.GetDocument(declaration)!;
        SemanticModel model = (await document.GetSemanticModelAsync(token))!;
        var pattern = new GenericNotificationPattern();
        return (pattern, pattern.Find((await document.GetSyntaxRootAsync(token))!, model).Single(), model);
    }
}
