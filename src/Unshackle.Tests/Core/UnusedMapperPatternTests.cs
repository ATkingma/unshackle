using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// TAAK-289: an IMapper that only served migrated Map calls goes, with the arguments at its callers in other projects, so AddAutoMapper
// can go too. Anything the plan is not sure of keeps the IMapper (and with it the registration).
public class UnusedMapperPatternTests
{
    private const string Shop = """
        using System;
        using AutoMapper;
        using Microsoft.Extensions.DependencyInjection;

        namespace Shop;

        public class Order { public int Id { get; set; } }
        public class OrderDto { public int Id { get; set; } }

        public class ShopProfile : Profile
        {
            public ShopProfile()
            {
                CreateMap<Order, OrderDto>();
            }
        }

        public class OrderService
        {
            private readonly IMapper _mapper;

            public OrderService(string name, IMapper mapper)
            {
                Name = name;
                _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            }

            public string Name { get; }

            public OrderDto Get(Order order) => _mapper.Map<OrderDto>(order);
        }

        public static class Registration
        {
            public static IServiceCollection AddShop(this IServiceCollection services)
            {
                services.AddAutoMapper(typeof(ShopProfile).Assembly);
                return services.AddTransient<OrderService>();
            }
        }
        """;

    private const string Test = """
        using AutoMapper;

        namespace Shop.Tests;

        public class OrderServiceTests
        {
            private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<ShopProfile>()).CreateMapper();

            public OrderDto Get() => new OrderService(name: "shop", mapper: _mapper).Get(new Order());
        }
        """;

    [Fact]
    public async Task FixAsync_MapperOnlyForMigratedCalls_RemovesItWithTheArgumentsInOtherProjectsAndTheRegistration()
    {
        // Arrange
        Solution solution = Create(Shop, Test);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", Reasons(report)));
        string shop = await TextAsync(report.Solution, "Shop.cs");
        shop.ShouldContain("public OrderService(string name)");
        shop.ShouldNotContain("IMapper");
        shop.ShouldNotContain("AddAutoMapper");
        shop.ShouldContain("public OrderDto Get(Order order) => order.ToOrderDto();");
        string test = await TextAsync(report.Solution, "Test.cs");
        test.ShouldContain("new OrderService(name: \"shop\").Get(new Order())");
        test.ShouldNotContain("_mapper");
    }

    [Theory]
    [InlineData("public string Name { get; }", "public string Name { get; } public IMapper Mapper => _mapper;")]
    [InlineData("public string Name { get; }", "public string Name { get; } public OrderService(int size) : this(size.ToString(), null!) { }")]
    [InlineData("public string Name { get; }", "public string Name { get; } public OrderService(string name) => Name = name + \"!\";")]
    public async Task FixAsync_MapperUsedOtherwise_KeepsItAndTheRegistration(string find, string replace)
    {
        // Arrange (handed out as a property; a caller passes null, as a test of the null check would; another constructor has the signature it would get)
        Solution solution = Create(Shop.Replace(find, replace, StringComparison.Ordinal), Test);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string shop = await TextAsync(report.Solution, "Shop.cs");
        shop.ShouldContain("public OrderService(string name, IMapper mapper)");
        shop.ShouldContain("public OrderDto Get(Order order) => order.ToOrderDto();");
        Reasons(report).ShouldContain(r => r.StartsWith("Stays on AutoMapper: IMapper is still used", StringComparison.Ordinal));
        (await TextAsync(report.Solution, "Test.cs")).ShouldContain("mapper: _mapper");
    }

    private const string Seeder = """
        using System;
        using AutoMapper;
        using Microsoft.Extensions.DependencyInjection;

        namespace Shop.Tests;

        public static class Seeder
        {
            public static OrderService? Seed(IServiceProvider services)
            {
                var mapper = services.GetService<IMapper>();
                if (mapper == null)
                {
                    Console.WriteLine("No mapper.");
                    return null;
                }

                return new OrderService("seed", mapper);
            }
        }
        """;

