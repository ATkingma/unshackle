using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<Order> orders)
    {
        orders.Should().AllSatisfy(o => o.Total.Should().Be(20m));
    }
}

public class Order
{
    public decimal Total { get; set; }
}
