using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IDictionary<string, int> x)
    {
        x.ShouldContainKey("a");
    }
}
