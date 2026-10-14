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

// Forms a single-file proof cannot show: nothing found, a profile from a package, a map in another project.
public class ConfigurationTestPatternTests
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

    // The mapper goes to a service (kavita, ombi): it keeps working with the emptied profiles, so nothing is reported.
    [Theory]
    [InlineData("var mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>()).CreateMapper(); Use(mapper);")]
    [InlineData("var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(OrderProfile).Assembly)); Use(config.CreateMapper());")]
    [InlineData("_mapper = _injected;")]
    public void Find_MapperPassedOn_FindsNothing(string statements)
    {
        // Arrange
        string code = Profile + $$"""

            public class OrderServiceTests
            {
                private readonly IMapper _injected = null!;
                private IMapper _mapper = null!;

                public void Arrange()
                {
                    {{statements}}
                }

                private static void Use(object service)
                {
                }

                public OrderDto Act(Order order) => _mapper.Map<OrderDto>(order);
            }
            """;
        (SyntaxNode root, SemanticModel model) = Compile(code, TestReferences.AutoMapper);

        // Act
        List<SyntaxNode> found = [.. new ConfigurationTestPattern().Find(root, model)];

        // Assert
        found.ShouldBeEmpty();
    }

    [Fact]
    public void Rewrite_ProfileFromReferencedAssembly_ReportsManual()
    {
        // Arrange
        using var image = new MemoryStream();
        CSharpCompilation.Create("Vendor", [CSharpSyntaxTree.ParseText("namespace Vendor; public class VendorProfile : AutoMapper.Profile { }", cancellationToken: TestContext.Current.CancellationToken)],
                TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .Emit(image, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        const string code = """
            using AutoMapper;

            public class VendorTests
            {
                public void Check()
                {
                    var config = new MapperConfiguration(cfg => cfg.AddProfile<Vendor.VendorProfile>());
                    config.AssertConfigurationIsValid();
                }
            }
            """;
        (SyntaxNode root, SemanticModel model) = Compile(code, [.. TestReferences.AutoMapper, MetadataReference.CreateFromImage(image.ToArray())]);
        var pattern = new ConfigurationTestPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert: the configuration stays (Manual, which blocks the all-or-nothing fix); its check alone would go.
        results.OfType<ManualResult>().ShouldHaveSingleItem().Reason.ShouldContain("it adds VendorProfile from a referenced assembly");
    }

    // The test project checks the profiles of the project it references: a CreateMap there outside a profile constructor stays AutoMapper.
    [Theory]
    [InlineData("CreateMap<Order, OrderDto>();", null)]
    [InlineData("this.AddOrderMaps();", "the map at Profile.cs:")]
    public async Task FixAsync_MapInReferencedProject_ChecksBeforeRemoving(string profileBody, string? manual)
    {
        // Arrange
        string profile = manual is null ? Profile : Profile.Replace("CreateMap<Order, OrderDto>();", profileBody, StringComparison.Ordinal) + """

            public static class OrderMaps
            {
                public static void AddOrderMaps(this IProfileExpression maps)
                {
                    maps.CreateMap<Order, OrderDto>();
                }
            }
            """;
        const string tests = """
            using AutoMapper;

            namespace Shop.Tests;

            public class MappingTests
            {
                public void ConfigurationIsValid()
                {
                    var config = new MapperConfiguration(cfg => cfg.AddProfile<Shop.Mapping.OrderProfile>());
                    config.AssertConfigurationIsValid();
                }
            }
            """;
        var workspace = new AdhocWorkspace();
        var mapping = ProjectId.CreateNewId();
        var app = ProjectId.CreateNewId();
        Solution solution = AddProject(AddProject(workspace.CurrentSolution, mapping, "Mapping"), app, "Tests")
            .AddProjectReference(app, new ProjectReference(mapping))
            .AddDocument(DocumentId.CreateNewId(mapping), "Profile.cs", profile, filePath: "Profile.cs")
            .AddDocument(DocumentId.CreateNewId(app), "Tests.cs", tests, filePath: "Tests.cs");

        // Act
        FixReport report = await new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        // Two findings: the local (no check in it) and the check; not a test method, so they go one by one.
        List<Finding> findings = [.. report.Findings.Where(f => f.PatternId == "automapper.configuration-test")];
        findings.Count.ShouldBe(2);
        if (manual is null)
        {
            findings.ShouldAllBe(f => f.Result is FixedResult);
        }
        else
        {
            // The local goes too, but the all-or-nothing fix leaves the whole solution as it was.
            findings[1].Result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(manual);
        }
    }

    // An AssertConfigurationIsValid that is not a statement of its own (here the body of a non-test method) stays Manual.
    [Fact]
    public void Rewrite_AssertAsExpressionBody_ReportsManual()
    {
        // Arrange
        string code = Profile + """

            public class Checks
            {
                private readonly IConfigurationProvider _configuration = null!;

                public void Validate() => _configuration.AssertConfigurationIsValid();
            }
            """;
        (SyntaxNode root, SemanticModel model) = Compile(code, TestReferences.AutoMapper);
        var pattern = new ConfigurationTestPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("is not a statement of its own");
    }

    private static (SyntaxNode Root, SemanticModel Model) Compile(string code, IEnumerable<MetadataReference> references)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("Check", [tree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return (tree.GetRoot(TestContext.Current.CancellationToken), compilation.GetSemanticModel(tree));
    }

    private static Solution AddProject(Solution solution, ProjectId id, string name) =>
        solution.AddProject(id, name, name, LanguageNames.CSharp)
            .WithProjectCompilationOptions(id, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(id, TestReferences.AutoMapper);
}
