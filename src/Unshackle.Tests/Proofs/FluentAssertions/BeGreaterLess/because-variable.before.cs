using System;
using FluentAssertions;

namespace Proofs;

public class BecauseVariableCase
{
    public void Test(int x, string why)
    {
        x.Should().BeGreaterThan(1, why, 1);
    }
}
