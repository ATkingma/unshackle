using System;
using FluentAssertions;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(int x)
    {
        var c = x.Should().BeGreaterThan(1);
    }
}
