using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Fixes.Moq.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

public class MoqTargetFrameworkTests
{
    [Theory]
    [InlineData("NETFRAMEWORK,NET45,NET45_OR_GREATER", true)]
    [InlineData("NETFRAMEWORK,NET461,NET461_OR_GREATER", false)]
    [InlineData("NETCOREAPP,NETCOREAPP1_1,NETCOREAPP1_1_OR_GREATER", true)]
    [InlineData("NETCOREAPP,NETCOREAPP2_0,NETCOREAPP2_0_OR_GREATER", false)]
    [InlineData("NETCOREAPP,NET8_0,NETCOREAPP2_0_OR_GREATER,NET8_0_OR_GREATER", false)]
    [InlineData("NETSTANDARD,NETSTANDARD1_6,NETSTANDARD1_6_OR_GREATER", true)]
    [InlineData("NETSTANDARD,NETSTANDARD2_0,NETSTANDARD2_0_OR_GREATER", false)]
    [InlineData("", false)]
    public void ProjectBlockers_TargetFramework_BlocksOnlyWithoutNetStandard20(string symbols, bool blocked)
    {
        // Arrange
        Project project = Project(symbols);

        // Act
        IReadOnlyList<ManualResult> blockers = new MoqFix().ProjectBlockers(project);

        // Assert
        if (blocked)
        {
            blockers.ShouldHaveSingleItem().Code.ShouldBe(nameof(ProjectBlockingReason.TargetFrameworkUnsupported));
        }
        else
        {
            blockers.ShouldBeEmpty();
        }
    }

    [Fact]
    public void Rewrite_UnsupportedTarget_ManualForEveryNode()
    {
        // Arrange
        const string code = "using Moq;\npublic interface IClock { int Count(); }\nclass C { object T() { var m = new Mock<IClock>(); return m.Object; } }";
        var options = new CSharpParseOptions(preprocessorSymbols: ["NETFRAMEWORK", "NET45"]);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, options, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], MoqReferences.Moq).GetSemanticModel(tree);
        var pattern = new CreatePattern();

        // Act
        PatternResult[] results = [.. pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldHaveSingleItem().ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(MoqReason.TargetFrameworkUnsupported));
    }

    private static Project Project(string symbols)
    {
        Solution solution = TestSolution.Create("class C { }", TestReferences.Net);
        Project project = solution.Projects.Single();
        return project.WithParseOptions(new CSharpParseOptions(preprocessorSymbols: symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)));
    }
}
