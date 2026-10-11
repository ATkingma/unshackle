using System;
using FluentAssertions;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(int x)
    {
        x.Should().BeGreaterThan(1, "it is {0}", 1);
    }
}
