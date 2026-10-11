using System;
using FluentAssertions;

namespace Proofs;

public class ChainSubjectCase
{
    private int Next() => 1;

    public void Test(int x)
    {
        Next().Should().BeGreaterThan(0).And.BeLessThan(5);
    }
}
