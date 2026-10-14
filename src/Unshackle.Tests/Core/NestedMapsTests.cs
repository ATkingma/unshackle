using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// Nested maps (TAAK-245) where a one-file proof cannot show it: other projects, other files, an older language version, MapInto.
public class NestedMapsTests
{
    private const string Contracts = """
        // @project Contracts
        using AutoMapper;

        namespace Contracts;

        ACCESS class Address
        {
            public string City { get; set; } = "";
        }

        ACCESS class AddressDto
        {
            public string City { get; set; } = "";
        }

        public class AddressProfile : Profile
        {
            public AddressProfile()
            {
                CreateMap<Address, AddressDto>();
            }
        }

        """;

    private const string Shop = """
        // @project Shop -> Contracts
        using AutoMapper;
        using Contracts;

        namespace Shop;

        public class Order
        {
            public Address Address { get; set; } = new();
        }

        public class OrderDto
        {
            public AddressDto Address { get; set; } = new();
        }

        public class OrderProfile : Profile
        {
            public OrderProfile()
            {
                CreateMap<Order, OrderDto>();
            }
        }

        public class OrderService
        {
            public OrderDto Get(IMapper mapper, Order order) => mapper.Map<OrderDto>(order);
        }

        """;

    [Fact]
    public async Task FixAsync_NestedMapInProjectDependedOn_CallsItsMethod()
    {
        // Arrange
        string before = Contracts.Replace("ACCESS", "public", StringComparison.Ordinal) + Shop;
        Solution solution = TestProjects.Create(before, TestReferences.AutoMapper);

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        string after = await TestProjects.GetTextAsync(report.Solution, before, TestContext.Current.CancellationToken);
        after.ShouldContain("Address = source.Address?.ToAddressDto(),");
        after.ShouldContain("public OrderDto Get(IMapper mapper, Order order) => order.ToOrderDto();");
        TestProjects.CompileErrors(after, TestReferences.AutoMapper).ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_NestedMapInternalInOtherProject_ReportsManual()
    {
        // Arrange
        string before = Contracts.Replace("ACCESS", "internal", StringComparison.Ordinal)
            .Replace("namespace Contracts;", "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Shop\")]\n\nnamespace Contracts;", StringComparison.Ordinal)
            + Shop.Replace("public class Order", "internal class Order", StringComparison.Ordinal)
                .Replace("public OrderDto Get", "internal OrderDto Get", StringComparison.Ordinal);
        Solution solution = TestProjects.Create(before, TestReferences.AutoMapper);

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("Address: the mapping method ToAddressDto becomes internal in another project"));
    }

    [Fact]
    public void Rewrite_NestedMapInOtherFileOfTheProject_IsFoundWithoutTheSolution()
    {
        // Arrange
        SyntaxTree addresses = CSharpSyntaxTree.ParseText(Contracts[(Contracts.IndexOf('\n') + 1)..].Replace("ACCESS", "public", StringComparison.Ordinal), cancellationToken: TestContext.Current.CancellationToken);
        SyntaxTree orders = CSharpSyntaxTree.ParseText(Shop[(Shop.IndexOf('\n') + 1)..], cancellationToken: TestContext.Current.CancellationToken);
        CSharpCompilation compilation = Compile(addresses, orders);
        SemanticModel model = compilation.GetSemanticModel(orders);
        var pattern = new CreateMapPattern();
        SyntaxNode profile = pattern.Find(orders.GetRoot(TestContext.Current.CancellationToken), model).OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().ShouldHaveSingleItem();

        // Act
        PatternResult result = pattern.Rewrite(profile, model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString().ShouldContain("Address = source.Address?.ToAddressDto(),");
    }

    [Fact]
    public void Rewrite_BeforeCSharp12_FallsBackToNewEmptyCollections()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText("""
            using System.Collections.Generic;
            using AutoMapper;

            public class Line { public int Quantity { get; set; } }
            public class LineDto { public int Quantity { get; set; } }
            public class Order { public List<Line> Lines { get; set; } public Line[] Archived { get; set; } }
            public class OrderDto { public List<LineDto> Lines { get; set; } public LineDto[] Archived { get; set; } }

            public class OrderProfile : Profile
            {
                public OrderProfile()
                {
                    CreateMap<Order, OrderDto>();
                    CreateMap<Line, LineDto>();
                }
            }
            """, new CSharpParseOptions(LanguageVersion.CSharp11), cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = Compile(tree).GetSemanticModel(tree);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem(), model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        string extensions = fixedResult.NewNodes[1].ToFullString();
        extensions.ShouldContain("Lines = source.Lines?.Select(x => x?.ToLineDto()).ToList() ?? new List<LineDto>(),");
        extensions.ShouldContain("Archived = source.Archived?.Select(x => x?.ToLineDto()).ToArray() ?? global::System.Array.Empty<LineDto>(),");
        fixedResult.RequiredNamespaces.ShouldBe(["System.Collections.Generic", "System.Linq"]);
    }

    // AllowNullCollections = true maps a null list to null instead of an empty one: the copy written for the defaults would differ.
    [Fact]
    public void Rewrite_AllowNullCollectionsConfigured_ReportsManual()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText("""
            using System.Collections.Generic;
            using AutoMapper;

            public class Order { public List<int> Numbers { get; set; } = []; }
            public class OrderDto { public List<int> Numbers { get; set; } = []; }

            public class OrderProfile : Profile
            {
                public OrderProfile()
                {
                    CreateMap<Order, OrderDto>();
                }
            }

            public static class Mapping
            {
                public static MapperConfiguration Create() => new(cfg =>
                {
                    cfg.AllowNullCollections = true;
                    cfg.AddProfile<OrderProfile>();
                });
            }
            """, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = Compile(tree).GetSemanticModel(tree);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("the null handling that AllowNullCollections at");
    }

    // AutoMapper maps into the nested object the destination already holds; a MapInto would replace it.
    [Fact]
    public async Task FixAsync_MapIntoWithNestedMember_ReportsManual()
    {
        // Arrange
        string code = """
            using AutoMapper;

            namespace Shop;

            public class Address { public string City { get; set; } = ""; }
            public class AddressDto { public string City { get; set; } = ""; }
            public class Order { public Address Address { get; set; } = new(); }
            public class OrderDto { public AddressDto Address { get; set; } = new(); }

            public class OrderProfile : Profile
            {
                public OrderProfile()
                {
                    CreateMap<Order, OrderDto>();
                    CreateMap<Address, AddressDto>();
                }
            }

            public class OrderService
            {
                public void Update(IMapper mapper, Order order, OrderDto dto) => mapper.Map(order, dto);
            }
            """;
        Solution solution = TestSolution.Create(code, TestReferences.AutoMapper);

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("MapInto would replace it"));
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldBe(code);
    }

    private static CSharpCompilation Compile(params SyntaxTree[] trees)
    {
        var compilation = CSharpCompilation.Create("Test", trees, TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return compilation;
    }
}
