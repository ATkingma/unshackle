using System;
using Shouldly;

namespace Proofs;

public class Lambda
{
    public void Test(decimal[] totals)
    {
        Array.ForEach(totals, t => t.ShouldBe(20m));
    }
}
