using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class CreateMapPatternTests
{
    // An emptied profile from an earlier run is not found again: a second run gives no Manual that would undo the first (AllOrNothing).
    [Fact]
    public void Find_EmptiedProfile_FindsNothing()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("public class P : AutoMapper.Profile { public P() { } }");

        // Act + Assert
        new CreateMapPattern().Find(root, model).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("public class A { public int id { get; set; } } public class B { public int Id { get; set; } }", "has no member with the same name")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; private set; } }", "has no public setter")]
    [InlineData("public class A { public int Id { get; set; } } public abstract class B { public int Id { get; set; } }", "no public parameterless constructor")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int[] Id { get; set; } = []; }", "(AutoMapper converts)")]
    [InlineData("public class A { public int Id { get; set; } } public class B { private B() { } public B(int id) { } }", "has several constructors or none that is public")]
    [InlineData("public class A { public int Id { get; set; } public int ID; } public class B { public B(int id) { } }", "matches several members of A")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public B(ref int id) { } }", "has a ref, out or params parameter id")]
    public void Rewrite_MapAutoMapperDoesMoreFor_ReportsManual(string types, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(types + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
    }

    // AutoMapper 14 reads the member list only in AssertConfigurationIsValid (TAAK-246): every constant form gives the map without it.
    [Theory]
    [InlineData("MemberList.Destination")]
    [InlineData("MemberList.Source")]
    [InlineData("memberList: MemberList.None")]
    [InlineData("AutoMapper.MemberList.None")]
    [InlineData("(MemberList)1")]
    public void Rewrite_ConstantMemberList_SameAsWithout(string argument)
    {
        // Arrange
        const string Types = "using AutoMapper; public class A { public int Id { get; set; } public string Extra { get; set; } = \"\"; } public class B { public int Id { get; set; } }";
        (SyntaxNode root, SemanticModel model) = Compile(Types + " public class P : Profile { public P() { CreateMap<A, B>(" + argument + "); } }");
        (SyntaxNode plainRoot, SemanticModel plainModel) = Compile(Types + " public class P : Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);
        PatternResult plain = pattern.Rewrite(pattern.Find(plainRoot, plainModel).ShouldHaveSingleItem(), plainModel);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToFullString())
            .ShouldBe(plain.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToFullString()));
    }

    // AutoMapper 14 fills a constructor parameter from the source member of the same name in any case (TAAK-251).
    [Fact]
    public void Rewrite_ConstructorParameterOtherCase_PassesMember()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } public string Name { get; set; } = \"\"; } public class B { public B(int id) { Id = id; } public int Id { get; set; } public string Name { get; set; } = \"\"; } public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string text = result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString();
        text.ShouldContain("public static B ToB(this A source) => new(source.Id)");
        text.ShouldContain("Name = source.Name,");
        text.ShouldNotContain("Id = source.Id");
    }

    // Dropping an expression that is not a constant could drop a side effect.
    [Fact]
    public void Rewrite_MemberListParameter_ReportsManual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } } public class B { public int Id { get; set; } } public class P : AutoMapper.Profile { public P(AutoMapper.MemberList list) { CreateMap<A, B>(list); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("is not a CreateMap<A, B>() without options");
    }

    private static (SyntaxNode Root, SemanticModel Model) Compile(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return (tree.GetRoot(), compilation.GetSemanticModel(tree));
    }
}
