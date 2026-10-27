using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Generics;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.AutoMapper;

// automapper.generic-map (TAAK-324, council round 13 A11): a Map on a type parameter is closed over its callers, or Manual GenericMapOpenSet.
public class GenericMapPatternTests
{
    private const string ProjectFile = "Shop.csproj";

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
        using AutoMapper;

        namespace Shop;

        ACCESS class OrderReader
        {
            private readonly IMapper _mapper;

            public OrderReader(IMapper mapper)
            {
                _mapper = mapper;
            }

            public MODIFIER TResult Read<TResult>(Order order) => _mapper.Map<TResult>(order);
        }
        """;

    private const string Callers = """
        namespace Shop;

        internal class Endpoints
        {
            public OrderDto Details(OrderReader reader, Order order) => reader.Read<OrderDto>(order);

            public OrderSummary Summary(OrderReader reader, Order order) => reader.Read<OrderSummary>(order);
        }
        """;

    [Fact]
    public async Task FixAsync_ProfileAndTwoCallers_DispatchesToGeneratedMethods()
    {
        // Arrange
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", "internal").Replace("MODIFIER ", ""), Callers);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, Reasons(report));
        report.Findings.ShouldContain(f => f.PatternId == "automapper.generic-map");
        string reader = await TextAsync(report.Solution, "Reader.cs");
        reader.ShouldContain("typeof(TResult) == typeof(OrderDto) ? (TResult)(object)order.ToOrderDto()");
        reader.ShouldContain(": typeof(TResult) == typeof(OrderSummary) ? (TResult)(object)order.ToOrderSummary()");
        reader.ShouldContain("throw new global::System.InvalidOperationException(");
    }

    [Fact]
    public async Task FixAsync_PublicMethodInExecutable_IsNotReachableFromOutside()
    {
        // Arrange
        Solution solution = Single(OutputKind.ConsoleApplication, Types, Reader.Replace("ACCESS", "public").Replace("MODIFIER ", ""), Callers + "\ninternal static class Program { private static void Main() { } }");

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, Reasons(report));
    }

    [Theory]
    [InlineData("public", "", "is public in a packable project")]
    [InlineData("internal", "virtual ", "virtual, an override or an interface member")]
    public async Task FixAsync_CallersNotAllKnown_IsOpenSet(string access, string modifier, string reason)
    {
        // Arrange
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", access).Replace("MODIFIER ", modifier), Callers);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        Finding finding = report.Findings.Single(f => f.PatternId == "automapper.generic-map");
        ManualResult manual = finding.Result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe("GenericMapOpenSet");
        manual.Reason.ShouldContain(reason);
    }

    // TAAK-324: with the project properties (Cli wires XmlProjectProperties) a public generic Map<T> in a library with IsPackable=false is closed,
    // where the guess from the compilation keeps it open; a packable library stays open.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FixAsync_PublicMethodInLibraryWithProjectProperties_FollowsIsPackable(bool packable)
    {
        // Arrange
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", "public").Replace("MODIFIER ", ""), Callers);
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.IsPackable(ProjectFile).Returns(packable);

        // Act
        FixReport guessed = await FixAsync(solution);
        FixReport report = await new Fixer([new AutoMapperFix(properties)]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        guessed.Findings.Single(f => f.PatternId == "automapper.generic-map").Result.ShouldBeOfType<ManualResult>().Code.ShouldBe("GenericMapOpenSet");
        Finding finding = report.Findings.Single(f => f.PatternId == "automapper.generic-map");
        if (packable)
        {
            ManualResult manual = finding.Result.ShouldBeOfType<ManualResult>();
            manual.Code.ShouldBe("GenericMapOpenSet");
            manual.Reason.ShouldContain("is public in a packable project");
            // A13 (council round 15): the reason names the one-line fix in the project file.
            manual.Reason.ShouldContain("Set `<IsPackable>false</IsPackable>` in Shop.csproj if this library is not published");
        }
        else
        {
            report.Findings.ShouldAllBe(f => f.Result is FixedResult, Reasons(report));
            (await TextAsync(report.Solution, "Reader.cs")).ShouldContain("typeof(TResult) == typeof(OrderDto) ? (TResult)(object)order.ToOrderDto()");
        }
    }

    [Theory]
    [InlineData("Func<Order, OrderDto> read = reader.Read<OrderDto>;", "used as a method group")]
    [InlineData("dynamic r = reader; r.Read<OrderDto>(order);", "called dynamically")]
    [InlineData("""[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Elsewhere")]""", "InternalsVisibleTo names Elsewhere")]
    public async Task FixAsync_UnprovableCaller_NamesIt(string code, string reason)
    {
        // Arrange
        string extra = code.StartsWith('[')
            ? code
            : $$"""
                using System;

                namespace Shop;

                internal class Other
                {
                    public void Run(OrderReader reader, Order order)
                    {
                        {{code}}
                    }
                }
                """;
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", "internal").Replace("MODIFIER ", ""), Callers, extra);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        ManualResult manual = report.Findings.Single(f => f.PatternId == "automapper.generic-map").Result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe("GenericMapOpenSet");
        manual.Reason.ShouldContain(reason);
    }

    [Fact]
    public async Task FixAsync_PairWithoutMap_IsOpenSet()
    {
        // Arrange
        const string Unmapped = """
            namespace Shop;

            public class Invoice
            {
            }

            internal class Billing
            {
                public Invoice Bill(OrderReader reader, Order order) => reader.Read<Invoice>(order);
            }
            """;
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", "internal").Replace("MODIFIER ", ""), Callers, Unmapped);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        ManualResult manual = report.Findings.Single(f => f.PatternId == "automapper.generic-map").Result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe("GenericMapOpenSet");
        manual.Reason.ShouldContain("Order → Invoice");
    }

    // Four generic levels between the Map call and the closing caller: more than the depth limit of 3.
    [Fact]
    public async Task FixAsync_ChainDeeperThanLimit_IsOpenSet()
    {
        // Arrange
        const string Chain = """
            namespace Shop;

            internal class Chain
            {
                public T A<T>(OrderReader reader, Order order) => reader.Read<T>(order);

                public T B<T>(OrderReader reader, Order order) => A<T>(reader, order);

                public T C<T>(OrderReader reader, Order order) => B<T>(reader, order);

                public OrderDto D(OrderReader reader, Order order) => C<OrderDto>(reader, order);
            }
            """;
        Solution solution = Single(OutputKind.DynamicallyLinkedLibrary, Types, Reader.Replace("ACCESS", "internal").Replace("MODIFIER ", ""), Chain);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        ManualResult manual = report.Findings.Single(f => f.PatternId == "automapper.generic-map").Result.ShouldBeOfType<ManualResult>();
        manual.Reason.ShouldContain("more than 3 levels of generic code");
    }

    // The closed type lives in a project the generic method's project does not see.
    [Fact]
    public async Task FixAsync_ClosedTypeInDependentProject_IsOpenSet()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var lib = ProjectId.CreateNewId();
        var app = ProjectId.CreateNewId();
        const string AppCode = """
            namespace App;

            public class AppDto
            {
            }

            internal class Use
            {
                public AppDto Get(Shop.OrderReader reader, Shop.Order order) => reader.Read<AppDto>(order);
            }
            """;
        Solution solution = AddProject(AddProject(workspace.CurrentSolution, lib, "Lib", OutputKind.DynamicallyLinkedLibrary), app, "App", OutputKind.ConsoleApplication)
            .AddProjectReference(app, new ProjectReference(lib))
            .AddDocument(DocumentId.CreateNewId(lib), "Types.cs", Types, filePath: "Types.cs")
            .AddDocument(DocumentId.CreateNewId(lib), "Reader.cs", Reader.Replace("ACCESS", "internal").Replace("MODIFIER ", ""), filePath: "Reader.cs")
            .AddDocument(DocumentId.CreateNewId(lib), "Friend.cs", """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("App")]""", filePath: "Friend.cs")
            .AddDocument(DocumentId.CreateNewId(app), "Use.cs", AppCode + "\ninternal static class Program { private static void Main() { } }", filePath: "Use.cs");

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        ManualResult manual = report.Findings.Single(f => f.PatternId == "automapper.generic-map").Result.ShouldBeOfType<ManualResult>();
        manual.Reason.ShouldContain("not a plain type visible from Lib");
    }

    [Fact]
    public void Find_GenericCall_IsNotMapCallPatterns()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Types + "\n" + Reader.Replace("ACCESS", "internal").Replace("MODIFIER ", "").Replace("using AutoMapper;\n\nnamespace Shop;", "", StringComparison.Ordinal), cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        List<SyntaxNode> generic = [.. new GenericMapPattern().Find(root, model)];
        List<SyntaxNode> plain = [.. new MapCallPattern().Find(root, model)];

        // Assert
        generic.ShouldHaveSingleItem().ShouldBeOfType<InvocationExpressionSyntax>().ToString().ShouldBe("_mapper.Map<TResult>(order)");
        plain.ShouldBeEmpty();
    }

    // The shared closure (TypeArgumentClosure) with a class type parameter: every written use, base classes included.
    [Fact]
    public void Close_TypeParameterOfClass_CollectsUsesAndDerivedTypes()
    {
        // Arrange
        const string Code = """
            internal class Repo<T> { }

            internal class OrderRepo : Repo<int> { }

            internal class Holder<U>
            {
                private Repo<U>? _repo;
            }

            internal static class Uses
            {
                public static Repo<string> Make() => new();

                public static Holder<long>? Hold() => null;
            }
            """;
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Code, cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        INamedTypeSymbol repo = compilation.GetTypeByMetadataName("Repo`1")!;

        // Act
        ClosedSets closed = new TypeArgumentClosure([compilation]).Close([repo.TypeParameters[0]]);

        // Assert
        closed.Unresolved.ShouldBeNull();
        closed.Sets.Select(s => s[repo.TypeParameters[0]].ToDisplayString()).Order(StringComparer.Ordinal).ShouldBe(["int", "long", "string"]);
    }

    private static Solution Single(OutputKind kind, params string[] files)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        Solution solution = AddProject(workspace.CurrentSolution, project, "Shop", kind);
        string[] names = ["Types.cs", "Reader.cs", "Callers.cs", "Extra.cs"];
        for (int i = 0; i < files.Length; i++)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(project), names[i], files[i], filePath: names[i]);
        }

        return solution;
    }

    private static Solution AddProject(Solution solution, ProjectId id, string name, OutputKind kind) =>
        solution.AddProject(ProjectInfo.Create(id, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: name == "Shop" ? ProjectFile : null))
            .WithProjectCompilationOptions(id, new CSharpCompilationOptions(kind))
            .WithProjectParseOptions(id, new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReferences(id, TestReferences.AutoMapper);

    private static Task<FixReport> FixAsync(Solution solution) =>
        new Fixer([new AutoMapperFix()]).FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

    private static string Reasons(FixReport report) =>
        string.Join(" | ", report.Findings.Select(f => f.PatternId + ": " + (f.Result is ManualResult m ? m.Reason : f.Result.GetType().Name)));

    private static async Task<string> TextAsync(Solution solution, string name) =>
        (await solution.Projects.SelectMany(p => p.Documents).Single(d => d.Name == name).GetTextAsync(TestContext.Current.CancellationToken)).ToString();
}
