using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.AutoMapper;

// TAAK-311 (council A1): a stub of a mocked IMapper goes only when its value provably equals what the generated method returns and the
// mock serves only maps that are all migrated; otherwise the stub stays and its Manual row names the test (file:line) and why.
public class MockSetUpPatternTests
{
    private const string Types = """
        namespace Shop;

        public class Customer { public int Id { get; set; } public string Name { get; set; } = ""; }
        public class CustomerDto { public int Id { get; set; } public string Name { get; set; } = ""; }
        public record CustomerRecord { public int Id { get; set; } public string Name { get; set; } = ""; }
        public class Order { public int Id { get; set; } }
        public class OrderDto { public int Id { get; set; } }
        """;

    private const string Profile = """
        using AutoMapper;

        namespace Shop;

        public class ShopProfile : Profile
        {
            public ShopProfile()
            {
                CreateMap<Customer, CustomerDto>();
                CreateMap<Customer, CustomerRecord>();
                CreateMap<Order, OrderDto>();
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

            public CustomerDto Customer(Customer customer) => _mapper.Map<CustomerDto>(customer);
        }
        """;

    // A stand-in for Moq's API surface (an assembly named Moq), so the Setup(...).Returns(...) shape is covered without the package.
    private const string MoqStandIn = """
        using System;
        using System.Linq.Expressions;

        namespace Moq;

        public class Mock<T> where T : class
        {
            public T Object => null!;

            public ISetup<TResult> Setup<TResult>(Expression<Func<T, TResult>> expression) => null!;

            public void Verify<TResult>(Expression<Func<T, TResult>> expression) { }
        }

        public interface ISetup<TResult>
        {
            void Returns(TResult value);
        }
        """;

