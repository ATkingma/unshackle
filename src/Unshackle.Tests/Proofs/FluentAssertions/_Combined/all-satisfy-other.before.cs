using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<Order> orders)
    {
        orders.Should().AllSatisfy(o =>
        {
            o.Note.Should().NotBeNull();
            o.Tags.Should().Contain("new");
        });
    }
}

public class Order
{
    public string? Note { get; set; }

    public List<string> Tags { get; set; } = new();
}
