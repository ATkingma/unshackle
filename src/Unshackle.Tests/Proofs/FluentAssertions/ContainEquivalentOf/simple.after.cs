using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldContain(2);
        x.ShouldNotContain(5);
    }
}
