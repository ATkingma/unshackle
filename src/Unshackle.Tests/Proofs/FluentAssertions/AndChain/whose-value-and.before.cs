using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        x.Should().ContainKey("a").WhoseValue.Should().BeGreaterThan(0).And.BeLessThan(10);
    }
}
