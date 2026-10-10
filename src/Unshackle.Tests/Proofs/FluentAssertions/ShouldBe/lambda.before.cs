using System;
using FluentAssertions;

namespace Proofs;

public class Lambda
{
    public void Test(decimal[] totals)
    {
        Array.ForEach(totals, t => t.Should().Be(20m));
    }
}
