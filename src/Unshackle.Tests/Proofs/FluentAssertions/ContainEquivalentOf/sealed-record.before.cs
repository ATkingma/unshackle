using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public sealed record Item(int Id, string Name, DateTime At);

public class Case
{
    public void Test(List<Item> x, Item item)
    {
        x.Should().ContainEquivalentOf(item);
        x.Should().NotContainEquivalentOf(new Item(1, "a", DateTime.MinValue));
    }
}
