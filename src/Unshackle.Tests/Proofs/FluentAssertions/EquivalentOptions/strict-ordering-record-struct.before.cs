using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public record struct Pair(int Id, string Name);

public class Case
{
    public void Test(List<Pair> a, Pair[] b)
    {
        a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());
    }
}
