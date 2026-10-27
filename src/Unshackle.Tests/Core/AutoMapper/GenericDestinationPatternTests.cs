using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.AutoMapper;

// automapper.generic-destination (TAAK-325, council round 13 A12): ProjectTo<T> on a type parameter selects a closed projection by typeof before
// the query is built, or is Manual GenericProjectionOpenSet.
public class GenericDestinationPatternTests
{
    private const string Types = """
        using AutoMapper;

        namespace Shop;

        public class Order
        {
            public int Id { get; set; }
        }

        public class OrderDto
        {
            public int Id { get; set; }
        }

        public class OrderSummary
        {
            public int Id { get; set; }
        }

        public class OrderProfile : Profile
        {
            public OrderProfile()
            {
                CreateMap<Order, OrderDto>();
                CreateMap<Order, OrderSummary>();
            }
        }
        """;

    private const string Reader = """
        using System.Linq;
        using AutoMapper;
        using AutoMapper.QueryableExtensions;

        namespace Shop;

        ACCESS class OrderReader
        {
            private readonly IMapper _mapper;

            public OrderReader(IMapper mapper)
            {
                _mapper = mapper;
            }

            public IQueryable<TResult> Query<TResult>(IQueryable<Order> orders) => orders.ProjectTo<TResult>(_mapper.ConfigurationProvider);
        }
        """;

    private const string Callers = """
        using System.Linq;

        namespace Shop;

        internal class Endpoints
        {
            public IQueryable<OrderDto> Details(OrderReader reader, IQueryable<Order> orders) => reader.Query<OrderDto>(orders);

            public IQueryable<OrderSummary> Summaries(OrderReader reader, IQueryable<Order> orders) => reader.Query<OrderSummary>(orders);
        }
        """;

    // Non-generic ProjectTo calls of both pairs: the profile writes their projections, so each member of the closed set has a Fixed projection.
    private const string PlainProjections = """
        using System.Linq;
        using AutoMapper;
        using AutoMapper.QueryableExtensions;

        namespace Shop;

        internal class Plain
        {
            public IQueryable<OrderDto> A(IQueryable<Order> orders, IConfigurationProvider configuration) => orders.ProjectTo<OrderDto>(configuration);

            public IQueryable<OrderSummary> B(IQueryable<Order> orders, IConfigurationProvider configuration) => orders.ProjectTo<OrderSummary>(configuration);
        }
        """;

    [Fact]
    public async Task FixAsync_ProfileWithPlainProjections_SelectsGeneratedProjectionByType()
    {
        // Arrange
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", "internal"), Callers, PlainProjections);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, Reasons(report));
        report.Findings.ShouldContain(f => f.PatternId == "automapper.generic-destination");
        string reader = await TextAsync(report.Solution, "Reader.cs");
        reader.ShouldContain("orders.Select(typeof(TResult) == typeof(OrderDto) ? (Expression<Func<Order, TResult>>)(object)OrderProfileExtensions.OrderToOrderDtoProjection");
        reader.ShouldContain(": typeof(TResult) == typeof(OrderSummary) ? (Expression<Func<Order, TResult>>)(object)OrderProfileExtensions.OrderToOrderSummaryProjection");
        reader.ShouldContain("using System.Linq.Expressions;");
    }

    [Fact]
    public async Task FixAsync_PairWithoutPlainProjectTo_IsOpenSet()
    {
        // Arrange: only OrderDto has a ProjectTo of its own; the profile writes no projection for OrderSummary.
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", "internal"), Callers,
            PlainProjections.Replace("orders.ProjectTo<OrderSummary>(configuration)", "Enumerable.Empty<OrderSummary>().AsQueryable()", StringComparison.Ordinal));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        ManualResult manual = report.Findings.Single(f => f.PatternId == "automapper.generic-destination").Result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe("GenericProjectionOpenSet");
        manual.Reason.ShouldContain("Order → OrderSummary");
        manual.Reason.ShouldContain("no ProjectTo<OrderSummary> on a query of Order with a type of its own");
    }

    [Fact]
    public async Task FixAsync_PublicInPackableLibrary_IsOpenSet()
    {
        // Arrange
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", "public"), Callers, PlainProjections);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        ManualResult manual = report.Findings.Single(f => f.PatternId == "automapper.generic-destination").Result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe("GenericProjectionOpenSet");
        manual.Reason.ShouldContain("is public in a packable project");
    }

    [Fact]
    public void Find_GenericProjectTo_IsNotProjectToPatterns()
    {
        // Arrange
        SyntaxTree types = CSharpSyntaxTree.ParseText(Types, cancellationToken: TestContext.Current.CancellationToken);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Reader.Replace("ACCESS", "internal") + "\n" + PlainProjections[PlainProjections.IndexOf("internal class", StringComparison.Ordinal)..],
            cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("Test", [types, tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        List<SyntaxNode> generic = [.. new GenericDestinationPattern().Find(root, model)];
        List<SyntaxNode> plain = [.. new ProjectToPattern().Find(root, model)];

        // Assert
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        generic.ShouldHaveSingleItem().ShouldBeOfType<InvocationExpressionSyntax>().ToString().ShouldBe("orders.ProjectTo<TResult>(_mapper.ConfigurationProvider)");
        plain.Count.ShouldBe(2);
    }

    private static Solution Single(OutputKind kind, params string[] files)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(project, VersionStamp.Default, "Shop", "Shop", LanguageNames.CSharp, filePath: "Shop.csproj"))
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(kind))
            .WithProjectParseOptions(project, new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReferences(project, TestReferences.AutoMapper);
        string[] names = ["Types.cs", "Reader.cs", "Callers.cs", "Extra.cs"];
        for (int i = 0; i < files.Length; i++)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(project), names[i], files[i], filePath: names[i]);
        }

        return solution;
    }

    private static Task<FixReport> FixAsync(Solution solution) =>
        new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

    private static string Reasons(FixReport report) =>
        string.Join(" | ", report.Findings.Select(f => f.PatternId + ": " + (f.Result is ManualResult m ? m.Reason : f.Result.GetType().Name)));

    private static async Task<string> TextAsync(Solution solution, string name) =>
        (await solution.Projects.SelectMany(p => p.Documents).Single(d => d.Name == name).GetTextAsync(TestContext.Current.CancellationToken)).ToString();
}
