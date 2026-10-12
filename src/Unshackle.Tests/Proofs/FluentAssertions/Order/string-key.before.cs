using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<Item> x)
    {
        x.Should().BeInAscendingOrder(p => p.Sku);
    }
}

public class Item
{
    public string Sku { get; set; } = "";
    public int Qty { get; set; }
}
