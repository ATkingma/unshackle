using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> x)
    {
        x.ShouldNotBeEmpty();
        x.ShouldAllBe(s => s.Length > 1);
    }
}
