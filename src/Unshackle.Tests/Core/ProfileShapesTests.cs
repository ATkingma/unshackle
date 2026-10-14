using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// Profile shapes (TAAK-248) where a one-file proof cannot show it: Map calls in another file, a profile rewritten in an earlier run,
// line endings, what Find reports.
public class ProfileShapesTests
{
    private const string Dto = """
        using AutoMapper;

        namespace Shop.Mapping;

        public class TodoItem
        {
            public int Id { get; set; }
        }

        public class TodoItemDto
        {
            public int Id { get; set; }

            private class Mapping : Profile
            {
                public Mapping() => CreateMap<TodoItem, TodoItemDto>();
            }
        }
        """;

    private const string Service = """
        using AutoMapper;

        namespace Shop.Todos;

        public class TodoService
        {
            public Shop.Mapping.TodoItemDto Get(IMapper mapper, Shop.Mapping.TodoItem item) => mapper.Map<Shop.Mapping.TodoItemDto>(item);

            public Shop.Mapping.TodoItemDto Update(IMapper mapper, Shop.Mapping.TodoItem item, Shop.Mapping.TodoItemDto dto) => mapper.Map(item, dto);
        }
        """;

    [Fact]
    public void Find_NestedProfile_ReportsTheOutermostType()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Dto, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = Compile(tree).GetSemanticModel(tree);

        // Act
        SyntaxNode found = new CreateMapPattern().Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem();

        // Assert
        found.ShouldBeOfType<ClassDeclarationSyntax>().Identifier.ValueText.ShouldBe("TodoItemDto");
    }

    [Fact]
    public async Task FixAsync_NestedProfileInOtherFile_MapCallsFindItsMethods()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(project, TestReferences.AutoMapper)
            .AddDocument(DocumentId.CreateNewId(project), "Dto.cs", Dto, filePath: "Dto.cs")
            .AddDocument(DocumentId.CreateNewId(project), "Service.cs", Service, filePath: "Service.cs");

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        Document[] documents = [.. report.Solution.Projects.Single().Documents];
        string dto = (await documents.Single(d => d.Name == "Dto.cs").GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        string service = (await documents.Single(d => d.Name == "Service.cs").GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        dto.ShouldContain("public static class TodoItemDtoMappingExtensions");
        dto.ShouldContain("public static TodoItemDto MapInto(this TodoItem source, TodoItemDto destination)");
        service.ShouldContain("using Shop.Mapping;");
        service.ShouldContain("=> item.ToTodoItemDto();");
        service.ShouldContain("=> item.MapInto(dto);");
        var after = CSharpCompilation.Create("After", [CSharpSyntaxTree.ParseText(dto, cancellationToken: TestContext.Current.CancellationToken),
            CSharpSyntaxTree.ParseText(service, cancellationToken: TestContext.Current.CancellationToken)], TestReferences.AutoMapper,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        after.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    // A nested profile emptied by an earlier run leaves <Outer><Profile>Extensions; a member of another profile still calls its method.
    [Fact]
    public void Rewrite_NestedMapOfNestedProfileRewrittenEarlier_CallsItsMethod()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText("""
            using AutoMapper;

            namespace Shop;

            public class Address
            {
                public string City { get; set; } = "";
            }

            public class AddressDto
            {
                public string City { get; set; } = "";

                private class Mapping : Profile
                {
                    public Mapping()
                    {
                    }
                }
            }

            public static class AddressDtoMappingExtensions
            {
                public static AddressDto ToAddressDto(this Address source) => new()
                {
                    City = source.City,
                };
            }

            public class Order
            {
                public Address Address { get; set; } = new();
            }

            public class OrderDto
            {
                public AddressDto Address { get; set; } = new();

                private class Mapping : Profile
                {
                    public Mapping() => CreateMap<Order, OrderDto>();
                }
            }
            """, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = Compile(tree).GetSemanticModel(tree);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem(), model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.NewNodes[1].ToFullString().ShouldContain("public static class OrderDtoMappingExtensions");
        fixedResult.NewNodes[1].ToFullString().ShouldContain("Address = source.Address?.ToAddressDto(),");
    }

    [Fact]
    public void Rewrite_ExpressionBodiedConstructorWithCrLf_KeepsTheLineEndings()
    {
        // Arrange
        string code = "namespace Shop;\r\n\r\npublic class A\r\n{\r\n    public int Id { get; set; }\r\n}\r\n\r\npublic class B\r\n{\r\n    public int Id { get; set; }\r\n}\r\n\r\n"
            + "public class P : AutoMapper.Profile\r\n{\r\n    public P() => CreateMap<A, B>();\r\n}\r\n";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = Compile(tree).GetSemanticModel(tree);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes[0].ToString().ShouldBe("public class P : AutoMapper.Profile\r\n{\r\n    public P()\r\n    {\r\n    }\r\n}");
    }

    private static CSharpCompilation Compile(SyntaxTree tree)
    {
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return compilation;
    }
}
