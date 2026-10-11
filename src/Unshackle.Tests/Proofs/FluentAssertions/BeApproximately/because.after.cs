using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(double d)
    {
        d.ShouldBe(1.5, 0.01, "rounding");
    }
}
