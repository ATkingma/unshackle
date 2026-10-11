using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string?> a, List<string?> b)
    {
        a.Should().BeEquivalentTo(b, "any order");
    }
}
