using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<string> x)
    {
        var found = x.Should().ContainEquivalentOf("a").Which;
    }
}
