using System;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(float f, double a, double b)
    {
        f.Should().BeNegative("it dropped");
        (a - b).Should().BeNegative();
    }
}
