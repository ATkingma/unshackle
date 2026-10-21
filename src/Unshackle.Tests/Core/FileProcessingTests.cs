using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// TAAK-298: the pre-filter and parallel file processing change nothing in the output. Each case runs the Fixer three ways
// (sequential on every file, sequential with the pre-filter, the default) and compares findings in order and every file's text.
public class FileProcessingTests
{
    private const string FluentGlobalUsing = "global using FluentAssertions;\n";

    private const string FluentWithoutUsing = """
        public class OrderTests
        {
            public void Total() => 5.Should().Be(5);
        }

        """;

    private const string FluentUnusedUsing = """
        using FluentAssertions;

        public class Plain
        {
            public int Value => 1;
        }

        """;

    // A helper of the user's own that returns an assertion of the old library; the caller names only the helper.
    private const string FluentHelper = """
        using FluentAssertions;
        using FluentAssertions.Numeric;

        public static class Check
        {
            public static NumericAssertions<int> That(int value) => value.Should();
        }

        """;

    private const string FluentHelperCaller = """
        public class HelperTests
        {
            public void Uses() => Check.That(1).Be(1);
        }

        """;

    private const string Unrelated = """
        public class Unrelated
        {
            public string Name() => nameof(Unrelated);
        }

        """;

    private const string MediatRRequest = """
        using MediatR;

        public class Ping : IRequest<Unit>
        {
        }

        """;

    private const string MediatRBase = """
        using System.Threading;
        using System.Threading.Tasks;
        using MediatR;

        public abstract class BaseHandler : IRequestHandler<Ping, Unit>
        {
            public abstract Task<Unit> Handle(Ping request, CancellationToken cancellationToken);
        }

        """;

    private const string MediatRDerived = """
        using System.Threading;
        using System.Threading.Tasks;

        public class PingHandler : BaseHandler
        {
            public override Task<Unit> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult(default(Unit));
        }

        """;

    private const string AutoMapperProfile = """
        using AutoMapper;

        public class OrderProfile : Profile
        {
            public OrderProfile()
            {
                CreateMap<Order, OrderDto>();
            }
        }

        """;

    private const string AutoMapperModels = """
        public class Order
        {
            public int Id { get; set; }
        }

        public class OrderDto
        {
            public int Id { get; set; }
        }

        """;

    private const string AutoMapperCaller = """
        using AutoMapper;

        public class OrderService
        {
            private readonly IMapper _mapper;

            public OrderService(IMapper mapper)
            {
                _mapper = mapper;
            }

            public OrderDto Get(Order order) => _mapper.Map<OrderDto>(order);
        }

        """;

    // Code that only a later target framework sees: the second pass replaces the inactive-branch row of the first (TAAK-223).
    private const string FluentMultiTarget = """
        using FluentAssertions;

        public class Branches
        {
            public void A()
            {
                1.Should().Be(1);
        #if NETFRAMEWORK
                2.Should().Be(2);
        #else
                3.Should().Be(3);
        #endif
            }
        }

        """;

    public static TheoryData<string> Cases() => ["fluentassertions", "mediatr", "automapper", "many", "multitarget"];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task FixAsync_EveryWayOfProcessingFiles_GivesTheSameReport(string name)
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (IFix fix, IReadOnlyList<MetadataReference> references, IReadOnlyList<(string Path, string Code)> files) = Case(name);
        var fixer = new Fixer([fix]);

        // Act
        string reference = await RunAsync(fixer, fix, references, files, new FixRequest { FixId = fix.Id, Sequential = true, CheckEveryFile = true }, cancellationToken);
        string filtered = await RunAsync(fixer, fix, references, files, new FixRequest { FixId = fix.Id, Sequential = true }, cancellationToken);
        string parallelEveryFile = await RunAsync(fixer, fix, references, files, new FixRequest { FixId = fix.Id, CheckEveryFile = true }, cancellationToken);
        List<string> parallel = [];
        foreach (int degree in new[] { 2, 4, 8, 3, 0 })
        {
            parallel.Add(await RunAsync(fixer, fix, references, files, new FixRequest { FixId = fix.Id, MaxDegree = degree == 0 ? null : degree }, cancellationToken));
        }

