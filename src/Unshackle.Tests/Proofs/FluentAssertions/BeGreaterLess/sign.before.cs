using System;
using FluentAssertions;

namespace Proofs;

public class SignCase
{
    public void Test(int x, double d)
    {
        x.Should().BePositive();
        d.Should().BeNegative("it dropped");
    }
}
