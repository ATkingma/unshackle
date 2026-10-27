using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.AutoMapper;

// TAAK-325 (council round 13, A12): the selector that automapper.generic-destination writes is chosen in C# before Select is called, so EF Core
// sees the same expression tree as for the closed ProjectTo rewrite. The helper below is the shape of Proofs/AutoMapper/GenericDestination/paging-helper.after.cs.
public class GenericDestinationTranslationTests
{
    [Fact]
    public void ToQueryString_GenericSelector_EqualsClosedProjection()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();

        // Act
        string closedDto = context.Orders.Skip(10).Take(10).Select(OrderMapping.OrderToOrderDtoProjection).ToQueryString();
        string genericDto = Page<Order, OrderDto>(context.Orders).ToQueryString();
        string closedSummary = context.Orders.Skip(10).Take(10).Select(OrderMapping.OrderToOrderSummaryProjection).ToQueryString();
        string genericSummary = Page<Order, OrderSummary>(context.Orders).ToQueryString();

        // Assert
        genericDto.ShouldBe(closedDto);
        genericSummary.ShouldBe(closedSummary);
        closedSummary.ShouldNotBe(closedDto);
        Page<Order, OrderDto>(context.Orders).ToList().ShouldBeEmpty();
    }

    [Fact]
    public void Page_TypeOutsideClosedSet_Throws()
    {
        // Arrange
        IQueryable<Order> orders = Array.Empty<Order>().AsQueryable();

        // Act
        Action act = () => Page<Order, Order>(orders);

        // Assert
        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("No generated projection");
    }

    // The emitted dispatch, as automapper.generic-destination writes it for the closed set {(Order, OrderDto), (Order, OrderSummary)}.
    private static IQueryable<TResult> Page<T, TResult>(IQueryable<T> query) =>
        query
            .Skip(10)
            .Take(10)
            .Select(typeof(T) == typeof(Order) && typeof(TResult) == typeof(OrderDto) ? (Expression<Func<T, TResult>>)(object)OrderMapping.OrderToOrderDtoProjection
                : typeof(T) == typeof(Order) && typeof(TResult) == typeof(OrderSummary) ? (Expression<Func<T, TResult>>)(object)OrderMapping.OrderToOrderSummaryProjection
                : throw new InvalidOperationException($"No generated projection for {typeof(T)}, {typeof(TResult)}."));

    public class Order
    {
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    public class OrderSummary
    {
        public int Id { get; set; }
    }

    private static class OrderMapping
    {
        public static Expression<Func<Order, OrderDto>> OrderToOrderDtoProjection => source => new OrderDto { Id = source.Id, Name = source.Name };

        public static Expression<Func<Order, OrderSummary>> OrderToOrderSummaryProjection => source => new OrderSummary { Id = source.Id };
    }

    private class ShopContext : DbContext
    {
        public ShopContext(DbContextOptions<ShopContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();
    }
}
