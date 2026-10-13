using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IReadOnlyDictionary<string, int> x)
    {
        x.ContainsKey("a").ShouldBeTrue();
        x["a"].ShouldBe(1);
    }
}
