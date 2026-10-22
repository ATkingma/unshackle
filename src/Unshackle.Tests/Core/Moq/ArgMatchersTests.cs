using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core.Fixes.Moq;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

public class ArgMatchersTests
{
    private const string Service = "public interface IService { string Find(int id, string name); int Count(); string Name { get; } void Save(ref int x); }\n";

    [Theory]
    [InlineData("x => x.Find(It.IsAny<int>(), It.IsAny<string>())", "m.Object.Find(Arg.Any<int>(), Arg.Any<string>())")]
    [InlineData("x => x.Find(It.Is<int>(i => i > 2), \"a\")", "m.Object.Find(Arg.Is<int>(i => i > 2), Arg.Is<string>(\"a\"))")]
    [InlineData("x => x.Find(1, \"a\")", "m.Object.Find(1, \"a\")")]
    [InlineData("x => x.Find(1, It.IsNotNull<string>())", "m.Object.Find(Arg.Is<int>(1), Arg.Is<string>(a => a != null))")]
    [InlineData("x => x.Count()", "m.Object.Count()")]
    [InlineData("x => x.Name", "m.Object.Name")]
    public void Rewrite_SupportedShape_Fixed(string lambda, string expected)
    {
        // Arrange
        (ExpressionSyntax node, SemanticModel model) = Arrange(lambda);

        // Act
        var call = MockCall.Rewrite(node, SyntaxFactory.ParseExpression("m.Object"), model);

        // Assert
        call.Blocker.ShouldBeNull();
        call.Node!.NormalizeWhitespace().ToString().ShouldBe(SyntaxFactory.ParseExpression(expected).NormalizeWhitespace().ToString());
    }

    [Theory]
    [InlineData("x => x.Find(It.IsInRange(1, 3, Moq.Range.Inclusive), \"a\")", "MatcherUnsupported")]
    [InlineData("x => x.Find(It.IsAny<int>() + 1, \"a\")", "MatcherNested")]
    [InlineData("x => x.Find(x.Count(), \"a\")", "ArgumentUsesMock")]
    [InlineData("x => x.Find(id: 1, name: \"a\")", "NamedArgument")]
    [InlineData("x => x.Save(ref It.Ref<int>.IsAny)", "RefOutOrParams")]
    [InlineData("x => x.Name.Length", "CallShape")]
    public void Rewrite_UnsupportedShape_Blocked(string lambda, string code)
    {
        // Arrange
        (ExpressionSyntax node, SemanticModel model) = Arrange(lambda);

        // Act
        var call = MockCall.Rewrite(node, SyntaxFactory.ParseExpression("m.Object"), model);

        // Assert
        call.Blocker.ShouldNotBeNull().Code.ToString().ShouldBe(code);
    }

    private static (ExpressionSyntax Lambda, SemanticModel Model) Arrange(string lambda)
    {
        string code = "using Moq;\n" + Service + "class C { void T(Mock<IService> m) { m.Setup(" + lambda + "); } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], MoqReferences.Moq).GetSemanticModel(tree);
        InvocationExpressionSyntax setup = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Setup" });
        return (setup.ArgumentList.Arguments[0].Expression, model);
    }
}
