using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(int? x)
    {
        x.Should().NotBeNull().And.BeGreaterThan(1);
    }
}
