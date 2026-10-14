using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// The profile and the Map(source, destination) call in different files: the profile rewrite writes MapInto because the call needs it.
public class MapIntoPatternTests
{
    private const string Profile = """
        using AutoMapper;

        namespace Shop.Mapping;

        public class Order
        {
            public int Id { get; set; }
        }

        public class OrderDto
        {
            public int Id { get; set; }
        }

        public class OrderProfile : Profile
        {
            public OrderProfile()
            {
                CreateMap<Order, OrderDto>();
            }
        }
        """;

    private const string Service = """
        using AutoMapper;

        namespace Shop.Orders;

        public class OrderService
        {
            public Shop.Mapping.OrderDto Update(IMapper mapper, Shop.Mapping.Order order, Shop.Mapping.OrderDto dto) => mapper.Map(order, dto);
        }
        """;

    [Fact]
    public async Task FixAsync_ProfileInOtherFile_WritesMapIntoAndCallsItWithUsing()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(project, TestReferences.AutoMapper)
            .AddDocument(DocumentId.CreateNewId(project), "Profile.cs", Profile, filePath: "Profile.cs")
            .AddDocument(DocumentId.CreateNewId(project), "Service.cs", Service, filePath: "Service.cs");

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
        string profile = await TextAsync(report.Solution, "Profile.cs");
        profile.ShouldContain("public static OrderDto MapInto(this Order source, OrderDto destination)");
        profile.ShouldContain("destination.Id = source.Id;");
        string service = await TextAsync(report.Solution, "Service.cs");
        service.ShouldContain("using Shop.Mapping;");
        service.ShouldContain("=> order.MapInto(dto);");
    }

    // Map(source, destination) keeps the value of a false Condition (TAAK-255), also on the reverse map of a ReverseMap (TAAK-264): Manual.
    [Fact]
    public async Task FixAsync_ReverseMapConditionWithMapInto_ReportsManual()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(project, TestReferences.AutoMapper)
            .AddDocument(DocumentId.CreateNewId(project), "Profile.cs", Profile
                .Replace("public int Id { get; set; }\n}\n\npublic class OrderDto", "public int Id { get; set; }\n    public bool Flag { get; set; }\n}\n\npublic class OrderDto", StringComparison.Ordinal)
                .Replace("CreateMap<Order, OrderDto>();", "CreateMap<Order, OrderDto>().ReverseMap().ForMember(o => o.Flag, opt => opt.Condition(d => d.Id > 0));", StringComparison.Ordinal), filePath: "Profile.cs")
            .AddDocument(DocumentId.CreateNewId(project), "Service.cs", Service.Replace("mapper.Map(order, dto)", "mapper.Map(dto, order)", StringComparison.Ordinal), filePath: "Service.cs");

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Select(f => f.Result).OfType<ManualResult>().Select(r => r.Reason).ShouldContain(r => r.Contains("Map(source, destination) (MapInto)", StringComparison.Ordinal));
    }

    private static async Task<string> TextAsync(Solution solution, string name) =>
        (await solution.Projects.SelectMany(p => p.Documents).Single(d => d.Name == name).GetTextAsync(TestContext.Current.CancellationToken)).ToString();
}