    private static readonly Lazy<MetadataReference> s_moq = new(() =>
    {
        var compilation = CSharpCompilation.Create("Moq", [CSharpSyntaxTree.ParseText(MoqStandIn)],
            [.. TestReferences.AutoMapper, MetadataReference.CreateFromFile(typeof(Expression).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        compilation.Emit(stream).Success.ShouldBeTrue();
        return MetadataReference.CreateFromImage(stream.ToArray());
    });

    private static string Test(string body) => $$"""
        using AutoMapper;
        using NSubstitute;

        namespace Shop;

        public class ShopServiceTests
        {
            public object Customer()
            {
                IMapper mapper = Substitute.For<IMapper>();
        {{body}}
            }
        }
        """;

    [Fact]
    public async Task FixAsync_StubEqualToGeneratedMap_RemovesStubAndMigratesClassUnderTest()
    {
        // Arrange
        string test = Test("""
                    var customer = new Customer { Id = 7, Name = "Ann" };
                    var expected = new CustomerDto { Id = customer.Id, Name = "Ann" };
                    mapper.Map<CustomerDto>(customer).Returns(expected);
                    return new ShopService(mapper).Customer(customer).Name == expected.Name;
            """);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", Service), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string after = await TextAsync(report.Solution, "Test.cs");
        after.ShouldNotContain(".Returns(expected);");
        after.ShouldContain("new ShopService().Customer(customer)");
        (await TextAsync(report.Solution, "Service.cs")).ShouldContain("customer.ToCustomerDto()");
        (await TextAsync(report.Solution, "Profile.cs")).ShouldNotContain("CreateMap<Customer, CustomerDto>");
        report.Findings.Select(f => f.Result).OfType<FixedResult>().Select(f => f.Warning)
            .ShouldContain(w => w != null && w.Contains("The stub of IMapper.Map<CustomerDto> at Test.cs:13 is removed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FixAsync_MoqStubEqualToGeneratedMap_RemovesStub()
    {
        // Arrange
        const string Test = """
            using Moq;

            namespace Shop;

            public class ShopServiceTests
            {
                private readonly Mock<AutoMapper.IMapper> _mapper = new();

                public object Customer()
                {
                    var customer = new Customer { Id = 7 };
                    var expected = new CustomerDto { Id = 7 };
                    _mapper.Setup(m => m.Map<CustomerDto>(customer)).Returns(expected);
                    return new ShopService(_mapper.Object).Customer(customer).Id == expected.Id;
                }
            }
            """;
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", Service), ("Test.cs", Test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        string after = await TextAsync(report.Solution, "Test.cs");
        after.ShouldNotContain("Setup(");
        after.ShouldContain("new ShopService().Customer(customer)");
        (await TextAsync(report.Solution, "Service.cs")).ShouldContain("customer.ToCustomerDto()");
    }

    [Theory]
    [InlineData("""
                var customer = new Customer { Id = 7, Name = "Ann" };
                var expected = new CustomerDto { Id = 7, Name = "Bob" };
                mapper.Map<CustomerDto>(customer).Returns(expected);
                return new ShopService(mapper).Customer(customer);
        """, "CustomerDto.Name is not provably the value the generated method copies")]
    [InlineData("""
                mapper.Map<CustomerDto>(Arg.Any<Customer>()).Returns(new CustomerDto());
                return new ShopService(mapper).Customer(new Customer());
        """, "an argument matcher such as It.IsAny or Arg.Any")]
    [InlineData("""
                var customer = new Customer { Id = 7 };
                var expected = new CustomerDto { Id = 7 };
                mapper.Map<CustomerDto>(customer).Returns(expected);
                return ReferenceEquals(new ShopService(mapper).Customer(customer), expected);
        """, "the test uses the stubbed CustomerDto itself at Test.cs:")]
    [InlineData("""
                var customer = new Customer { Id = 7 };
                var expected = new CustomerDto { Id = 7 };
                customer.Id = 8;
                mapper.Map<CustomerDto>(customer).Returns(expected);
                return new ShopService(mapper).Customer(customer);
        """, "the test changes customer after creating it")]
    [InlineData("""
                var customer = new Customer { Id = 7 };
                var expected = new CustomerDto { Id = 7 };
                mapper.Map<CustomerDto>(customer).Returns(expected);
                new ShopService(mapper).Customer(customer);
                return mapper.Received().Map<CustomerDto>(customer);
        """, "the mock is used another way at Test.cs:")]
    public async Task FixAsync_StubNotProvablyEqual_KeepsStubAndNamesTest(string body, string reason)
    {
        // Arrange
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", Service), ("Test.cs", Test(body)));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Test.cs")).ShouldContain(".Returns(");
        (await TextAsync(report.Solution, "Service.cs")).ShouldContain("_mapper.Map<CustomerDto>(customer)");
        Reasons(report).ShouldContain(r => r.Contains("keeps calling IMapper", StringComparison.Ordinal) && r.Contains(reason, StringComparison.Ordinal), string.Join(" | ", Reasons(report)));
    }

    [Fact]
    public async Task FixAsync_RecordComparedWhole_RemovesStub()
    {
        // Arrange
        const string RecordService = """
            using AutoMapper;

            namespace Shop;

            public class ShopService
            {
                private readonly IMapper _mapper;

                public ShopService(IMapper mapper)
                {
                    _mapper = mapper;
                }

                public CustomerRecord Customer(Customer customer) => _mapper.Map<CustomerRecord>(customer);
            }
            """;
        string test = Test("""
                    var customer = new Customer { Id = 7 };
                    var expected = new CustomerRecord { Id = 7 };
                    mapper.Map<CustomerRecord>(customer).Returns(expected);
                    return Equals(new ShopService(mapper).Customer(customer), expected);
            """);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", RecordService), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Test.cs")).ShouldNotContain(".Returns(expected);");
        (await TextAsync(report.Solution, "Service.cs")).ShouldContain("customer.ToCustomerRecord()");
    }

    // The class under test also maps Order, which the mock does not stub (it answers null, the generated method does not): the stub stays.
    [Fact]
    public async Task FixAsync_ClassMapsAPairTheMockDoesNotStub_KeepsStub()
    {
        // Arrange
        string service = Service.Replace("""
                public CustomerDto Customer(Customer customer) => _mapper.Map<CustomerDto>(customer);
            """, """
                public CustomerDto Customer(Customer customer) => _mapper.Map<CustomerDto>(customer);

                public OrderDto Order(Order order) => _mapper.Map<OrderDto>(order);
            """, StringComparison.Ordinal);
        string test = Test("""
                    var customer = new Customer { Id = 7 };
                    var expected = new CustomerDto { Id = 7 };
                    mapper.Map<CustomerDto>(customer).Returns(expected);
                    return new ShopService(mapper).Customer(customer);
            """);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", service), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Test.cs")).ShouldContain(".Returns(expected);");
        Reasons(report).ShouldContain(r => r.Contains("the map Order → OrderDto at Service.cs:", StringComparison.Ordinal) && r.Contains("is not stubbed by this mock", StringComparison.Ordinal),
            string.Join(" | ", Reasons(report)));
    }

