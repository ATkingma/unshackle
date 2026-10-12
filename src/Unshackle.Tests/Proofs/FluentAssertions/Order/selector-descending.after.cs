using System;
using System.Collections.Generic;
using Shouldly;
using System.Linq;

namespace Proofs;

public class Case
{
    public void Test(List<Item> x)
    {
        x.Select(p => p.Qty).ShouldBeInOrder(SortDirection.Descending);
    }
}

public class Item
{
    public string Sku { get; set; } = "";
    public int Qty { get; set; }
}
