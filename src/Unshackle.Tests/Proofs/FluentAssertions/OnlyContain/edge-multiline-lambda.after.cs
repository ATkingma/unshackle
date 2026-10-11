using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<string> names)
    {
        names.ShouldNotBeEmpty();
        names.ShouldAllBe(n =>
            n.Length > 0
            && n != "x");
    }
}
