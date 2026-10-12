using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(List<string> a, string[] b)
    {
        a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());
    }
}
