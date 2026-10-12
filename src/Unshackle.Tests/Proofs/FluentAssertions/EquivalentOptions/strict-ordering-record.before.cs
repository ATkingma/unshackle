using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public sealed record Item(int Id, string Name, double Score, DateTime At);

public class Case
{
    public void Test(List<Item> a, Item[] b)
    {
        a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());
    }
}
