using System;
using Shouldly;

namespace Proofs;

public class AndChainCase
{
    public void Test(Order order)
    {
        order.Total.ShouldBeGreaterThan(20m);
        order.Total.ShouldBeLessThan(30m);
        order.Total.ShouldBeGreaterThan(1m, "above");
        order.Total.ShouldBeInRange(2m, 40m, "within");
        order.Total.ShouldBeLessThan(50m);
    }
}

public class Order
{
    public decimal Total { get; set; }
}
