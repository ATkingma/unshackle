using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Proofs;

public enum OrderStatus
{
    Open,
    Cancelled,
}

public class Order
{
    public OrderStatus Status { get; set; }

    public int Total { get; set; }
}

// Custom assertion extension: user code built on FluentAssertions' extension model.
public static class OrderAssertionExtensions
{
    public static OrderAssertions Should(this Order instance) => new(instance);
}

public class OrderAssertions : ReferenceTypeAssertions<Order, OrderAssertions>
{
    public OrderAssertions(Order instance)
        : base(instance)
    {
    }

    protected override string Identifier => "order";

    public AndConstraint<OrderAssertions> BeCancelled(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Status == OrderStatus.Cancelled)
            .FailWith("Expected {context:order} to be cancelled{reason}, but it is {0}.", Subject.Status);

        return new AndConstraint<OrderAssertions>(this);
    }
}

public class CustomAssertionClass
{
    public void Test(Order order)
    {
        order.Should().BeCancelled("we just cancelled it").And.NotBeNull();
        order.Total.Should().Be(0);
    }
}
