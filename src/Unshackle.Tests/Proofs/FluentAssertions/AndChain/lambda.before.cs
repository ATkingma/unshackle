using System;
using FluentAssertions;

namespace Proofs;

public class ChainLambdaCase
{
    private int Next() => 1;

    public void Test(int x)
    {
        Action a = () => x.Should().BeGreaterThan(1).And.BeLessThan(5);
    }
}
