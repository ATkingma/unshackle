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
    public async Task FixAsync_NestedProfile_WritesProjectionNextToOutermostType()
    {
        // Arrange
        const string Dto = """
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
                    public Mapping()
                    {
                        CreateMap<TodoItem, TodoItemDto>();
                    }
                }
            }
            """;
        const string Query = """
            using AutoMapper;
            using AutoMapper.QueryableExtensions;

            namespace Shop.Orders;

            public class TodoQueries
            {
                public System.Linq.IQueryable<Shop.Mapping.TodoItemDto> All(IMapper mapper, System.Linq.IQueryable<Shop.Mapping.TodoItem> items) =>
                    items.ProjectTo<Shop.Mapping.TodoItemDto>(mapper.ConfigurationProvider);
            }
            """;
        Solution solution = Create(("Dto.cs", Dto), ("Queries.cs", Query));

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        (await TextAsync(report.Solution, "Dto.cs")).ShouldContain("public static Expression<Func<TodoItem, TodoItemDto>> TodoItemToTodoItemDtoProjection => source => new TodoItemDto");
        (await TextAsync(report.Solution, "Queries.cs")).ShouldContain("items.Select(TodoItemDtoMappingExtensions.TodoItemToTodoItemDtoProjection);");
    }

    [Fact]
    public async Task FixAsync_MapFromThroughMaybeNullMember_ProjectionIsManual()
    {
        // Arrange: nullable oblivious, so Order.Address may be null; ToOrderDto can say ?. like AutoMapper's null-safe MapFrom, an expression tree cannot.
        string profile = Profile.ReplaceLineEndings("\n")
            .Replace("        CreateMap<Order, OrderDto>();\n", "        CreateMap<Order, OrderDto>().ForMember(d => d.Id, o => o.MapFrom(s => s.Address.City.Length));\n", StringComparison.Ordinal);
        Solution solution = Create(("Profile.cs", profile), ("Queries.cs", Queries));

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("an expression tree throws"), string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
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

    private const string CapturedProfile = """
        #nullable enable
        using System.Collections.Generic;
        using System.Linq;
        using AutoMapper;

        namespace Shop.Mapping;

        public class Reading
        {
            public int AppUserId { get; set; }
            public int PagesRead { get; set; }
        }

        public class Series
        {
            public int Id { get; set; }
            public List<Reading> Readings { get; set; } = [];
        }

        public class SeriesDto
        {
            public int Id { get; set; }
            public int PagesRead { get; set; }
        }

        public class SeriesProfile : Profile
        {
            public SeriesProfile()
            {
                int userId = 0;

                CreateMap<Series, SeriesDto>()
                    .ForMember(d => d.PagesRead, o => o.MapFrom(s => s.Readings.Where(r => r.AppUserId == userId).Sum(r => r.PagesRead)));
            }
        }
        """;

    private static string CapturedQueries(string call) => $$"""
        using AutoMapper;
        using AutoMapper.QueryableExtensions;
        using Shop.Mapping;

        namespace Shop.Orders;

        public class SeriesQueries
        {
            public System.Linq.IQueryable<SeriesDto> All(IMapper mapper, System.Linq.IQueryable<Series> series, int userId, long wide) =>
                {{call}};
        }
        """;

    [Fact]
    public async Task FixAsync_CapturedVariable_ProjectionTakesItAsParameter()
    {
        // Arrange: kavita's userId placeholder; ProjectTo fills it, Map and a ProjectTo without parameters keep 0 (measured, TAAK-265).
        Solution solution = Create(("Profile.cs", CapturedProfile), ("Queries.cs", CapturedQueries("series.ProjectTo<SeriesDto>(mapper.ConfigurationProvider, new { userId })")));

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        string profile = await TextAsync(report.Solution, "Profile.cs");
        profile.ShouldContain("ToSeriesDto(this Series source, int userId = 0) => new()");
        profile.ShouldContain("SeriesToSeriesDtoProjection(int userId = 0) => source => new SeriesDto");
        profile.ShouldNotContain("int userId = 0;");
        (await TextAsync(report.Solution, "Queries.cs")).ShouldContain("series.Select(SeriesProfileExtensions.SeriesToSeriesDtoProjection(userId));");
    }

    [Theory]
    [InlineData("series.ProjectTo<SeriesDto>(mapper.ConfigurationProvider, new { other = userId })", "reads no variable of the profile constructor with that name")]
    [InlineData("series.ProjectTo<SeriesDto>(mapper.ConfigurationProvider, new { userId = wide })", "another type than the variable of the profile constructor (int)")]
    [InlineData("series.ProjectTo<SeriesDto>(mapper.ConfigurationProvider, userId)", "fills in or leaves out members per call")]
    public async Task FixAsync_ParametersThatDoNotFitTheCapturedVariable_AreManual(string call, string reason)
    {
        // Arrange
        Solution solution = Create(("Profile.cs", CapturedProfile), ("Queries.cs", CapturedQueries(call)));

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains(reason), string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        (await TextAsync(report.Solution, "Queries.cs")).ShouldContain("ProjectTo<SeriesDto>");
    }

    [Fact]
    public async Task FixAsync_SameMapInTwoProfiles_NamesTheMeasuredAutoMapperBehaviour()
    {
        // Arrange: AutoMapper 14 uses the map registered last and does not throw (only AssertConfigurationIsValid does); the order is not known here.
        string twice = Profile.ReplaceLineEndings("\n") + "\n\npublic class SecondProfile : Profile\n{\n    public SecondProfile()\n    {\n        CreateMap<Order, OrderDto>();\n    }\n}\n";
        Solution solution = Create(("Profile.cs", twice), ("Queries.cs", Queries));

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("Several profiles map Order to OrderDto; AutoMapper uses the one registered last"),
            string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        (await TextAsync(report.Solution, "Queries.cs")).ShouldContain("ProjectTo<Shop.Mapping.OrderDto>");
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
