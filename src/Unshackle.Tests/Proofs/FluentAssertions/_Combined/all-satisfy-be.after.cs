using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<Order> orders)
    {
        orders.ShouldNotBeEmpty();
        foreach (var o in orders)
        {
            o.Total.ShouldBe(20m);
        }
    }
}

public class Order
{
    public decimal Total { get; set; }
}
