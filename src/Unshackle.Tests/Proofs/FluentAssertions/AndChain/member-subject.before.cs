using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        _order.Lines.Should().NotBeEmpty().And.HaveCount(1);
    }

    private readonly Order _order = new();
}

public class Order
{
    public List<int> Lines { get; } = [];
}
