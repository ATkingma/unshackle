#nullable enable
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(double? d, DateTime? t, DateTime u)
    {
        d.Should().BeApproximately(1.5, 0.01);
        t.Should().BeCloseTo(u, TimeSpan.FromSeconds(1));
    }
}
