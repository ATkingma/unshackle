using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(double d, float f, decimal m)
    {
        d.ShouldBe(1.5, 0.01);
        f.ShouldBe(1.5f, 0.01f);
        m.ShouldBe(25m, 0.01m);
    }
}
