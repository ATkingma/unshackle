using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<Item> x)
    {
        x.Select(p => p.Sku).ShouldBeInOrder(SortDirection.Ascending, System.StringComparer.Ordinal);
    }
}

public class Item
{
    public string Sku { get; set; } = "";
    public int Qty { get; set; }
}
