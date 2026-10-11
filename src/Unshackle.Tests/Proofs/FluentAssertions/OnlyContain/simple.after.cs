using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldNotBeEmpty();
        x.ShouldAllBe(i => i > 0);
    }
}
