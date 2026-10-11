#nullable enable
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(double? d)
    {
        d.Should().NotBeApproximately(1.5, 0.01);
    }
}
