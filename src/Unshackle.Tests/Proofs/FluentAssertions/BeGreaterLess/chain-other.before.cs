using System;
using FluentAssertions;

namespace Proofs;

public class OtherCase
{
    private int Next() => 1;

    public void Test(int x)
    {
        x.Should().BeGreaterThan(1).And.NotBe(3);
    }
}
