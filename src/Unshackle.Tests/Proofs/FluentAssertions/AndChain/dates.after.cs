using Shouldly;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(DateTime a, DateTime b, DateTime c)
    {
        a.ShouldBeGreaterThan(b);
        a.ShouldBeLessThan(c);
    }
}
