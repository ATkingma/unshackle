using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
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