        // Assert
        reference.ShouldContain("Fixed");
        filtered.ShouldBe(reference);
        parallelEveryFile.ShouldBe(reference);
        parallel.ShouldAllBe(p => p == reference);
    }

    [Fact]
    public async Task FixAsync_FilesFinishInShuffledOrder_RowsStayInFileOrder()
    {
        // Arrange: each file sleeps a random time in Find, so the parallel files finish in another order than they start.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.slow");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(call =>
        {
            Thread.Sleep(Random.Shared.Next(0, 30));
            return call.Arg<SyntaxNode>().DescendantNodes().OfType<InvocationExpressionSyntax>();
        });
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(call => PatternResult.Manual(call.Arg<SyntaxNode>().ToString()));
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns([]);
        fix.AddedNamespaces.Returns([]);
        fix.FixesFilesInParallel.Returns(true);
        List<(string Path, string Code)> files =
            [.. Enumerable.Range(0, 30).Select(i => ($"/repo/F{i:D2}.cs", $"public class F{i}\n{{\n    public void M() {{ A{i}(); B{i}(); }}\n\n    private static void A{i}() {{ }}\n\n    private static void B{i}() {{ }}\n}}\n"))];
        var fixer = new Fixer([fix]);
        string sequential = await RunAsync(fixer, fix, TestReferences.Net, files, new FixRequest { FixId = "test", Sequential = true }, cancellationToken);

        // Act
        List<string> parallel = [];
        for (int run = 0; run < 5; run++)
        {
            parallel.Add(await RunAsync(fixer, fix, TestReferences.Net, files, new FixRequest { FixId = "test", MaxDegree = 2 + (2 * run) }, cancellationToken));
        }

        // Assert
        sequential.ShouldContain("/repo/F29.cs:3:test.slow:Manual:None:B29()");
        parallel.ShouldAllBe(p => p == sequential);
    }

    [Fact]
    public async Task FixAsync_GlobalUsingUnusedUsingAndHelper_StillFixedWithThePreFilter()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (IFix fix, IReadOnlyList<MetadataReference> references, IReadOnlyList<(string Path, string Code)> files) = Case("fluentassertions");

        // Act
        string report = await RunAsync(new Fixer([fix]), fix, references, files, new FixRequest { FixId = fix.Id }, cancellationToken);

        // Assert
        report.ShouldContain("5.ShouldBe(5)");
        report.ShouldContain("Check.That(1).Be(1)");
        report.ShouldContain("/repo/HelperTests.cs:3:");
        report.ShouldNotContain("using FluentAssertions;\n\npublic class Plain");
        report.ShouldContain("--- /repo/Unrelated.cs\n" + Unrelated);
    }

    private static (IFix Fix, IReadOnlyList<MetadataReference> References, IReadOnlyList<(string Path, string Code)> Files) Case(string name) => name switch
    {
        "fluentassertions" => (new FluentAssertionsFix(TestReferences.ShouldlyAssembly), TestReferences.FluentAssertions,
        [
            ("/repo/GlobalUsings.cs", FluentGlobalUsing), ("/repo/OrderTests.cs", FluentWithoutUsing), ("/repo/Plain.cs", FluentUnusedUsing),
            ("/repo/Check.cs", FluentHelper), ("/repo/HelperTests.cs", FluentHelperCaller), ("/repo/Unrelated.cs", Unrelated),
        ]),
        "mediatr" => (new MediatRFix(), TestReferences.MediatR,
            [("/repo/Ping.cs", MediatRRequest), ("/repo/BaseHandler.cs", MediatRBase), ("/repo/PingHandler.cs", MediatRDerived), ("/repo/Unrelated.cs", Unrelated)]),
        "automapper" => (new AutoMapperFix(), TestReferences.AutoMapper,
            [("/repo/OrderProfile.cs", AutoMapperProfile), ("/repo/Models.cs", AutoMapperModels), ("/repo/OrderService.cs", AutoMapperCaller), ("/repo/Unrelated.cs", Unrelated)]),
        "multitarget" => (new FluentAssertionsFix(TestReferences.ShouldlyAssembly), TestReferences.FluentAssertions,
            [("/repo/Branches.cs", FluentMultiTarget), ("/repo/OrderTests.cs", "using FluentAssertions;\n\n" + FluentWithoutUsing), ("/repo/Unrelated.cs", Unrelated)]),
        // Many files, so the parallel run interleaves them.
        _ => (new FluentAssertionsFix(TestReferences.ShouldlyAssembly), TestReferences.FluentAssertions,
            [.. Enumerable.Range(0, 40).Select(i => ($"/repo/T{i:D2}.cs", i % 3 == 0
                ? $"public class U{i} {{ }}\n"
                : $"using FluentAssertions;\n\npublic class T{i}\n{{\n    public void A() => {i}.Should().Be({i});\n\n    public void B() => \"x\".Should().Contain(\"x\");\n}}\n"))]),
    };

    // The report as text: every finding in order, then every file.
    private static async Task<string> RunAsync(
        Fixer fixer, IFix fix, IReadOnlyList<MetadataReference> references, IReadOnlyList<(string Path, string Code)> files, FixRequest request, CancellationToken cancellationToken)
    {
        // Two target frameworks when a file has #if code: a multi-target project is loaded once per framework.
        Solution solution = TestSolution.NewEmpty();
        string[][] targets = files.Any(f => f.Code.Contains("#if", StringComparison.Ordinal)) ? [["NETFRAMEWORK"], []] : [[]];
        foreach (string[] symbols in targets)
        {
            var projectId = ProjectId.CreateNewId();
            solution = solution
                .AddProject(projectId, $"App({symbols.Length})", "App", LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithProjectParseOptions(projectId, new CSharpParseOptions(preprocessorSymbols: symbols))
                .AddMetadataReferences(projectId, references);
            foreach ((string path, string code) in files)
            {
                solution = solution.AddDocument(DocumentId.CreateNewId(projectId), Path.GetFileName(path), code, filePath: path);
            }
        }

        FixReport report = await fixer.FixAsync(solution, request, cancellationToken);
        List<string> lines = [.. report.Findings.Select(f => $"{f.FilePath}:{f.Line}:{f.PatternId}:" + f.Result switch
        {
            FixedResult fixedResult => $"Fixed:{fixedResult.Warning}:{fixedResult.Info}",
            ManualResult manual => $"Manual:{manual.Kept}:{manual.Reason}",
            _ => f.Result.GetType().Name,
        })];
        foreach (Document document in report.Solution.Projects.SelectMany(p => p.Documents).OrderBy(d => d.FilePath, StringComparer.Ordinal).ThenBy(d => d.Project.Name, StringComparer.Ordinal))
        {
            lines.Add($"--- {document.FilePath}\n{await document.GetTextAsync(cancellationToken)}");
        }

        return string.Join("\n", lines);
    }
}
