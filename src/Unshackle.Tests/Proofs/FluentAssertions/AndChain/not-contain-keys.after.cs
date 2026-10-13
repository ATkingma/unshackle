using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        x.ShouldNotContainKey("a");
        x.ShouldNotContainKey("b");
    }
}
