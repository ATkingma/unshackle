using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IReadOnlyDictionary<int, string> x)
    {
        x.ShouldContainKey(1);
    }
}
