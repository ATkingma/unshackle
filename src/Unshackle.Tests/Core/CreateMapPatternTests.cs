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

    private static (SyntaxNode Root, SemanticModel Model) Compile(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return (tree.GetRoot(), compilation.GetSemanticModel(tree));
    }
}
