using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        var v = x.Should().ContainKey("a").WhoseValue;
    }
}
