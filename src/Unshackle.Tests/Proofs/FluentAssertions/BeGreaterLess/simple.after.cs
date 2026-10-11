using System;
using Shouldly;

namespace Proofs;

public class SimpleCase
{
    public void Test(int x, decimal d, double f, long l)
    {
        x.ShouldBeGreaterThan(1);
        x.ShouldBeGreaterThanOrEqualTo(2);
        d.ShouldBeLessThan(3m);
        f.ShouldBeLessThanOrEqualTo(4.5);
        l.ShouldBeGreaterThanOrEqualTo(5);
        l.ShouldBeLessThanOrEqualTo(6);
    }
}
