using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

// Moq corpus round 1 (claude/tasks/CORPUSRUN-moq.md B2-B7): the whole fix, and the result must compile.
public class CorpusRegressionTests
{
    private static async Task<(string Text, FixReport Report)> FixAsync(string code)
    {
        var fixer = new Fixer([new MoqFix(MoqReferences.NSubstituteAssembly)]);
        Solution solution = TestSolution.Create(code, MoqReferences.Moq);
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "moq" }, TestContext.Current.CancellationToken);
        return (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken), report);
    }

    private static IEnumerable<string> Errors(string text) =>
        TestSolution.CompileErrors(text, [.. MoqReferences.Moq, .. MoqReferences.NSubstituteAssembly]).Select(d => d.ToString());

    [Fact]
    public async Task Fixer_MockFromOutArgument_ManualAndCompiles()
    {
        // Arrange: B4 (jellyfin SubtitleControllerTests)
        const string code = """
            using Moq;
            public interface IFiles { bool Exists(string path); }
            public class T
            {
                private static object Create(out Mock<IFiles> files) { files = new Mock<IFiles>(); return files.Object; }

                public void Test()
                {
                    object sut = Create(out var files);
                    files.Setup(f => f.Exists("a")).Returns(true);
                }
            }
            """;

        // Act
        (string text, FixReport report) = await FixAsync(code);

        // Assert
        report.Findings.Where(f => f.Line == 10).ShouldAllBe(f => f.Result is ManualResult && ((ManualResult)f.Result).Code == nameof(MoqReason.MockPassedAround));
        text.ShouldContain("files.Setup(f => f.Exists(\"a\")).Returns(true);");
        Errors(text).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fixer_MockFromDeconstruction_ManualAndCompiles()
    {
        // Arrange: B5 (orchardcore MediaGalleryCapabilitiesFilterTests)
        const string code = """
            using Moq;
            public interface INotifier { void Add(string message); }
            public class T
            {
                private static (Mock<INotifier> Notifier, object Filter) Create() { var notifier = new Mock<INotifier>(); return (notifier, notifier.Object); }

                public void Test()
                {
                    var (notifier, filter) = Create();
                    notifier.Verify(n => n.Add(It.IsAny<string>()), Times.Once);
                }
            }
            """;

        // Act
        (string text, _) = await FixAsync(code);

        // Assert
        text.ShouldContain("notifier.Verify(n => n.Add(It.IsAny<string>()), Times.Once);");
        Errors(text).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fixer_NullNextToMatcher_PredicateSpecAndCompiles()
    {
        // Arrange: B7 (orchardcore ContentQuickNavigationTests)
        const string code = """
            using Moq;
            public interface IAuth { bool Allowed(string user, object resource, int level); }
            public class T
            {
                public void Test()
                {
                    var auth = new Mock<IAuth>();
                    auth.Setup(a => a.Allowed(It.IsAny<string>(), null, default)).Returns(true);
                    var sut = auth.Object;
                }
            }
            """;

        // Act
        (string text, _) = await FixAsync(code);

        // Assert
        text.ShouldContain("auth.Allowed(Arg.Any<string>(), Arg.Is<object>(a => a == null), Arg.Is<int>(default(int))).Returns(true);");
        Errors(text).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fixer_ManualOnMockFromElsewhere_RemovesUnusedMoqUsing()
    {
        // Arrange: B3 (radarr EventAggregatorFixture): the Setup on a Mock<T> from a helper stays, it needs no `using Moq;`.
        const string code = """
            using Moq;
            public interface IFactory { int Build(); }
            public class Mocker { public global::Moq.Mock<T> GetMock<T>() where T : class => null!; }
            public class T
            {
                private readonly Mocker _mocker = new Mocker();

                public void Test()
                {
                    _mocker.GetMock<IFactory>().Setup(f => f.Build()).Returns(1);
                }
            }
            """;

        // Act
        (string text, _) = await FixAsync(code);

        // Assert
        text.ShouldNotContain("using Moq;");
        Errors(text).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fixer_PinnedTypeShadowedByOwnNamespace_GlobalQualifiedAndCompiles()
    {
        // Arrange: B6 (orchardcore DataMigrationManagerTests): `YesSql.Indexes.IIndexProvider` would bind to `Data.YesSql` here.
        const string code = """
            using Moq;
            namespace YesSql.Indexes { public interface IIndexProvider { } }
            namespace Data.YesSql { public class Store { } }
            namespace Api { public interface IStore { string Find(global::YesSql.Indexes.IIndexProvider provider); } }
            namespace Data.Migration.Tests
            {
                public class T
                {
                    public object Test()
                    {
                        var store = new Mock<Api.IStore>();
                        return store.Object;
                    }
                }
            }
            """;

        // Act
        (string text, _) = await FixAsync(code);

        // Assert
        text.ShouldContain("Substitute.For<");
        Errors(text).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fixer_DependentStillUsesMoqThroughHelperProject_HelperKeepsPackage()
    {
        // Arrange: B2 (ocelot): Unit gets Moq only through its reference to Testing, and still uses it (a Mock<T> parameter).
        IProjectFileStore projectFiles = Substitute.For<IProjectFileStore>();
        projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([]);
        var fixer = new Fixer([new MoqFix(MoqReferences.NSubstituteAssembly)], projectFiles);
        var testing = ProjectId.CreateNewId();
        var unit = ProjectId.CreateNewId();
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(Project(testing, "Testing"))
            .AddProject(Project(unit, "Unit").WithProjectReferences([new ProjectReference(testing)]))
            .AddDocument(DocumentId.CreateNewId(testing), "Helper.cs", "public interface IClock { int Now(); }\npublic static class Helper { }", filePath: "/repo/Testing/Helper.cs")
            .AddDocument(DocumentId.CreateNewId(unit), "Test.cs", "using Moq;\npublic class T { public void Use(Mock<IClock> clock) { } }", filePath: "/repo/Unit/Test.cs");
        List<ProjectMigration> edited = [];
        await projectFiles.EditAsync(Arg.Do<IEnumerable<ProjectMigration>>(m => edited.AddRange(m)), Arg.Any<CancellationToken>());

        // Act
        await fixer.FixAsync(solution, new FixRequest { FixId = "moq" }, TestContext.Current.CancellationToken);

        // Assert
        edited.Single(m => m.ProjectPath == "/repo/Testing/Testing.csproj").KeepRemovedPackage.ShouldBeTrue();
        edited.Single(m => m.ProjectPath == "/repo/Unit/Unit.csproj").KeepRemovedPackage.ShouldBeTrue();
    }

    private static ProjectInfo Project(ProjectId id, string name) =>
        ProjectInfo.Create(id, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: $"/repo/{name}/{name}.csproj")
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(MoqReferences.Moq);
}
