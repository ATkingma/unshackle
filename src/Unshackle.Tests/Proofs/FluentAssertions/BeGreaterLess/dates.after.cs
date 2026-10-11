using Shouldly;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(DateTime a, DateTime b, DateTimeOffset c, DateTimeOffset d)
    {
        a.ShouldBeGreaterThan(b);
        a.ShouldBeGreaterThanOrEqualTo(b);
        a.ShouldBeLessThan(b);
        c.ShouldBeLessThanOrEqualTo(d, "deadline");
    }
}
