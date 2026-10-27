using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.EfBulk;

// TAAK-342 step 6 (council round 17, B1/B4): the shapes efbulk writes run on EF Core 10 + SQLite in memory as one set-based statement whose
// SQL is the one a Z batch call sends (DELETE/UPDATE ... WHERE <the query's filter>, captured values as parameters, entity members as
// columns). The Z packages themselves cannot run here (paid Extensions trial), so the old side is the documented Z behaviour: one
// statement, no entities loaded or tracked.
public class EfBulkSqlTests
{
    [Fact]
    public async Task ExecuteDelete_WhereFilter_IsOneDeleteStatement()
    {
        // Arrange
        await using Shop shop = await Shop.CreateAsync(TestContext.Current.CancellationToken);

        // Act
        int deleted = await shop.Context.Orders.Where(o => o.Id > 2).ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        // Assert
        deleted.ShouldBe(1);
        shop.Statements.ShouldHaveSingleItem().ShouldBe("""
            DELETE FROM "Orders" AS "o"
            WHERE "o"."Id" > 2
            """.ReplaceLineEndings());
        (await shop.Context.Orders.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteUpdate_CapturedLocalAndEntityArithmetic_IsOneUpdateStatement()
    {
        // Arrange: the shape of Proofs/EfBulk/Update/plus-initializer.after.cs.
        await using Shop shop = await Shop.CreateAsync(TestContext.Current.CancellationToken);
        string status = "done";

        // Act
        int updated = await shop.Context.Orders.Where(o => o.Id > 1)
            .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Status, o => status).SetProperty(o => o.Retries, o => o.Retries * 2 + 1), TestContext.Current.CancellationToken);

        // Assert
        updated.ShouldBe(2);
        string sql = shop.Statements.ShouldHaveSingleItem();
        sql.ShouldStartWith("UPDATE \"Orders\" AS \"o\"");
        sql.ShouldContain("\"Status\" = @");
        sql.ShouldContain("\"Retries\" = \"o\".\"Retries\" * 2 + 1");
        sql.ShouldContain("WHERE \"o\".\"Id\" > 1");
        List<Order> orders = await shop.Context.Orders.AsNoTracking().OrderBy(o => o.Id).ToListAsync(TestContext.Current.CancellationToken);
        orders.Select(o => (o.Status, o.Retries)).ShouldBe([("new", 1), ("done", 5), ("done", 7)]);
    }

    [Fact]
    public async Task ExecuteUpdate_EnumAndNegation_TranslateAsColumnsAndParameters()
    {
        // Arrange
        await using Shop shop = await Shop.CreateAsync(TestContext.Current.CancellationToken);
        bool archived = true;

        // Act
        await shop.Context.Orders.ExecuteUpdateAsync(
            setters => setters.SetProperty(o => o.Kind, o => OrderKind.Return).SetProperty(o => o.Open, o => !archived).SetProperty(o => o.Retries, o => -o.Retries),
            TestContext.Current.CancellationToken);

        // Assert
        shop.Statements.ShouldHaveSingleItem().ShouldNotContain("SELECT");
        List<Order> orders = await shop.Context.Orders.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        orders.ShouldAllBe(o => o.Kind == OrderKind.Return && !o.Open && o.Retries <= 0);
    }

    [Fact]
    public async Task ExecuteUpdate_DecimalArithmetic_FollowsTheProvider()
    {
        // Arrange: council round 17, B4 asks for a decimal rounding/overflow proof before decimal arithmetic is Fixed. SQLite has no decimal
        // type: EF Core stores TEXT and evaluates arithmetic in its own ef_* functions with .NET decimal semantics, so this proves nothing
        // about SQL Server's decimal(18,2) rounding or arithmetic overflow, where the result follows the column type.
        await using Shop shop = await Shop.CreateAsync(TestContext.Current.CancellationToken);
        decimal huge = decimal.MaxValue;

        // Act
        await shop.Context.Orders.ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Total, o => o.Total / 3m), TestContext.Current.CancellationToken);
        Func<Task> overflow = () => shop.Context.Orders.ExecuteUpdateAsync(
            setters => setters.SetProperty(o => o.Total, o => o.Total * huge), TestContext.Current.CancellationToken);

        // Assert: provider-specific results (.NET decimal on SQLite, an exception on overflow), so efbulk keeps decimal arithmetic Manual.
        shop.Statements.First().ShouldContain("ef_divide");
        decimal total = (await shop.Context.Orders.AsNoTracking().OrderBy(o => o.Id).FirstAsync(TestContext.Current.CancellationToken)).Total;
        total.ShouldBe(10m / 3m);
        await overflow.ShouldThrowAsync<Exception>();
    }

    public enum OrderKind
    {
        Sale,
        Return,
    }

    public class Order
    {
        public int Id { get; set; }

        public string Status { get; set; } = "";

        public int Retries { get; set; }

        public decimal Total { get; set; }

        public OrderKind Kind { get; set; }

        public bool Open { get; set; }
    }

    private class ShopContext : DbContext
    {
        public ShopContext(DbContextOptions<ShopContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();
    }

    // Collects the SQL of every executed non-query command (ExecuteDelete/ExecuteUpdate), so a test can see that nothing was loaded first.
    private class StatementLog : DbCommandInterceptor
    {
        public List<string> Statements { get; } = [];

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Statements.Add(command.CommandText.ReplaceLineEndings());
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<System.Data.Common.DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Statements.Add(command.CommandText.ReplaceLineEndings());
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private class Shop : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly StatementLog _log;

        private Shop(SqliteConnection connection, StatementLog log, ShopContext context)
        {
            _connection = connection;
            _log = log;
            Context = context;
        }

        public ShopContext Context { get; }

        public List<string> Statements => _log.Statements;

        public static async Task<Shop> CreateAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync(cancellationToken);
            var log = new StatementLog();
            var context = new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlite(connection).AddInterceptors(log).Options);
            await context.Database.EnsureCreatedAsync(cancellationToken);
            context.Orders.AddRange(
                new Order { Id = 1, Status = "new", Retries = 1, Total = 10m },
                new Order { Id = 2, Status = "new", Retries = 2, Total = 20m },
                new Order { Id = 3, Status = "new", Retries = 3, Total = 30m });
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            log.Statements.Clear();
            return new Shop(connection, log, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
