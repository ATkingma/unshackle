using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SampleShop.Data;
using SampleShop.Dtos;
using SampleShop.Features.Orders;

namespace SampleShop.Tests;

public class OrderHandlerTests
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddSampleShop().BuildServiceProvider();

    private ISender Sender => _provider.GetRequiredService<ISender>();

    private static PlaceOrderCommand BookOrder() =>
        new("Timme", [new OrderLineDto { ProductName = "Book", Quantity = 2, UnitPrice = 10m }]);

    [Fact]
    public async Task PlaceOrder_ThenGet_ReturnsDto()
    {
        var id = await Sender.Send(BookOrder(), TestContext.Current.CancellationToken);

        var dto = await Sender.Send(new GetOrderQuery(id), TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.LineCount.Should().Be(1);
        dto.Status.Should().Be("Placed");

        // fa.be-equivalent-to on an object → ShouldBeEquivalentTo (check member-by-member semantics)
        dto.Should().BeEquivalentTo(new OrderDto
        {
            Id = id,
            CustomerName = "Timme",
            Status = "Placed",
            LineCount = 1,
            Total = 20m,
        });
    }

    [Fact]
    public async Task PlaceOrder_WithoutLines_Throws()
    {
        var command = new PlaceOrderCommand("Timme", []);

        // fa.throw-async → await Should.ThrowAsync<ArgumentException>(act)
        Func<Task> act = () => Sender.Send(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one line*");
    }

    [Fact]
    public async Task PlaceOrder_PublishesNotification_AndRunsBehavior()
    {
        await Sender.Send(BookOrder(), TestContext.Current.CancellationToken);

        var log = _provider.GetRequiredService<AuditLog>();
        log.Entries.Should().Contain("Order 1 placed");
        log.Entries.Should().Contain(e => e.StartsWith("Handling"));
    }

    [Fact]
    public async Task ListOrders_ProjectsAll()
    {
        await Sender.Send(BookOrder(), TestContext.Current.CancellationToken);
        await Sender.Send(BookOrder(), TestContext.Current.CancellationToken);

        var orders = await Sender.Send(new ListOrdersQuery(), TestContext.Current.CancellationToken);

        orders.Should().HaveCount(2);
        orders.Should().AllSatisfy(o => o.Total.Should().Be(20m));
    }
}