    [Fact]
    public async Task FixAsync_MapWithOptions_KeepsStub()
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Customer, CustomerDto>();", "CreateMap<Customer, CustomerDto>().ForMember(d => d.Name, o => o.Ignore());", StringComparison.Ordinal);
        string test = Test("""
                    var customer = new Customer { Id = 7 };
                    var expected = new CustomerDto { Id = 7 };
                    mapper.Map<CustomerDto>(customer).Returns(expected);
                    return new ShopService(mapper).Customer(customer);
            """);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", Service), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Test.cs")).ShouldContain(".Returns(expected);");
        Reasons(report).ShouldContain(r => r.Contains("is not a plain CreateMap<Customer, CustomerDto>()", StringComparison.Ordinal), string.Join(" | ", Reasons(report)));
    }

    // Options on another map of the profile do not touch the stubbed one: its stub still goes.
    [Fact]
    public async Task FixAsync_OptionsOnAnotherMap_RemovesStub()
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Order, OrderDto>();", "CreateMap<Order, OrderDto>().ForMember(d => d.Id, o => o.Ignore());", StringComparison.Ordinal);
        string test = Test("""
                    var customer = new Customer { Id = 7 };
                    var expected = new CustomerDto { Id = 7 };
                    mapper.Map<CustomerDto>(customer).Returns(expected);
                    return new ShopService(mapper).Customer(customer);
            """);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", Service), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Test.cs")).ShouldNotContain(".Returns(expected);");
    }

    // A profile setting may change every map of the profile: the stub stays.
    [Fact]
    public async Task FixAsync_ProfileSetting_KeepsStub()
    {
        // Arrange
        string profile = Profile.Replace("CreateMap<Order, OrderDto>();", "CreateMap<Order, OrderDto>(); AllowNullCollections = true;", StringComparison.Ordinal);
        string test = Test("""
                    var customer = new Customer { Id = 7 };
                    var expected = new CustomerDto { Id = 7 };
                    mapper.Map<CustomerDto>(customer).Returns(expected);
                    return new ShopService(mapper).Customer(customer);
            """);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", profile), ("Service.cs", Service), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report.Solution, "Test.cs")).ShouldContain(".Returns(expected);");
    }

    [Fact]
    public async Task FixAsync_MockWithoutStub_NamesTheTestOfTheClassUnderTest()
    {
        // Arrange
        string test = Test("""
                    return new ShopService(mapper).Customer(new Customer());
            """);
        Solution solution = Create(("Types.cs", Types), ("Profile.cs", Profile), ("Service.cs", Service), ("Test.cs", test));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        Reasons(report).ShouldContain(r => r.Contains("ShopService is a class under test", StringComparison.Ordinal) && r.Contains("(test: Test.cs:10: the mock stubs no map", StringComparison.Ordinal),
            string.Join(" | ", Reasons(report)));
    }

    private static Task<FixReport> FixAsync(Solution solution) =>
        new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

    private static List<string> Reasons(FixReport report) => [.. report.Findings.Select(f => f.Result).OfType<ManualResult>().Select(ReportRow.ReasonOf)];

    private static Solution Create(params (string Name, string Text)[] documents)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution.AddProject(project, "Shop", "Shop", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(project, [.. TestReferences.AutoMapper, s_moq.Value]);
        foreach ((string name, string text) in documents)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(project), name, text, filePath: name);
        }

        return solution;
    }

    private static async Task<string> TextAsync(Solution solution, string name) =>
        (await solution.Projects.SelectMany(p => p.Documents).Single(d => d.Name == name).GetTextAsync(TestContext.Current.CancellationToken)).ToString();
}
