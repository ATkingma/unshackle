using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        x.ShouldContainKey("a", "it is there");
        x["a"].ShouldBe(1, "it is one");
    }
}
