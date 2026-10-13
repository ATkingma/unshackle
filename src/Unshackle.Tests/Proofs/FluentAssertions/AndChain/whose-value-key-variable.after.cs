using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x, string key)
    {
        x.ShouldContainKey(key);
        x[key].ShouldBe(1);
    }
}
