using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public sealed record Item(int Id, List<int> Tags);

public class Case
{
    public void Test(List<Item> x, Item item)
    {
        x.Should().ContainEquivalentOf(item);
    }
}
