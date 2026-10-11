using System;
using FluentAssertions;

namespace Proofs;

public class InRangeCase
{
    public void Test(int x)
    {
        x.Should().BeInRange(1, 5);
        x.Should().NotBeInRange(6, 9);
    }
}
