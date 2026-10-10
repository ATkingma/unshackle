using SampleShop.Domain;

namespace SampleShop.Tests;

public class OrderTests
{
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

    [Fact]
    public void Total_SumsAllLines()
    {
        var order = CreateOrder();

        // fa.should-be → order.Total.ShouldBe(25m)
        order.Total.Should().Be(25m);
    }

    [Fact]
    public void Lines_OnList_HasCount()
    {
        var order = CreateOrder();

        // fa.have-count on List<T> → order.Lines.Count.ShouldBe(2)
        order.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void Lines_OnEnumerable_HasCount()
    {
        var order = CreateOrder();

        // fa.have-count on IEnumerable<T> → ....Count().ShouldBe(1)
        order.Lines.Where(l => l.Quantity > 1).Should().HaveCount(1);
    }

    [Fact]
    public void CustomerName_IgnoresCase()
    {
        var order = CreateOrder();

        // fa.be-equivalent-to on string → ShouldBe("TIMME", StringCompareShould.IgnoreCase)
        order.CustomerName.Should().BeEquivalentTo("TIMME");
    }

    [Fact]
    public void Cancel_PlacedOrder_SetsStatus()
    {
        var order = CreateOrder();

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
        (order.Status == OrderStatus.Cancelled).Should().BeTrue();
    }

    [Fact]
    public void Cancel_ShippedOrder_Throws()
    {
        var order = CreateOrder();
        order.Status = OrderStatus.Shipped;

        // fa.throw → Should.Throw<InvalidOperationException>(act)
        Action act = () => order.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("*shipped*");
    }

    [Fact]
    public void Lines_ContainBook_AndPricesArePositive()
    {
        var order = CreateOrder();

        order.Lines.Select(l => l.ProductName).Should().Contain("Book");
        order.Lines.Should().NotBeEmpty();
        order.Lines.Should().OnlyContain(l => l.UnitPrice > 0);
        order.Total.Should().BeGreaterThan(20m).And.BeLessThan(30m);
    }

    [Fact]
    public void NewOrder_HasNoLines()
    {
        var order = new Order();

        order.Lines.Should().BeEmpty();
        order.CustomerName.Should().BeEmpty();
    }
}
