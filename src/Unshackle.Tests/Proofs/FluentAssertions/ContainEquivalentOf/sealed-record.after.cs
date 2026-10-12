using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public sealed record Item(int Id, string Name, DateTime At);

public class Case
{
    public void Test(List<Item> x, Item item)
    {
        x.ShouldContain(item);
        x.ShouldNotContain(new Item(1, "a", DateTime.MinValue));
    }
}
