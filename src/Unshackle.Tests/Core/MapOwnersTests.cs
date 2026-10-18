using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// TAAK-272: the partial migration. One map that cannot be rewritten stays on AutoMapper with every map it needs (or that needs it);
// the rest of the solution is migrated, and only an unplanned Manual finding still undoes the whole fix.
public class MapOwnersTests
{
    private const string Types = """
        namespace Shop;

        public class Line { public int Id { get; set; } }
        public class LineDto { public int Id { get; set; } }
        public class Order { public int Id { get; set; } public Line Line { get; set; } = new(); }
        public class OrderDto { public int Id { get; set; } public LineDto Line { get; set; } = new(); }
        public class Customer { public int Id { get; set; } }
        public class CustomerDto { public int Id { get; set; } }
        """;

    // BeforeMap is not migrated: CreateMap<Customer, CustomerDto> stays, the other two maps are rewritten.
    private const string Profile = """
        using AutoMapper;

        namespace Shop;

        public class ShopProfile : Profile
        {
            public ShopProfile()
            {
                CreateMap<Order, OrderDto>();
                CreateMap<Line, LineDto>();
                CreateMap<Customer, CustomerDto>().BeforeMap((s, d) => d.Id = 1);
            }
        }
        """;

    private const string Service = """
        using AutoMapper;

        namespace Shop;

        public class ShopService
        {
            private readonly IMapper _mapper;

            public ShopService(IMapper mapper)
            {
                _mapper = mapper;
            }

            public OrderDto Order(Order order) => _mapper.Map<OrderDto>(order);

            public CustomerDto Customer(Customer customer) => _mapper.Map<CustomerDto>(customer);
        }
        """;

    [Fact]
    public async Task FixAsync_OneMapWithAProblem_KeepsOnlyThatMapOnAutoMapper()
    {
        // Arrange
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", Service));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string profile = await TextAsync(report.Solution, "Profile.cs");
        profile.ShouldContain("CreateMap<Customer, CustomerDto>().BeforeMap((s, d) => d.Id = 1);");
        profile.ShouldNotContain("CreateMap<Order, OrderDto>();");
        profile.ShouldNotContain("CreateMap<Line, LineDto>();");
        profile.ShouldContain("public static OrderDto ToOrderDto(this Order source)");
        profile.ShouldContain("Line = source.Line?.ToLineDto()");
        string service = await TextAsync(report.Solution, "Service.cs");
        service.ShouldContain("order.ToOrderDto()");
        service.ShouldContain("_mapper.Map<CustomerDto>(customer)");
        Reasons(report).ShouldContain(r => r.StartsWith("Stays on AutoMapper: CreateMap<Customer, CustomerDto> stays in the profile", StringComparison.Ordinal) && r.Contains("BeforeMap"));
        Reasons(report).ShouldContain(r => r.StartsWith("Stays on AutoMapper: this call keeps using IMapper: the map Customer → CustomerDto", StringComparison.Ordinal));
    }

