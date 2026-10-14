using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class ForMemberMapFromPatternTests
{
    private const string Types = "public class C { public string Name { get; set; } = \"\"; } "
        + "public class A { public int Id { get; set; } public C? Customer { get; set; } public string Text { get; set; } = \"\"; } "
        + "public class B { public int Id { get; set; } public long Wide { get; set; } public string Label { get; set; } = \"\"; public string Other { get; set; } = \"\"; public int ReadOnly { get; } } ";

    [Fact]
    public void Rewrite_MapFromWithRenamedParameter_UsesSourceInTheExpression()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("CreateMap<A, B>().ForMember(d => d.Wide, o => o.MapFrom(x => x.Id + 1)).ForMember(d => d.Other, o => o.Ignore()).ForMember(d => d.Label, o => o.Ignore());");

        // Act
        string text = Rewrite(root, model).ShouldBeOfType<FixedResult>().NewNodes[^1].ToString();

        // Assert
        text.ShouldContain("Wide = source.Id + 1,");
        text.ShouldNotContain("x.Id");
        text.ShouldContain("Id = source.Id,");
    }

    [Fact]
    public void Rewrite_MapFromOnReadOnlyMember_ReportsManual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("CreateMap<A, B>().ForMember(d => d.ReadOnly, o => o.MapFrom(s => s.Id));");

        // Act
        PatternResult result = Rewrite(root, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("has no public setter");
    }

    [Fact]
    public void Rewrite_MapFromConfiguredTwice_ReportsManual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "CreateMap<A, B>().ForMember(d => d.Wide, o => o.MapFrom(s => s.Id)).ForMember(d => d.Wide, o => o.Ignore());");

        // Act
        PatternResult result = Rewrite(root, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("is not a CreateMap<A, B>() without options");
    }

    [Fact]
    public void Rewrite_ReceiverCheckedForNull_IsMoved()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "CreateMap<A, B>().ForMember(d => d.Label, o => o.MapFrom(s => s.Customer != null ? s.Customer.Name : \"\")).ForMember(d => d.Wide, o => o.Ignore()).ForMember(d => d.Other, o => o.Ignore());");

        // Act
        string text = Rewrite(root, model).ShouldBeOfType<FixedResult>().NewNodes[^1].ToString();

        // Assert
        text.ShouldContain("Label = source.Customer != null ? source.Customer.Name : \"\",");
    }

    [Fact]
    public void Rewrite_NullableAnnotatedReceiver_UsesNullConditional()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "CreateMap<A, B>().ForMember(d => d.Label, o => o.MapFrom(s => s.Customer.Name)).ForMember(d => d.Wide, o => o.Ignore()).ForMember(d => d.Other, o => o.Ignore());");

        // Act
        string text = Rewrite(root, model).ShouldBeOfType<FixedResult>().NewNodes[^1].ToString();

        // Assert
        text.ShouldContain("Label = source.Customer?.Name!,");
    }

    private static PatternResult Rewrite(SyntaxNode root, SemanticModel model)
    {
        var pattern = new ForMemberMapFromPattern();
        return pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);
    }

    private static (SyntaxNode Root, SemanticModel Model) Compile(string statement)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("#nullable enable\n" + Types + "public class P : AutoMapper.Profile { public P() { " + statement + " } }");
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return (tree.GetRoot(), compilation.GetSemanticModel(tree));
    }
}
