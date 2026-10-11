using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class EdgeCase
{
    public void Test(int total)
    {
        Action act = () => total.Should().Be(3);
        act.Should().Throw<AssertionFailedException>();
    }
}
