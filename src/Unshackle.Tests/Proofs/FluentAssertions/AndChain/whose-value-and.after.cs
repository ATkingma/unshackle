using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        x.ShouldContainKey("a");
        x["a"].ShouldBeGreaterThan(0);
        x["a"].ShouldBeLessThan(10);
    }
}
