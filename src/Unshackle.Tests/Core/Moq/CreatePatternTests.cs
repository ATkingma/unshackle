using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.Moq.Patterns;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

public class CreatePatternTests
{
    private const string Clock = "public interface IClock { System.DateTime Now { get; } }\n";

    [Fact]
    public void Rewrite_LocalUsedOnlyAsObject_FixesDeclarationAndObject()
    {
        // Arrange
        const string code = "using Moq;\n" + Clock + "class C { object T() { var m = new Mock<IClock>(); return m.Object; } }";
        (CreatePattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        string[] results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model)).Select(Text)];

        // Assert
        results.ShouldBe(["var m = Substitute.For<IClock>()", "m"]);
    }

    [Fact]
    public void Rewrite_MockOfWithPredicate_Manual()
    {
        // Arrange
        const string code = "using Moq;\n" + Clock + "class C { object T() => Mock.Of<IClock>(c => c.Now == default); }";
        (CreatePattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldStartWith("MockOfSetup");
    }

    [Fact]
    public void Rewrite_MockGet_Manual()
    {
        // Arrange
        const string code = "using Moq;\n" + Clock + "class C { object T(IClock c) => Mock.Get(c); }";
        (CreatePattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldStartWith("MockGet");
    }

    [Fact]
    public void Rewrite_MockParameter_ManualAndObjectLeftAlone()
    {
        // Arrange
        const string code = "using Moq;\n" + Clock + "class C { object T(Mock<IClock> m) => m.Object; }";
        (CreatePattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        PatternResult[] results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldHaveSingleItem().ShouldBeOfType<ManualResult>().Reason.ShouldStartWith("MockPassedAround");
    }

    [Fact]
    public void Rewrite_InterfaceReturningInterface_ManualForNewMockButFixedForMockOf()
    {
        // Arrange: new Mock<T>() answers null for an interface, NSubstitute and Mock.Of a recursive mock.
        const string code = "using Moq;\n" + Clock + "public interface IFactory { IClock Create(); }\n"
            + "class C { object A() => new Mock<IFactory>().Object; object B() => Mock.Of<IFactory>(); }";
        (CreatePattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        PatternResult[] results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results[0].ShouldBeOfType<ManualResult>().Reason.ShouldStartWith("DefaultValueDiffers");
        results[1].ShouldBeOfType<FixedResult>();
    }

    [Fact]
    public void Rewrite_MockOfInterfaceWithClassReturn_Manual()
    {
        // Arrange: Mock.Of mocks a non-sealed class, NSubstitute returns null.
        const string code = "using Moq;\npublic class Dto { }\npublic interface IRepo { Dto Get(); }\nclass C { object T() => Mock.Of<IRepo>(); }";
        (CreatePattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("Dto");
    }

    [Fact]
    public void Rewrite_TwoMocksInOneDeclaration_ManualAndObjectsLeftAlone()
    {
        // Arrange
        const string code = "using Moq;\n" + Clock + "class C { Mock<IClock> _a = new(), _b = new(); object T() => _a.Object; }";
        (CreatePattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        PatternResult[] results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldHaveSingleItem().ShouldBeOfType<ManualResult>().Reason.ShouldStartWith("MockDeclaration");
    }

    [Fact]
    public void Find_OwnMockClass_FindsNothing()
    {
        // Arrange
        const string code = "class Mock<T> { public T Object => default!; }\nclass C { object T() => new Mock<string>().Object; }";
        (CreatePattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        IEnumerable<SyntaxNode> found = pattern.Find(root, model);

        // Assert
        found.ShouldBeEmpty();
    }

    private static string Text(PatternResult result) => result switch
    {
        FixedResult f => f.NewNodes.Single().ToString(),
        ManualResult m => m.Reason,
        _ => throw new InvalidOperationException(),
    };

    private static (CreatePattern Pattern, SemanticModel Model, SyntaxNode Root) Arrange(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], MoqReferences.Moq).GetSemanticModel(tree);
        return (new CreatePattern(), model, tree.GetRoot(TestContext.Current.CancellationToken));
    }
}
