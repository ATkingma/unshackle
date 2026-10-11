using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(Dictionary<string, List<int>> map)
    {
        map.ShouldContainKey("a");
    }
}
