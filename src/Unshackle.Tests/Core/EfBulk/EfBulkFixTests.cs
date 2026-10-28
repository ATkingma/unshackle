using Microsoft.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EfBulk;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EfBulk;

public class EfBulkFixTests
{
    private const string Model = """
        public class Order
        {
            public int Id { get; set; }
            public string Status { get; set; } = "";
            public System.DateTime Changed { get; set; }
        }

        public class ShopContext : DbContext
        {
            public DbSet<Order> Orders => Set<Order>();
        }
        """;

    [Fact]
    public void Stubs_Compile_WithoutErrors()
    {
        // Arrange
        const string code = "using Z.EntityFramework.Plus; using Microsoft.EntityFrameworkCore; class C { }";

        // Act
        IReadOnlyList<Diagnostic> errors = TestSolution.CompileErrors(code, EfBulkReferences.Before);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_SameNameOutsideZ_IsNotFound()
    {
        // Arrange
        string code = $$"""
            using System.Linq;
            using Microsoft.EntityFrameworkCore;
            using LookAlike;

            {{EfBulkReferences.LookAlikeSource}}

            {{Model}}

            public class Cleanup
            {
                public int Run(ShopContext db) => db.Orders.Delete();
            }
            """;

        // Act
        FixReport report = await RunAsync(code, EfBulkReferences.Before);

        // Assert
        report.Findings.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_NoExecuteDeleteInTheProject_IsManual()
    {
        // Arrange: like EF Core 3-6, nothing in the compilation declares ExecuteDelete.
        const string code = """
            using System.Linq;
            using Z.EntityFramework.Plus;

            public class Order { public int Id { get; set; } }

            public class Cleanup
            {
                public int Run(IQueryable<Order> orders) => orders.Delete();
            }
            """;
        IReadOnlyList<MetadataReference> references = [.. TestReferences.Net, EfBulkReferences.Plus, EfBulkReferences.Extensions];

        // Act
        FixReport report = await RunAsync(code, references);

        // Assert
        report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(EfBulkReason.NoNativeExecute));
    }

    [Fact]
    public async Task FixAsync_NoExecuteDeleteInTheProject_AddsOneProjectRow()
    {
        // Arrange: council round 17, B3, two blocked calls give one project-level row next to the per-call rows.
        const string code = """
            using System.Linq;
            using Z.EntityFramework.Plus;

            public class Order { public int Id { get; set; } }

            public class Cleanup
            {
                public int Run(IQueryable<Order> orders) => orders.Delete() + orders.Where(o => o.Id > 1).Delete();
            }
            """;
        Solution solution = TestSolution.Create(code, [.. TestReferences.Net, EfBulkReferences.Plus, EfBulkReferences.Extensions]);
        solution = solution.WithProjectFilePath(solution.ProjectIds.Single(), "Shop.csproj");

        // Act
        FixReport report = await new Fixer([new EfBulkFix()]).FixAsync(solution, new FixRequest { FixId = "efbulk" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Count(f => f.Result is ManualResult { Code: nameof(EfBulkReason.NoNativeExecute) }).ShouldBe(2);
        Finding project = report.Findings.Where(f => f.FilePath == "Shop.csproj").ShouldHaveSingleItem();
        ManualResult row = project.Result.ShouldBeOfType<ManualResult>();
        row.Code.ShouldBe(nameof(EfBulkReason.EfCoreBelow7));
        row.Reason.ShouldContain("EF Core < 7");
        row.Reason.ShouldContain("2 batch call(s) blocked");
    }

    [Fact]
    public async Task FixAsync_ExecuteDeleteAvailable_AddsNoProjectRow()
    {
        // Arrange
        Solution solution = TestSolution.Create(Source("db.Orders.Delete()"), EfBulkReferences.Before);
        solution = solution.WithProjectFilePath(solution.ProjectIds.Single(), "Shop.csproj");

        // Act
        FixReport report = await new Fixer([new EfBulkFix()]).FixAsync(solution, new FixRequest { FixId = "efbulk" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Select(f => (f.Result as ManualResult)?.Code).ShouldNotContain(nameof(EfBulkReason.EfCoreBelow7));
    }

    [Fact]
    public async Task FixAsync_BatchBuilder_ReasonNamesTheOptionsAndTheCall()
    {
        // Arrange: council round 17, B7 and B4.
        string code = Source("db.Orders.Delete(b => { b.BatchSize = 10; b.BatchDelayInterval = 5; })");

        // Act
        FixReport report = await RunAsync(code, EfBulkReferences.Before);

        // Assert
        ManualResult result = report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>();
        result.Code.ShouldBe(nameof(EfBulkReason.BatchOptions));
        result.Reason.ShouldContain("the batch options BatchSize, BatchDelayInterval");
        result.Reason.ShouldContain("'db.Orders.Delete(b => { b.BatchSize = 10; b.BatchDelayInterval = 5; })'");
    }

    [Fact]
    public async Task FixAsync_StaticServerMember_IsManual()
    {
        // Arrange
        string code = Source("db.Orders.Update(o => new Order { Changed = System.DateTime.Now })");

        // Act
        FixReport report = await RunAsync(code, EfBulkReferences.Before);

        // Assert
        report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(EfBulkReason.UpdateValueNotProvable));
    }

    [Fact]
    public async Task FixAsync_AnonymousObjectUpdate_IsManual()
    {
        // Arrange: the real packages bind an anonymous object to the Expression<Func<T, object>> overload.
        string code = Source("db.Orders.Update(o => new { Status = \"done\" })");

        // Act
        FixReport report = await RunAsync(code, EfBulkReferences.Before);

        // Assert
        report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(EfBulkReason.UpdateFactoryShape));
    }

    [Fact]
    public async Task FixAsync_ConditionalValue_IsManual()
    {
        // Arrange
        string code = Source("db.Orders.Update(o => new Order { Status = o.Id > 3 ? \"a\" : \"b\" })");

        // Act
        FixReport report = await RunAsync(code, EfBulkReferences.Before);

        // Assert
        report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(EfBulkReason.UpdateValueNotProvable));
    }

    [Fact]
    public async Task FixAsync_LocalNamedSetters_PicksAFreeName()
    {
        // Arrange
        string code = Source("db.Orders.Update(o => new Order { Status = setters })", "string setters = \"x\";");

        // Act
        FixReport report = await RunAsync(code, EfBulkReferences.Before);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldContain("ExecuteUpdate(setters2 => setters2.SetProperty(o => o.Status, o => setters))");
        TestSolution.CompileErrors(text, EfBulkReferences.Before).ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_FutureKeepsTheUsing_DeleteIsFixed()
    {
        // Arrange
        string code = Source("db.Orders.Delete() + db.Orders.Future().Count()");

        // Act
        FixReport report = await RunAsync(code, EfBulkReferences.Before);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        report.Findings.ShouldContain(f => f.PatternId == "efbulk.delete" && f.Result is FixedResult);
        text.ShouldContain("using Z.EntityFramework.Plus;");
        text.ShouldContain("db.Orders.ExecuteDelete() + db.Orders.Future().Count()");
    }

    [Fact]
    public async Task FixAsync_NoZUsageLeft_RemovesBothZPackages()
    {
        // Arrange: council round 17, B5.
        IProjectFileStore store = Store();

        // Act
        await RunWithStoreAsync(Source("db.Orders.Delete()"), store);

        // Assert
        await store.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => !m.Single().KeepRemovedPackage
                && m.Single().Package.RemovedPackage == "Z.EntityFramework.Plus.EFCore"
                && m.Single().Package.AlsoRemovedPackages.Single() == "Z.EntityFramework.Extensions.EFCore"
                && m.Single().Package.AddedPackages.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_FutureLeft_KeepsThePackageAndReportsTheCallWithItsLine()
    {
        // Arrange
        IProjectFileStore store = Store();

        // Act
        FixReport report = await RunWithStoreAsync(Source("db.Orders.Delete() + db.Orders.Future().Count()"), store);

        // Assert
        await store.Received(1).EditAsync(Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().KeepRemovedPackage), Arg.Any<CancellationToken>());
        Finding future = report.Findings.Single(f => f.PatternId == "efbulk.unsupported");
        future.Line.ShouldBe(22);
        ManualResult result = future.Result.ShouldBeOfType<ManualResult>();
        result.Code.ShouldBe(nameof(EfBulkReason.Unsupported));
        result.Reason.ShouldContain("'db.Orders.Future()'");
        report.Findings.ShouldContain(f => f.FilePath == "Shop.csproj" && f.Result is ManualResult);
    }

    [Fact]
    public async Task FixAsync_BulkInsert_IsManualAndNamesTheMitFork()
    {
        // Arrange
        string code = Source("1", "db.BulkInsert(new System.Collections.Generic.List<Order>());");

        // Act
        FixReport report = await RunAsync(code, EfBulkReferences.Before);

        // Assert
        ManualResult result = report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>();
        result.Code.ShouldBe(nameof(EfBulkReason.Unsupported));
        result.Reason.ShouldContain("EFCore.BulkExtensions.MIT");
    }

    [Fact]
    public async Task FixAsync_ExtensionsOnlyProject_KeptRowNamesExtensions()
    {
        // Arrange: council round 21, B13.
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.PackageReferences("Shop.csproj").Returns(["Microsoft.EntityFrameworkCore", "Z.EntityFramework.Extensions.EFCore"]);

        // Act
        FixReport report = await RunWithStoreAsync(Source("db.Orders.Future().Count()"), Store(), new EfBulkFix(properties));

        // Assert
        string reason = report.Findings.Single(f => f.FilePath == "Shop.csproj").Result.ShouldBeOfType<ManualResult>().Reason;
        reason.ShouldStartWith("Z.EntityFramework.Extensions.EFCore is still used");
        reason.ShouldNotContain("Plus");
    }

    [Fact]
    public async Task FixAsync_ProjectFileUnreadable_KeptRowNamesPlusOrExtensions()
    {
        // Arrange: council round 21, B13.
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.PackageReferences(Arg.Any<string>()).Returns((IReadOnlyList<string>?)null);

        // Act
        FixReport report = await RunWithStoreAsync(Source("db.Orders.Future().Count()"), Store(), new EfBulkFix(properties));

        // Assert
        report.Findings.Single(f => f.FilePath == "Shop.csproj").Result.ShouldBeOfType<ManualResult>().Reason
            .ShouldStartWith("Z.EntityFramework.Plus.EFCore or Z.EntityFramework.Extensions.EFCore is still used");
    }

    [Fact]
    public async Task FixAsync_OtherZUsagesNextToManualRow_OneRowWithCountAndEveryLine()
    {
        // Arrange: council round 21, B14. Audit locals on lines 21-22, the Future call (Manual) on line 23.
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.PackageReferences("Shop.csproj").Returns(["Z.EntityFramework.Plus.EFCore"]);
        string code = Source("db.Orders.Future().Count()", "Audit? first = null;\n        Audit? second = first;");

        // Act
        FixReport report = await RunWithStoreAsync(code, Store(), new EfBulkFix(properties));

        // Assert
        report.Findings.ShouldContain(f => f.PatternId == "efbulk.unsupported" && f.Line == 23);
        Finding other = report.Findings.Where(f => f.PatternId == Fixer.UnmatchedPatternId).ShouldHaveSingleItem();
        other.Line.ShouldBe(21);
        other.Result.ShouldBeOfType<ManualResult>().Reason.ShouldStartWith("2 other usages of Z.EntityFramework.Plus.EFCore that no pattern migrates: lines 21, 22.");
    }

    [Fact]
    public async Task FixAsync_ReferencedProjectGetsZOnlyTransitively_NoKeptRow()
    {
        // Arrange: TAAK-356 (morenote): Logic -> Config -> Models; only Models declares Z, Config gets it transitively.
        IProjectFileStore store = Store();
        List<ProjectMigration> edited = [];
        await store.EditAsync(Arg.Do<IEnumerable<ProjectMigration>>(m => edited.AddRange(m)), Arg.Any<CancellationToken>());
        var models = ProjectId.CreateNewId();
        var config = ProjectId.CreateNewId();
        var logic = ProjectId.CreateNewId();
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(Project(models, "Models"))
            .AddProject(Project(config, "Config").WithProjectReferences([new ProjectReference(models)]))
            .AddProject(Project(logic, "Logic").WithProjectReferences([new ProjectReference(config)]))
            .AddDocument(DocumentId.CreateNewId(models), "Marker.cs", "public class Marker { }", filePath: "/repo/Models/Model.cs")
            .AddDocument(DocumentId.CreateNewId(config), "Settings.cs", "public class Settings { }", filePath: "/repo/Config/Settings.cs")
            .AddDocument(DocumentId.CreateNewId(logic), "Cleanup.cs", Source("db.Orders.Future().Count()"), filePath: "/repo/Logic/Cleanup.cs");

        // Act
        FixReport report = await new Fixer([new EfBulkFix()], store).FixAsync(solution, new FixRequest { FixId = "efbulk" }, TestContext.Current.CancellationToken);

        // Assert
        edited.Single(m => m.ProjectPath == "/repo/Models/Models.csproj").KeepRemovedPackage.ShouldBeTrue();
        edited.Single(m => m.ProjectPath == "/repo/Config/Config.csproj").KeepRemovedPackage.ShouldBeFalse();
        report.Findings.ShouldNotContain(f => f.FilePath == "/repo/Config/Config.csproj");
    }

    private static ProjectInfo Project(ProjectId id, string name) =>
        ProjectInfo.Create(id, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: $"/repo/{name}/{name}.csproj")
            .WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(EfBulkReferences.Before);

    private static IProjectFileStore Store()
    {
        IProjectFileStore store = Substitute.For<IProjectFileStore>();
        store.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([]);
        return store;
    }

    private static Task<FixReport> RunWithStoreAsync(string code, IProjectFileStore store, EfBulkFix? fix = null)
    {
        Solution solution = TestSolution.Create(code, EfBulkReferences.Before);
        solution = solution.WithProjectFilePath(solution.ProjectIds.Single(), "Shop.csproj");
        return new Fixer([fix ?? new EfBulkFix()], store).FixAsync(solution, new FixRequest { FixId = "efbulk" }, TestContext.Current.CancellationToken);
    }

    private static string Source(string call, string local = "") => $$"""
        using System.Linq;
        using Microsoft.EntityFrameworkCore;
        using Z.EntityFramework.Plus;

        {{Model}}

        public class Cleanup
        {
            public int Run(ShopContext db)
            {
                {{local}}
                return {{call}};
            }
        }
        """;

    private static Task<FixReport> RunAsync(string code, IEnumerable<MetadataReference> references) =>
        new Fixer([new EfBulkFix()]).FixAsync(
            TestSolution.Create(code, references), new FixRequest { FixId = "efbulk" }, TestContext.Current.CancellationToken);
}
