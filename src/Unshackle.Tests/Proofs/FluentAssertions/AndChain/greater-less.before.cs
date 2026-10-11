using System;
using FluentAssertions;

namespace Proofs;

public class AndChainCase
{
    public void Test(Order order)
    {
        order.Total.Should().BeGreaterThan(20m).And.BeLessThan(30m);
        order.Total.Should().BeGreaterThan(1m, "above").And.BeInRange(2m, 40m, "within").And.BeLessThan(50m);
    }
}

public class Order
{
    public decimal Total { get; set; }
}
