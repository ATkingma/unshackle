using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class ValueResolverPatternTests
{
    private const string Types = "public class A { public int Id { get; set; } public string Name { get; set; } = \"\"; } "
        + "public class B { public int Id { get; set; } public int Code { get; set; } } "
        + "public class Plain : AutoMapper.IValueResolver<A, B, int> { public int Resolve(A s, B d, int m, AutoMapper.ResolutionContext c) => s.Id + 1; } "
        + "public class Stateful : AutoMapper.IValueResolver<A, B, int> { private int _calls; public int Resolve(A s, B d, int m, AutoMapper.ResolutionContext c) => ++_calls; } "
        + "public class Explicit : AutoMapper.IValueResolver<A, B, int> { int AutoMapper.IValueResolver<A, B, int>.Resolve(A s, B d, int m, AutoMapper.ResolutionContext c) => 1; } "
        + "public class UsesDestination : AutoMapper.IValueResolver<A, B, int> { public int Resolve(A s, B d, int m, AutoMapper.ResolutionContext c) => d.Id; } "
        + "public class UsesMember : AutoMapper.IMemberValueResolver<A, B, string, int> { public int Resolve(A s, B d, string sm, int m, AutoMapper.ResolutionContext c) => sm.Length + m; } "
        + "public class Working : AutoMapper.IValueResolver<A, B, int> { public Working() { System.Console.WriteLine(); } public int Resolve(A s, B d, int m, AutoMapper.ResolutionContext c) => 1; } ";

    [Fact]
    public void Rewrite_PlainResolver_CallsResolve()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("CreateMap<A, B>().ForMember(d => d.Code, o => o.MapFrom<Plain>());");

        // Act
        string text = Rewrite(root, model).ShouldBeOfType<FixedResult>().NewNodes[^1].ToString();

        // Assert
        text.ShouldContain("Code = new Plain().Resolve(source, default!, default!, default!),");
        text.ShouldContain("Id = source.Id,");
    }

    [Theory]
    [InlineData("o.MapFrom<Stateful>()", "keeps state in fields")]
    [InlineData("o.MapFrom<Explicit>()", "implements IValueResolver.Resolve explicitly")]
    [InlineData("o.MapFrom<UsesDestination>()", "uses d (the destination object AutoMapper is building)")]
    [InlineData("o.MapFrom<UsesMember, string>(s => s.Name)", "uses m (the member's current value)")]
    [InlineData("o.MapFrom<Working>()", "a constructor that does work")]
    [InlineData("o.MapFrom((s, d, m, ctx) => ctx.Items.Count)", "uses ctx (AutoMapper's ResolutionContext)")]
    [InlineData("o.MapFrom((s, d) => this.GetHashCode())", "uses this or base of the profile")]
    public void Rewrite_UnsafeResolver_ReportsManual(string mapFrom, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile($"CreateMap<A, B>().ForMember(d => d.Code, o => {mapFrom});");

        // Act
        PatternResult result = Rewrite(root, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
    }

    [Fact]
    public void Rewrite_ResolverWithReverseMap_ReportsManual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("CreateMap<A, B>().ForMember(d => d.Code, o => o.MapFrom<Plain>()).ReverseMap();");

        // Act
        PatternResult result = Rewrite(root, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("ReverseMap next to a value resolver");
    }

    [Fact]
    public void Find_ResolverForm_IsNotClaimedByMapFromPattern()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("CreateMap<A, B>().ForMember(d => d.Code, o => o.MapFrom((s, d) => s.Id));");

        // Act
        IEnumerable<SyntaxNode> found = new ForMemberMapFromPattern().Find(root, model);

        // Assert
        found.ShouldBeEmpty();
    }

    private static PatternResult Rewrite(SyntaxNode root, SemanticModel model)
    {
        var pattern = new ValueResolverPattern();
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
