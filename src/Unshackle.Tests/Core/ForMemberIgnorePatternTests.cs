using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class ForMemberIgnorePatternTests
{
    private const string Types = "public class A { public int Id { get; set; } } public class B { public int Id { get; set; } public int Rank { get; set; } internal int Hidden { get; set; } } ";

    // Exactly one of the two patterns claims a profile, otherwise the Fixer would rewrite it twice.
    [Theory]
    [InlineData("CreateMap<A, B>().ForMember(d => d.Rank, o => o.Ignore());", 1, 0, 0)]
    [InlineData("CreateMap<A, B>().ForMember(d => d.Rank, o => o.MapFrom(s => s.Id));", 0, 1, 0)]
    [InlineData("CreateMap<A, B>().ForMember(d => d.Rank, o => o.MapFrom(s => s.Id)).ForMember(d => d.Id, o => o.Ignore());", 0, 1, 0)]
    [InlineData("CreateMap<A, B>();", 0, 0, 1)]
    public void Find_ProfileWithOrWithoutOptions_IsClaimedByOneOfThePatterns(string statement, int byIgnoreCount, int byMapFromCount, int byCreateMapCount)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(Types + "public class P : AutoMapper.Profile { public P() { " + statement + " } }");

        // Act
        int byIgnore = new ForMemberIgnorePattern().Find(root, model).Count();
        int byMapFrom = new ForMemberMapFromPattern().Find(root, model).Count();
        int byCreateMap = new CreateMapPattern().Find(root, model).Count();

        // Assert
        byIgnore.ShouldBe(byIgnoreCount);
        byMapFrom.ShouldBe(byMapFromCount);
        byCreateMap.ShouldBe(byCreateMapCount);
    }

    [Fact]
    public void Rewrite_IgnoredMemberNotPublic_ReportsManual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            Types + "public class P : AutoMapper.Profile { public P() { CreateMap<A, B>().ForMember(d => d.Rank, o => o.Ignore()).ForMember(d => d.Hidden, o => o.Ignore()); } }");
        var pattern = new ForMemberIgnorePattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("does not have as a public property or field");
    }

    [Fact]
    public void Rewrite_IgnoredMemberHasSourceMember_LeavesItOut()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } } public class B { public int Id { get; set; } } "
            + "public class P : AutoMapper.Profile { public P() { CreateMap<A, B>().ForMember(d => d.Id, o => o.Ignore()); } }");
        var pattern = new ForMemberIgnorePattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes[^1].ToString().ShouldNotContain("source.Id");
    }

    private static (SyntaxNode Root, SemanticModel Model) Compile(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return (tree.GetRoot(), compilation.GetSemanticModel(tree));
    }
}
