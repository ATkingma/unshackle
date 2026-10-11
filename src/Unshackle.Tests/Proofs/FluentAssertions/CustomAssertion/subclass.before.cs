using FluentAssertions;
using FluentAssertions.Primitives;

namespace Proofs;

public class Order;

public class OrderAssertions : ReferenceTypeAssertions<Order, OrderAssertions>
{
    public OrderAssertions(Order subject)
        : base(subject)
    {
    }

    protected override string Identifier => "order";

    public AndConstraint<OrderAssertions> BeShipped() => new(this);
}

public class Subclass
{
    public void Test(Order order)
    {
        new OrderAssertions(order).BeShipped();
    }
}
