using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(Dictionary<string, List<int>> map)
    {
        map.Should().ContainKey("a");
    }
}
