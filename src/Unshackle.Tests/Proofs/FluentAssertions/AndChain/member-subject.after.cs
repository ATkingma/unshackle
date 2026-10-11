using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        _order.Lines.ShouldNotBeEmpty();
        _order.Lines.Count.ShouldBe(1);
    }

    private readonly Order _order = new();
}

public class Order
{
    public List<int> Lines { get; } = [];
}
