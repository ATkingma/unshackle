using System;
using FluentAssertions;

namespace Proofs;

public class NoBlockCase
{
    private int Next() => 1;

    public void Test(int x)
    {
        if (x > 0)
            x.Should().BeGreaterThan(1).And.BeLessThan(5);
    }
}
