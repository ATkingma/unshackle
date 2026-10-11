using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Order order, List<int> numbers, Action act)
    {
        order.Id.ShouldBe(1);
        order.Total.ShouldBeGreaterThan(10m);
        order.Total.ShouldBeLessThan(30m);
        numbers.ShouldNotBeEmpty();
        numbers.ShouldAllBe(i => i > 0);
        Should.Throw<InvalidOperationException>(act).Message.ShouldBe("shipped", Shouldly.StringCompareShould.IgnoreCase);
        order.Note.ShouldBeNull();
    }
}

public class Order
{
    public int Id { get; set; }

    public decimal Total { get; set; }

    public string? Note { get; set; }
}
