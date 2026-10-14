using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// The profile and the ProjectTo call in different files: the profile rewrite writes the projection because the call needs it.
public class ProjectToPatternTests
{
    private const string Profile = """
        using AutoMapper;

        namespace Shop.Mapping;

        public class Address
        {
            public string City { get; set; } = "";
        }

        public class AddressDto
        {
            public string City { get; set; } = "";
        }

        public class Order
        {
            public int Id { get; set; }
            public Address Address { get; set; } = new();
        }

        public class OrderDto
        {
            public int Id { get; set; }
            public AddressDto Address { get; set; } = new();
        }

        public class OrderProfile : Profile
        {
            public OrderProfile()
            {
                CreateMap<Order, OrderDto>();
                CreateMap<Address, AddressDto>();
            }
        }
        """;

    private const string Queries = """
        using AutoMapper;
        using AutoMapper.QueryableExtensions;

        namespace Shop.Orders;

        public class OrderQueries
        {
            public System.Linq.IQueryable<Shop.Mapping.OrderDto> All(IMapper mapper, System.Linq.IQueryable<Shop.Mapping.Order> orders) =>
                orders.ProjectTo<Shop.Mapping.OrderDto>(mapper.ConfigurationProvider);
        }
        """;

    [Fact]
    public async Task FixAsync_ProfileInOtherFile_WritesProjectionAndSelectsIt()
    {
        // Arrange
        Solution solution = Create(("Profile.cs", Profile), ("Queries.cs", Queries));

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        string profile = await TextAsync(report.Solution, "Profile.cs");
        profile.ShouldContain("public static Expression<Func<Order, OrderDto>> OrderToOrderDtoProjection => source => new OrderDto");
        profile.ShouldContain("Address = source.Address == null ? null : new AddressDto { City = source.Address.City },");
        profile.ShouldNotContain("AddressToAddressDtoProjection");
        string queries = await TextAsync(report.Solution, "Queries.cs");
        queries.ShouldContain("using Shop.Mapping;");
        queries.ShouldContain("orders.Select(OrderProfileExtensions.OrderToOrderDtoProjection);");
    }

    [Fact]
    public async Task FixAsync_NestedMapInOtherProfile_IsManual()
    {
        // Arrange
        string split = Profile.ReplaceLineEndings("\n").Replace("        CreateMap<Address, AddressDto>();\n", "", StringComparison.Ordinal)
            +"\n\npublic class AddressProfile : Profile\n{\n    public AddressProfile()\n    {\n        CreateMap<Address, AddressDto>();\n    }\n}\n";
        Solution solution = Create(("Profile.cs", split), ("Queries.cs", Queries));

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("Address is Address mapped to AddressDto by another profile"));
        (await TextAsync(report.Solution, "Queries.cs")).ShouldContain("ProjectTo<Shop.Mapping.OrderDto>");
    }

    [Fact]
    public async Task FixAsync_NoProjectToCall_WritesNoProjection()
    {
        // Arrange
        Solution solution = Create(("Profile.cs", Profile));

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        string profile = await TextAsync(report.Solution, "Profile.cs");
        profile.ShouldContain("ToOrderDto(this Order source)");
        profile.ShouldNotContain("Expression<Func<");
    }

    private static Solution Create(params (string Name, string Text)[] documents)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(project, TestReferences.AutoMapper);
        foreach ((string name, string text) in documents)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(project), name, text, filePath: name);
        }

        return solution;
    }

    private static async Task<string> TextAsync(Solution solution, string name) =>
        (await solution.Projects.SelectMany(p => p.Documents).Single(d => d.Name == name).GetTextAsync(TestContext.Current.CancellationToken)).ToString();
}
