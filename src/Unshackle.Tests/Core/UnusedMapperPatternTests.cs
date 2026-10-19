using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
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
    // lookup only while the scope of the lookup surely registers IMapper (TAAK-292, council decision 3), and the finding names the call.
    [Fact]
    public async Task FixAsync_ServiceLookupInTheRegisteringProject_RemovesItAndTheNullCheckWithAWarning()
    {
        // Arrange
        Solution solution = Create(Shop, Test, seeder: Seeder);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string seeder = await TextAsync(report.Solution, "Seeder.cs");
        seeder.ShouldNotContain("GetService");
        seeder.ShouldContain("return new OrderService(\"seed\");");
        List<string> warnings = [.. report.Findings.Select(f => f.Result).OfType<FixedResult>().Select(f => f.Warning).OfType<string>()];
        warnings.ShouldContain(w => w.Contains("AddAutoMapper at Shop.cs:", StringComparison.Ordinal), string.Join(" | ", warnings));
    }

    // Rule 1: the AddAutoMapper call may also be in a project that depends on the lookup's project (the host).
    [Fact]
    public async Task FixAsync_ServiceLookupRegisteredByADependentHost_RemovesIt()
    {
        // Arrange
        const string Host = """
            using Microsoft.Extensions.DependencyInjection;

            namespace Shop.Host;

            public static class Startup
            {
                public static void Configure(IServiceCollection services) => services.AddAutoMapper(typeof(Shop.ShopProfile).Assembly);
            }
            """;
        Solution solution = Create(Shop.Replace("services.AddAutoMapper(typeof(ShopProfile).Assembly);", "", StringComparison.Ordinal), Host, seeder: Seeder);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Seeder.cs")).ShouldNotContain("GetService");
        report.Findings.Select(f => f.Result).OfType<FixedResult>().ShouldContain(f => f.Warning != null && f.Warning.Contains("AddAutoMapper at Test.cs:7", StringComparison.Ordinal));
    }

    // Each failing rule keeps the lookup and its null check, and the report names the rule with file:line and a typed reason code
    // (council round 3: target-typed containers and deferred lambdas included).
    [Theory]
    [InlineData("", "rule 1: no AddAutoMapper call in Shop.Tests or a project that depends on it", KeptReason.NoRegistration)]
    [InlineData("in shop", "rule 1: no AddAutoMapper call in Shop or a project that depends on it", KeptReason.NoRegistration)]
    [InlineData("if", "is conditional or deferred (inside an if)", KeptReason.ConditionalOrDeferredRegistration)]
    [InlineData("#if", "is conditional or deferred (inside an #if)", KeptReason.ConditionalOrDeferredRegistration)]
    [InlineData("stored lambda", "is conditional or deferred (inside a lambda that is not passed directly to a registration or builder call",
        KeptReason.ConditionalOrDeferredRegistration)]
    [InlineData("local function", "is conditional or deferred (inside a local function", KeptReason.ConditionalOrDeferredRegistration)]
    [InlineData("container", "rule 3: Seeder.cs:21 builds a new ServiceCollection() without AddAutoMapper in the same method", KeptReason.ContainerWithoutRegistration)]
    [InlineData("target-typed", "rule 3: Seeder.cs:21 builds a new ServiceCollection() (target-typed new()) without AddAutoMapper", KeptReason.ContainerWithoutRegistration)]
    public async Task FixAsync_ServiceLookupNotSurelyRegistered_KeepsTheLookupAndNamesTheRule(string variant, string rule, KeptReason code)
    {
        // Arrange ("": the lookup sits in the test project, which AddAutoMapper in Shop does not cover)
        string shop = variant switch
        {
            "in shop" => Shop.Replace("services.AddAutoMapper(typeof(ShopProfile).Assembly);", "services.AddSingleton(new MapperConfiguration(cfg => cfg.AddProfile<ShopProfile>()).CreateMapper());", StringComparison.Ordinal),
            "if" => Shop.Replace("services.AddAutoMapper(", "if (services.Count > 0) services.AddAutoMapper(", StringComparison.Ordinal),
            "#if" => Shop.Replace("services.AddAutoMapper(typeof(ShopProfile).Assembly);", "#if !UNSET\n        services.AddAutoMapper(typeof(ShopProfile).Assembly);\n#endif", StringComparison.Ordinal),
            "stored lambda" => Shop.Replace("services.AddAutoMapper(typeof(ShopProfile).Assembly);",
                "Action<IServiceCollection> register = s => s.AddAutoMapper(typeof(ShopProfile).Assembly);\n        register(services);", StringComparison.Ordinal),
            "local function" => Shop.Replace("services.AddAutoMapper(typeof(ShopProfile).Assembly);",
                "void Register() => services.AddAutoMapper(typeof(ShopProfile).Assembly);\n        Register();", StringComparison.Ordinal),
            _ => Shop,
        };
        string seeder = variant switch
        {
            "container" => Seeder.Replace("    }\n}", "    }\n\n    public static IServiceProvider Build() => new ServiceCollection().BuildServiceProvider();\n}", StringComparison.Ordinal),
            "target-typed" => Seeder.Replace("    }\n}", "    }\n\n    public static ServiceCollection Build() => new();\n}", StringComparison.Ordinal),
            _ => Seeder,
        };
        Solution solution = variant == "" ? Create(shop, seeder) : Create(shop, Test, seeder: seeder);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert (the parameter still goes: its argument is a plain name)
        string kept = await TextAsync(report.Solution, variant == "" ? "Test.cs" : "Seeder.cs");
        kept.ShouldContain("var mapper = services.GetService<IMapper>();");
        kept.ShouldContain("if (mapper == null)");
        kept.ShouldContain("return new OrderService(\"seed\");");
        Reasons(report).ShouldContain(r => r.StartsWith("Stays on AutoMapper: the GetService<IMapper>() lookup stays", StringComparison.Ordinal) && r.Contains(rule, StringComparison.Ordinal),
            string.Join(" | ", Reasons(report)));
        report.Findings.Select(f => f.Result).OfType<ManualResult>().ShouldContain(m => m.Kept == code && !m.Reason.StartsWith("Stays on", StringComparison.Ordinal));
        (await TextAsync(report.Solution, "Shop.cs")).ShouldContain("public OrderService(string name)");
    }

    // Council round 3 (Q3b): a lambda passed directly to an unconditional call on IServiceCollection runs while the container is built.
    [Fact]
    public async Task FixAsync_AddAutoMapperInALambdaPassedToARegistrationCall_RemovesTheLookup()
    {
        // Arrange
        string shop = Shop.Replace("services.AddAutoMapper(typeof(ShopProfile).Assembly);", "services.With(s => s.AddAutoMapper(typeof(ShopProfile).Assembly));", StringComparison.Ordinal)
            + "\n\npublic static class ServiceCollectionExtensions\n{\n    public static IServiceCollection With(this IServiceCollection services, Action<IServiceCollection> add)\n    {\n"
            + "        add(services);\n        return services;\n    }\n}\n";
        Solution solution = Create(shop, Test, seeder: Seeder);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Seeder.cs")).ShouldNotContain("GetService");
        report.Findings.Select(f => f.Result).OfType<ManualResult>().ShouldNotContain(m => m.Kept != KeptReason.None);
    }

    // Council round 4 (Q3): a user type that only shares the name of a builder type does not count as a registration target.
    [Fact]
    public async Task FixAsync_AddAutoMapperInALambdaPassedToALookalikeBuilder_KeepsTheLookup()
    {
        // Arrange
        string shop = Shop.Replace("services.AddAutoMapper(typeof(ShopProfile).Assembly);",
                "new WebApplicationBuilder(services).ConfigureServices(s => s.AddAutoMapper(typeof(ShopProfile).Assembly));", StringComparison.Ordinal)
            + "\n\npublic class WebApplicationBuilder\n{\n    private readonly IServiceCollection _services;\n\n"
            + "    public WebApplicationBuilder(IServiceCollection services)\n    {\n        _services = services;\n    }\n\n"
            + "    public void ConfigureServices(Action<IServiceCollection> add)\n    {\n    }\n}\n";
        Solution solution = Create(shop, Test, seeder: Seeder);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Seeder.cs")).ShouldContain("var mapper = services.GetService<IMapper>();");
        report.Findings.Select(f => f.Result).OfType<ManualResult>().ShouldContain(m => m.Kept == KeptReason.ConditionalOrDeferredRegistration);
    }

    [Fact]
    public async Task FixAsync_ServiceLookupNotANullCheck_KeepsTheLookup()
    {
        // Arrange
        Solution solution = Create(Shop, Test, seeder: Seeder.Replace("if (mapper == null)", "if (mapper != null)", StringComparison.Ordinal));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string seeder = await TextAsync(report.Solution, "Seeder.cs");
        seeder.ShouldContain("var mapper = services.GetService<IMapper>();");
        seeder.ShouldContain("if (mapper != null)");
    }

    // Council decision 4: a class the removals leave empty stays, with an Info line in the finding.
    [Fact]
    public async Task FixAsync_FactoryClassLeftEmpty_KeepsItAndReportsAnInfoLine()
    {
        // Arrange
        const string Factory = """
            using AutoMapper;

            namespace Shop.Tests;

            public static class MapperHelper
            {
                public static IMapper Create() => new MapperConfiguration(cfg => cfg.AddProfile<ShopProfile>()).CreateMapper();
            }

            public class OrderServiceTests
            {
                public OrderDto Get() => new OrderService("shop", MapperHelper.Create()).Get(new Order());
            }
            """;
        Solution solution = Create(Shop, Factory);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string test = await TextAsync(report.Solution, "Test.cs");
        test.ShouldContain("public static class MapperHelper");
        test.ShouldNotContain("Create()");
        report.Findings.Select(f => f.Result).OfType<FixedResult>().ShouldContain(f => f.Info == "the class MapperHelper is now empty and kept; removing it is a separate refactoring.");
    }

    // The plan keys symbols by documentation comment id: overloaded constructors and a partial type over two files each keep their own key.
    [Fact]
    public async Task FixAsync_OverloadsAndPartialType_RemovesEveryMapper()
    {
        // Arrange
        const string Part = """
            namespace Shop;

            public partial class OrderService
            {
                public OrderService(AutoMapper.IMapper mapper) : this("default", mapper)
                {
                }

                public OrderDto Again(Order order) => _mapper.Map<OrderDto>(order);
            }
            """;
        Solution solution = Create(Shop.Replace("public class OrderService", "public partial class OrderService", StringComparison.Ordinal), Test, seeder: Part);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", Reasons(report)));
        string shop = await TextAsync(report.Solution, "Shop.cs");
        shop.ShouldContain("public OrderService(string name)");
        shop.ShouldNotContain("IMapper");
        shop.ShouldNotContain("AddAutoMapper");
        string part = await TextAsync(report.Solution, "Seeder.cs");
        part.ShouldContain("public OrderService() : this(\"default\")");
        part.ShouldContain("public OrderDto Again(Order order) => order.ToOrderDto();");
    }

    // One IMapper use the fix cannot remove keeps AddAutoMapper, while an IMapper elsewhere that only served migrated calls still goes.
    [Fact]
    public async Task FixAsync_OneRemainingMapperUse_KeepsAddAutoMapper()
    {
        // Arrange
        const string Other = """
            using AutoMapper;

            namespace Shop.Tests;

            public class Exporter
            {
                private readonly IMapper _mapper;

                public Exporter(IMapper mapper)
                {
                    _mapper = mapper;
                }

                public IMapper Mapper => _mapper;
            }
            """;
        Solution solution = Create(Shop, Other);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string shop = await TextAsync(report.Solution, "Shop.cs");
        shop.ShouldContain("public OrderService(string name)");
        shop.ShouldContain("services.AddAutoMapper(typeof(ShopProfile).Assembly);");
        (await TextAsync(report.Solution, "Test.cs")).ShouldContain("public IMapper Mapper => _mapper;");
        Reasons(report).ShouldContain(r => r.StartsWith("Stays on AutoMapper: IMapper is still used", StringComparison.Ordinal));
    }

    // The order is part of the design: the call patterns migrate the Map calls, then unused-imapper removes the mapper, then add-automapper
    // sees no IMapper user left.
    [Fact]
    public void AutoMapperFix_PatternOrder_CallPatternsThenUnusedMapperThenAddAutoMapper()
    {
        // Arrange
        List<string> ids = [.. new AutoMapperFix().Patterns.Select(p => p.Id)];

        // Act
        int unused = ids.IndexOf("automapper.unused-imapper");
        int register = ids.IndexOf("automapper.add-automapper");

        // Assert
        unused.ShouldBeGreaterThan(ids.IndexOf("automapper.map-call"));
        unused.ShouldBeGreaterThan(ids.IndexOf("automapper.map-into"));
        unused.ShouldBeGreaterThan(ids.IndexOf("automapper.project-to"));
        register.ShouldBeGreaterThan(unused);
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

    // The reasons as the report renders them (a kept finding with its "Stays on AutoMapper: " prefix).
    private static List<string> Reasons(FixReport report) => [.. report.Findings.Select(f => f.Result).OfType<ManualResult>().Select(ReportRow.ReasonOf)];

    // Shop.cs (and Seeder.cs) in project Shop; Test.cs in project Shop.Tests, which references Shop (keys must match across compilations).
    private static Solution Create(string shop, string test, string? seeder = null)
    {
        Solution solution = Create(shop, OutputKind.DynamicallyLinkedLibrary);
        if (seeder is not null)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(solution.ProjectIds[0]), "Seeder.cs", seeder, filePath: "Seeder.cs");
        }

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
