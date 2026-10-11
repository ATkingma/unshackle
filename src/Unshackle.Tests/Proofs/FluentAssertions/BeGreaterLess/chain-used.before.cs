using System;
using FluentAssertions;

namespace Proofs;

public class ChainUsedCase
{
    private int Next() => 1;

    public void Test(int x)
    {
        var c = x.Should().BeGreaterThan(1);
    }
}