    // The reviewed assumption: a null check on a GetService<IMapper>() lookup only took its branch without a registration. It goes with the
    // lookup only while the solution calls AddAutoMapper, and the finding carries a warning that says so.
    [Fact]
    public async Task FixAsync_ServiceLookupWithNullCheck_RemovesBothWithAWarning()
    {
        // Arrange
        Solution solution = Create(Shop, Seeder);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string seeder = await TextAsync(report.Solution, "Test.cs");
        seeder.ShouldNotContain("GetService");
        seeder.ShouldContain("return new OrderService(\"seed\");");
        report.Findings.Select(f => f.Result).OfType<FixedResult>().ShouldContain(f => f.Warning != null && f.Warning.Contains("null check on mapper", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("services.AddAutoMapper(typeof(ShopProfile).Assembly);", "services.AddSingleton(new MapperConfiguration(cfg => cfg.AddProfile<ShopProfile>()).CreateMapper());")]
    [InlineData("if (mapper == null)", "if (mapper != null)")]
    public async Task FixAsync_ServiceLookupNotRegisteredByAddAutoMapperOrNotANullCheck_KeepsTheLookup(string find, string replace)
    {
        // Arrange
        Solution solution = Create(Shop.Replace(find, replace, StringComparison.Ordinal), Seeder.Replace(find, replace, StringComparison.Ordinal));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert (the parameter still goes: its argument is a plain name)
        string seeder = await TextAsync(report.Solution, "Test.cs");
        seeder.ShouldContain("var mapper = services.GetService<IMapper>();");
        seeder.ShouldContain(replace.StartsWith("if", StringComparison.Ordinal) ? replace : "if (mapper == null)");
        seeder.ShouldContain("return new OrderService(\"seed\");");
        (await TextAsync(report.Solution, "Shop.cs")).ShouldContain("public OrderService(string name)");
    }

    // A top-level AddAutoMapper statement goes with its global statement (it used to be an internal error once nothing used IMapper).
    [Fact]
    public async Task FixAsync_TopLevelAddAutoMapper_IsRemoved()
    {
        // Arrange
        const string Program = """
            using Microsoft.Extensions.DependencyInjection;

            var services = new ServiceCollection();
            services.AddAutoMapper(typeof(Program).Assembly);
            services.AddTransient<object>();
            """;
        Solution solution = Create(Program, OutputKind.ConsoleApplication);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        report.Findings.ShouldContain(f => f.PatternId == "automapper.add-automapper" && f.Result is FixedResult);
        (await TextAsync(report.Solution, "Shop.cs")).ShouldNotContain("AddAutoMapper");
    }

    private static Task<FixReport> FixAsync(Solution solution) =>
        new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

    private static List<string> Reasons(FixReport report) => [.. report.Findings.Select(f => f.Result).OfType<ManualResult>().Select(m => m.Reason)];

    // Shop.cs in project Shop; Test.cs in project Shop.Tests, which references Shop (keys must match across compilations).
    private static Solution Create(string shop, string test)
    {
        Solution solution = Create(shop, OutputKind.DynamicallyLinkedLibrary);
        ProjectId shopId = solution.ProjectIds[0];
        var testId = ProjectId.CreateNewId();
        return solution.AddProject(testId, "Shop.Tests", "Shop.Tests", LanguageNames.CSharp)
            .WithProjectCompilationOptions(testId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(testId, TestReferences.AutoMapper)
            .AddProjectReference(testId, new ProjectReference(shopId))
            .AddDocument(DocumentId.CreateNewId(testId), "Test.cs", test, filePath: "Test.cs");
    }

    private static Solution Create(string shop, OutputKind kind)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        return workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(kind, nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(project, TestReferences.AutoMapper)
            .AddDocument(DocumentId.CreateNewId(project), "Shop.cs", shop, filePath: "Shop.cs");
    }

    private static async Task<string> TextAsync(Solution solution, string name) =>
        (await solution.Projects.SelectMany(p => p.Documents).Single(d => d.Name == name).GetTextAsync(TestContext.Current.CancellationToken)).ToString();
}
