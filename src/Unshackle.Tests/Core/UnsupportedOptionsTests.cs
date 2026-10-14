using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class UnsupportedOptionsTests
{
    private const string Types =
        "using AutoMapper; using System.Collections.Generic; public class A { public int Id { get; set; } public List<int> Ids { get; set; } = []; }"
        + " public class B { public int Id { get; set; } public List<int> Ids { get; set; } = []; } public class C { public int Id { get; set; } } public class D { public int Id { get; set; } }";

    // Measured against AutoMapper 14.0.0 (TAAK-257): these options change nothing on a map without nested objects or collections.
    [Theory]
    [InlineData(".MaxDepth(1)")]
    [InlineData(".MaxDepth(0)")]
    [InlineData(".PreserveReferences()")]
    [InlineData(".ForSourceMember(s => s.Id, o => o.DoNotValidate())")]
    [InlineData(".MaxDepth(3).ForSourceMember(s => s.Id, o => o.DoNotValidate()).PreserveReferences()")]
    public void Rewrite_OptionWithoutEffect_SameAsWithout(string option)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(Types + " public class P : Profile { public P() { CreateMap<C, D>()" + option + "; } }");
        (SyntaxNode plainRoot, SemanticModel plainModel) = Compile(Types + " public class P : Profile { public P() { CreateMap<C, D>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);
        PatternResult plain = pattern.Rewrite(pattern.Find(plainRoot, plainModel).ShouldHaveSingleItem(), plainModel);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToFullString())
            .ShouldBe(plain.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToFullString()));
    }

    [Theory]
    [InlineData("CreateMap<A, B>().MaxDepth(1);", "MaxDepth acts on Ids")]
    [InlineData("CreateMap<C, D>().PreserveReferences().ReverseMap();", "PreserveReferences acts on the reverse map")]
    [InlineData("CreateMap<C, D>().MaxDepth(int.Parse(\"1\"));", "MaxDepth limits how deep")]
    [InlineData("CreateMap<C, D>().ForSourceMember(\"Id\", o => o.DoNotValidate());", "ForSourceMember in a form other than ForSourceMember(s => s.X, o => o.DoNotValidate())")]
    [InlineData("CreateMap<C, D>().AfterMap((s, d) => d.Id++);", "AfterMap runs code after the members are set")]
    [InlineData("CreateMap<C, D>().IncludeBase<C, D>();", "IncludeBase inherits the options of the base map")]
    [InlineData("CreateMap<C, D>().Include<C, D>();", "Include makes this map also serve derived types")]
    [InlineData("CreateMap<C, D>().IncludeAllDerived();", "IncludeAllDerived")]
    [InlineData("CreateMap<C, D>().ForMember(d => d.Id, o => o.NullSubstitute(1));", "ForMember with NullSubstitute")]
    [InlineData("CreateMap<C, D>().ForMember(\"Id\", o => o.Ignore());", "ForMember in a form the rewrite does not read")]
    [InlineData("ShouldMapProperty = p => true; CreateMap<C, D>();", "ShouldMapProperty changes which members AutoMapper maps")]
    [InlineData("SourceMemberNamingConvention = new LowerUnderscoreNamingConvention(); CreateMap<C, D>();", "a naming convention changes which members match")]
    [InlineData("RecognizePrefixes(\"Get\"); CreateMap<C, D>();", "RecognizePrefixes changes which members match")]
    public void Rewrite_UnsupportedOption_NamesTheApi(string statements, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(Types + " public class P : Profile { public P() { " + statements + " } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).First(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax), model);

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
