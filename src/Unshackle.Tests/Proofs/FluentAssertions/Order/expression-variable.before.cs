using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq.Expressions;

namespace Proofs;

public class Case
{
    public void Test(List<Item> x)
    {
        Expression<Func<Item, int>> key = p => p.Qty;
        x.Should().BeInAscendingOrder(key);
    }
}

public class Item
{
    public string Sku { get; set; } = "";
    public int Qty { get; set; }
}
