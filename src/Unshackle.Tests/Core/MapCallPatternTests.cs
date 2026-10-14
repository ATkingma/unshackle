using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// The profile and the Map call in different files: the call sees the profile as it was, so the pattern derives ToB from the CreateMap.
public class MapCallPatternTests
{
    private const string Profile = """
        using AutoMapper;

        namespace Shop.Mapping;

        ACCESS class Order
        {
            public int Id { get; set; }
        }

        ACCESS class OrderDto
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
            internal Shop.Mapping.OrderDto Get(IMapper mapper, Shop.Mapping.Order order) => mapper.Map<Shop.Mapping.OrderDto>(order);
        }
        """;

    [Fact]
    public async Task FixAsync_ProfileInOtherFile_CallsGeneratedMethodWithUsing()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = AddProject(workspace.CurrentSolution, project, "Shop")
            .AddDocument(DocumentId.CreateNewId(project), "Profile.cs", Profile.Replace("ACCESS", "public", StringComparison.Ordinal), filePath: "Profile.cs")
            .AddDocument(DocumentId.CreateNewId(project), "Service.cs", Service, filePath: "Service.cs");

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
        string service = await TextAsync(report.Solution, "Service.cs");
        service.ShouldContain("using Shop.Mapping;");
        service.ShouldContain("=> order.ToOrderDto();");
    }

    [Theory]
    [InlineData("public", null)]
    [InlineData("internal", "becomes internal in another project")]
    public async Task FixAsync_ProfileInReferencedProject_FollowsAccessibility(string access, string? manual)
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var mapping = ProjectId.CreateNewId();
        var app = ProjectId.CreateNewId();
        Solution solution = AddProject(AddProject(workspace.CurrentSolution, mapping, "Mapping"), app, "App")
            .AddProjectReference(app, new ProjectReference(mapping))
            .AddDocument(DocumentId.CreateNewId(mapping), "Profile.cs", Profile.Replace("ACCESS", access, StringComparison.Ordinal), filePath: "Profile.cs")
            .AddDocument(DocumentId.CreateNewId(mapping), "Friend.cs", """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("App")]""", filePath: "Friend.cs")
            .AddDocument(DocumentId.CreateNewId(app), "Service.cs", Service, filePath: "Service.cs");

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        if (manual is null)
        {
            report.Findings.ShouldAllBe(f => f.Result is FixedResult);
            (await TextAsync(report.Solution, "Service.cs")).ShouldContain("=> order.ToOrderDto();");
        }
        else
        {
            report.Findings.ShouldContain(f => f.PatternId == "automapper.map-call" && f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains(manual));
        }
    }

    // The reverse map of a ReverseMap gets the method after its forward one; a Map call in another file still finds it (TAAK-249).
    [Fact]
    public async Task FixAsync_ReverseMapInOtherFile_CallsReverseMethod()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        string profile = Profile.Replace("ACCESS", "public", StringComparison.Ordinal)
            .Replace("CreateMap<Order, OrderDto>();", "CreateMap<Order, OrderDto>().ReverseMap();", StringComparison.Ordinal);
        const string Service = """
            using AutoMapper;

            namespace Shop.Orders;

            public class OrderService
            {
                internal Shop.Mapping.Order Create(IMapper mapper, Shop.Mapping.OrderDto dto) => mapper.Map<Shop.Mapping.Order>(dto);
            }
            """;
        Solution solution = AddProject(workspace.CurrentSolution, project, "Shop")
            .AddDocument(DocumentId.CreateNewId(project), "Profile.cs", profile, filePath: "Profile.cs")
            .AddDocument(DocumentId.CreateNewId(project), "Service.cs", Service, filePath: "Service.cs");

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
        (await TextAsync(report.Solution, "Service.cs")).ShouldContain("=> dto.ToOrder();");
        (await TextAsync(report.Solution, "Profile.cs")).ShouldContain("public static Order ToOrder(this OrderDto source)");
    }

    // No generated method: the reason names what blocks the map, the Manual profile or a CreateMap outside a profile constructor (TAAK-260).
    [Theory]
    [InlineData("AllowNullCollections = true; CreateMap<Order, OrderDto>();", "CreateMap<Order, OrderDto> is in profile OrderProfile, which this fix does not rewrite: ")]
    [InlineData("} public void Mapping(Profile profile) { profile.CreateMap<Order, OrderDto>();",
        "CreateMap<Order, OrderDto> is in OrderProfile.Mapping, outside a Profile constructor")]
    [InlineData("} public void Mapping(Profile profile) { profile.CreateMap(typeof(Order), typeof(OrderDto));",
        "CreateMap<Order, OrderDto> is in OrderProfile.Mapping, outside a Profile constructor")]
    public async Task FixAsync_MapNotRewritten_NamesTheBlocker(string body, string reason)
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        string profile = Profile.Replace("ACCESS", "public", StringComparison.Ordinal)
            .Replace("CreateMap<Order, OrderDto>();", body, StringComparison.Ordinal);
        Solution solution = AddProject(workspace.CurrentSolution, project, "Shop")
            .AddDocument(DocumentId.CreateNewId(project), "Profile.cs", profile, filePath: "Profile.cs")
            .AddDocument(DocumentId.CreateNewId(project), "Service.cs", Service, filePath: "Service.cs");

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.PatternId == "automapper.map-call" && f.Result is ManualResult && ((ManualResult)f.Result).Reason.StartsWith(reason, StringComparison.Ordinal));
    }

    private static Solution AddProject(Solution solution, ProjectId id, string name) =>
        solution.AddProject(id, name, name, LanguageNames.CSharp)
            .WithProjectCompilationOptions(id, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(id, TestReferences.AutoMapper);

    private static async Task<string> TextAsync(Solution solution, string name) =>
        (await solution.Projects.SelectMany(p => p.Documents).Single(d => d.Name == name).GetTextAsync(TestContext.Current.CancellationToken)).ToString();
}
