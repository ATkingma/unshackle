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
            o.Note.ShouldNotBeNull();
            o.Tags.ShouldContain("new");
        }
    }
}

public class Order
{
    public string? Note { get; set; }

    public List<string> Tags { get; set; } = new();
}