    // REQ-2: a map that stays keeps the map AutoMapper runs for its member; REQ-3: the map that would call the ToY() of a kept map stays too.
    [Theory]
    [InlineData("CreateMap<Order, OrderDto>().BeforeMap((s, d) => d.Id = 1);", "CreateMap<Line, LineDto>();")]
    [InlineData("CreateMap<Order, OrderDto>();", "CreateMap<Line, LineDto>().BeforeMap((s, d) => d.Id = 1);")]
    public async Task FixAsync_KeptMap_KeepsTheMapsItIsConnectedWith(string orderMap, string lineMap)
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Order, OrderDto>();", orderMap, StringComparison.Ordinal)
            .Replace("CreateMap<Line, LineDto>();", lineMap, StringComparison.Ordinal);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", Service));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string text = await TextAsync(report.Solution, "Profile.cs");
        text.ShouldContain(orderMap);
        text.ShouldContain(lineMap);
        (await TextAsync(report.Solution, "Service.cs")).ShouldContain("_mapper.Map<OrderDto>(order)");
    }

    // REQ-5: Map<B>(object) picks the map at run time: every map to B stays, the others are migrated.
    [Fact]
    public async Task FixAsync_UntypedSource_KeepsEveryMapToItsDestination()
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Customer, CustomerDto>().BeforeMap((s, d) => d.Id = 1);", "CreateMap<Customer, CustomerDto>();", StringComparison.Ordinal);
        string service = Service.Replace("_mapper.Map<CustomerDto>(customer)", "_mapper.Map<CustomerDto>((object)customer)", StringComparison.Ordinal);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", service));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string text = await TextAsync(report.Solution, "Profile.cs");
        text.ShouldContain("CreateMap<Customer, CustomerDto>();");
        text.ShouldContain("ToOrderDto(this Order source)");
        Reasons(report).ShouldContain(r => r.Contains("every map to CustomerDto stays", StringComparison.Ordinal));
    }

    // REQ-8: a test that stubs IMapper keeps the stubbed map (and the production call it intercepts) on AutoMapper.
    [Fact]
    public async Task FixAsync_MockedMap_StaysOnAutoMapper()
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Customer, CustomerDto>().BeforeMap((s, d) => d.Id = 1);", "CreateMap<Customer, CustomerDto>();", StringComparison.Ordinal);
        const string Test = """
            using AutoMapper;
            using NSubstitute;

            namespace Shop;

            public class ShopServiceTests
            {
                public CustomerDto Customer()
                {
                    IMapper mapper = Substitute.For<IMapper>();
                    mapper.Map<CustomerDto>(Arg.Any<Customer>()).Returns(new CustomerDto());
                    return new ShopService(mapper).Customer(new Customer());
                }
            }
            """;
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", Service), ("Test.cs", Test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Test.cs")).ShouldContain("mapper.Map<CustomerDto>(Arg.Any<Customer>()).Returns(new CustomerDto());");
        string service = await TextAsync(report.Solution, "Service.cs");
        service.ShouldContain("_mapper.Map<CustomerDto>(customer)");
        service.ShouldContain("order.ToOrderDto()");
        (await TextAsync(report.Solution, "Profile.cs")).ShouldContain("CreateMap<Customer, CustomerDto>();");
    }

    private const string MockedTest = """
        using AutoMapper;
        using NSubstitute;

        namespace Shop;

        public class ShopServiceTests
        {
            private readonly IMapper _mapper = Substitute.For<IMapper>();

            public CustomerDto Customer()
            {
                _mapper.Map<CustomerDto>(Arg.Any<Customer>()).Returns(new CustomerDto());
                return new ShopService(_mapper).Customer(new Customer());
            }
        }
        """;

    private const string OtherService = """
        using AutoMapper;

        namespace Shop;

        public class ReportService
        {
            private readonly IMapper _mapper;

            public ReportService(IMapper mapper)
            {
                _mapper = mapper;
            }

            public CustomerDto Customer(Customer customer) => _mapper.Map<CustomerDto>(customer);
        }
        """;

    // TAAK-279 REQ-1–3: the stubbed map is shared: CreateMap stays for the class under test (ShopService), ToB() is generated, and a class no
    // test hands the mock to (ReportService) is migrated.
    [Fact]
    public async Task FixAsync_MockedMap_KeepsOnlyTheClassUnderTestOnIMapper()
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Customer, CustomerDto>().BeforeMap((s, d) => d.Id = 1);", "CreateMap<Customer, CustomerDto>();", StringComparison.Ordinal);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", Service), ("Other.cs", OtherService), ("Test.cs", MockedTest));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string text = await TextAsync(report.Solution, "Profile.cs");
        text.ShouldContain("CreateMap<Customer, CustomerDto>();");
        text.ShouldContain("public static CustomerDto ToCustomerDto(this Customer source)");
        text.ShouldNotContain("CreateMap<Order, OrderDto>();");
        (await TextAsync(report.Solution, "Service.cs")).ShouldContain("_mapper.Map<CustomerDto>(customer)");
        (await TextAsync(report.Solution, "Other.cs")).ShouldContain("customer.ToCustomerDto()");
        (await TextAsync(report.Solution, "Test.cs")).ShouldBe(MockedTest);
        Reasons(report).ShouldContain(r => r.Contains("ShopService is a class under test whose mocked IMapper stubs the map Customer → CustomerDto", StringComparison.Ordinal));
    }

    // REQ-4: a test that never constructs its class under test (a fixture or DI does) proves nothing: the pair stays everywhere.
    [Fact]
    public async Task FixAsync_MockedMapWithoutConstruction_KeepsThePairEverywhere()
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Customer, CustomerDto>().BeforeMap((s, d) => d.Id = 1);", "CreateMap<Customer, CustomerDto>();", StringComparison.Ordinal);
        string test = MockedTest.Replace("return new ShopService(_mapper).Customer(new Customer());", "return new CustomerDto();", StringComparison.Ordinal);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", Service), ("Other.cs", OtherService), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Profile.cs")).ShouldNotContain("ToCustomerDto");
        (await TextAsync(report.Solution, "Other.cs")).ShouldContain("_mapper.Map<CustomerDto>(customer)");
    }

    // REQ-2: a class the class under test hands its IMapper to runs the stub too, so it keeps IMapper.
    [Fact]
    public async Task FixAsync_ClassUnderTestHandsItsMapperOn_KeepsThatClassOnIMapper()
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Customer, CustomerDto>().BeforeMap((s, d) => d.Id = 1);", "CreateMap<Customer, CustomerDto>();", StringComparison.Ordinal);
        string service = Service.Replace("public CustomerDto Customer(Customer customer) => _mapper.Map<CustomerDto>(customer);",
            "public CustomerDto Customer(Customer customer) => new ReportService(_mapper).Customer(customer);", StringComparison.Ordinal);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", service), ("Other.cs", OtherService), ("Test.cs", MockedTest));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Other.cs")).ShouldContain("_mapper.Map<CustomerDto>(customer)");
        (await TextAsync(report.Solution, "Profile.cs")).ShouldContain("CreateMap<Customer, CustomerDto>();");
    }

    // REQ-5: a shared map keeps the map AutoMapper runs for its member in the profile, next to that member's generated ToY().
    [Fact]
    public async Task FixAsync_SharedMap_KeepsTheMapsItReachesInTheProfile()
    {
        // Arrange
        string test = MockedTest.Replace("_mapper.Map<CustomerDto>(Arg.Any<Customer>()).Returns(new CustomerDto());", "_mapper.Map<OrderDto>(Arg.Any<Order>()).Returns(new OrderDto());", StringComparison.Ordinal)
            .Replace("public CustomerDto Customer()", "public OrderDto Order()", StringComparison.Ordinal)
            .Replace("return new ShopService(_mapper).Customer(new Customer());", "return new ShopService(_mapper).Order(new Order());", StringComparison.Ordinal);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", Service), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string text = await TextAsync(report.Solution, "Profile.cs");
        text.ShouldContain("CreateMap<Order, OrderDto>();");
        text.ShouldContain("CreateMap<Line, LineDto>();");
        text.ShouldContain("public static LineDto ToLineDto(this Line source)");
        (await TextAsync(report.Solution, "Service.cs")).ShouldContain("_mapper.Map<OrderDto>(order)");
    }

    // REQ-11: a Manual finding that is not a planned keep (here a test configuration with maps of its own) still leaves everything as it was.
    [Fact]
    public async Task FixAsync_UnplannedManual_LeavesTheSolutionAsItIs()
    {
        // Arrange
        const string Test = """
            using AutoMapper;

            namespace Shop;

            public class MapperTests
            {
                public void Check() => new MapperConfiguration(cfg => cfg.CreateMap<Order, OrderDto>()).AssertConfigurationIsValid();
            }
            """;
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", Service), ("Test.cs", Test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        report.Solution.ShouldBeSameAs(solution);
        Reasons(report).ShouldContain(r => r.StartsWith(Fixer.NotRewrittenPrefix, StringComparison.Ordinal));
    }

    private static Task<FixReport> FixAsync(Solution solution) =>
        new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

    private static List<string> Reasons(FixReport report) => [.. report.Findings.Select(f => f.Result).OfType<ManualResult>().Select(m => m.Reason)];

    private static Solution Create(params (string Name, string Text)[] documents)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
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
