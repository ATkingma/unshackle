using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(double d, float f, decimal m)
    {
        d.Should().BeApproximately(1.5, 0.01);
        f.Should().BeApproximately(1.5f, 0.01f);
        m.Should().BeApproximately(25m, 0.01m);
    }
}
