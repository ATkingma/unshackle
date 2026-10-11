using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Order order, List<int> numbers, Action act)
    {
        order.Id.Should().Be(1);
        order.Total.Should().BeGreaterThan(10m).And.BeLessThan(30m);
        numbers.Should().OnlyContain(i => i > 0);
        act.Should().Throw<InvalidOperationException>().WithMessage("shipped");
        order.Note.Should().BeNull();
    }
}

public class Order
{
    public int Id { get; set; }

    public decimal Total { get; set; }

    public string? Note { get; set; }
}
