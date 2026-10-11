using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SampleShop.Data;
using SampleShop.Domain;
using SampleShop.Dtos;
using SampleShop.Features.Customers;
using SampleShop.Features.Orders;

namespace SampleShop.Advanced.Tests;

// Each test holds one FluentAssertions construction that needs its own pattern (or a Manual report).
public class AdvancedAssertionTests
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddSampleShop().BuildServiceProvider();

    private static Order CreateOrder() => new()
    {
        Id = 1,
        CustomerName = "Timme",
        Lines =
        [
            new OrderLine { ProductName = "Book", Quantity = 2, UnitPrice = 10m },
            new OrderLine { ProductName = "Pen", Quantity = 1, UnitPrice = 5m },
        ],
    };

    private static List<Product> Products() =>
    [
        new("A-1", "Book", new Money(10m, "EUR")),
        new("B-2", "Pen", new Money(2.5m, "EUR")),
    ];

    [Fact]
    public void ChainedAnd_OnString()
    {
        var order = CreateOrder();

        order.CustomerName.Should().StartWith("Ti").And.EndWith("me").And.HaveLength(5);
    }

    [Fact]
    public void ChainedAnd_OnCollection()
    {
        var order = CreateOrder();

        order.Lines.Should().NotBeEmpty().And.HaveCount(2).And.NotContainNulls();
    }

    [Fact]
    public void Which_AfterContainSingle()
    {
        var order = CreateOrder();

        order.Lines.Should().ContainSingle(l => l.ProductName == "Pen")
            .Which.UnitPrice.Should().Be(5m);
    }

    [Fact]
    public void WhoseValue_AfterContainKey()
    {
        var repository = new OrderRepository();
        var id = repository.Add(CreateOrder());

        repository.All.Should().ContainKey(id).WhoseValue.CustomerName.Should().Be("Timme");
    }

    [Fact]
    public void Because_WithArguments()
    {
        var order = CreateOrder();

        order.Total.Should().Be(25m, "two books of {0} and one pen of {1}", 10m, 5m);
        order.Lines.Should().HaveCount(2, because: "the order has {0} lines", 2);
        order.Status.Should().NotBe(OrderStatus.Shipped, "a new order is not shipped yet");
    }

    [Fact]
    public void AssertionScope_CollectsFailures()
    {
        var order = CreateOrder();

        using (new AssertionScope())
        {
            order.CustomerName.Should().Be("Timme");
            order.Lines.Should().HaveCount(2);
            order.Total.Should().BePositive();
        }
    }

    [Fact]
    public void BeEquivalentTo_WithOptions()
    {
        var dto = new OrderDto { Id = 1, CustomerName = "Timme", Status = "Placed", LineCount = 2, Total = 25m };

        dto.Should().BeEquivalentTo(
            new { CustomerName = "Timme", Status = "Placed", LineCount = 2, Total = 25m },
            options => options.ExcludingMissingMembers());
        dto.Should().BeEquivalentTo(
            new OrderDto { Id = 99, CustomerName = "Timme", Status = "Placed", LineCount = 2, Total = 25m },
            options => options.Excluding(d => d.Id));
    }

    [Fact]
    public void BeEquivalentTo_CollectionOfRecords_WithStrictOrdering()
    {
        var products = Products();

        products.Should().BeEquivalentTo(Products(), options => options.WithStrictOrdering());
        products.Should().Equal(Products());
        products.Should().ContainEquivalentOf(new Product("A-1", "Book", new Money(10m, "EUR")));
        products.Should().BeInAscendingOrder(p => p.Sku);
        products.Select(p => p.Price.Currency).Should().AllBe("EUR");
    }

    [Fact]
    public async Task ThrowAsync_WithMessage_AndWhere()
    {
        var sender = _provider.GetRequiredService<ISender>();

        Func<Task> act = () => sender.Send(new RegisterCustomerCommand("Ada", " ", "London"), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Last name*"))
            .Which.ParamName.Should().Be("request");
        await act.Should().ThrowExactlyAsync<ArgumentException>().Where(e => e.Message.Contains("required"));
    }

    [Fact]
    public void Throw_WithInnerAndAnd()
    {
        var order = CreateOrder();
        order.Status = OrderStatus.Shipped;

        order.Invoking(o => o.Cancel())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*shipped*")
            .And.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CustomAssertion_Extension()
    {
        var order = CreateOrder();

        order.Cancel();

        order.Should().BeCancelled("we just cancelled it").And.NotBeNull();
    }

    [Fact]
    public void TypeAndNumberAssertions()
    {
        object dto = new OrderDto { Total = 25.004m };
        var created = new DateTime(2026, 1, 1, 12, 0, 0);

        dto.Should().BeOfType<OrderDto>().Which.Total.Should().BeApproximately(25m, 0.01m);
        created.Should().BeCloseTo(new DateTime(2026, 1, 1, 12, 0, 1), TimeSpan.FromSeconds(2));
        "SO-123".Should().Match("SO-*").And.MatchRegex("^SO-[0-9]+$");
    }
}
