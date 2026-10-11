using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(double d, float f, decimal m, double a, double b)
    {
        d.Should().NotBeApproximately(1.5, 0.01);
        f.Should().NotBeApproximately(1.5f, 0.01f, "far");
        (a + b).Should().NotBeApproximately(a - b, 0.1);
        m.Should().NotBeApproximately(25m, 0.01m);
    }
}
