using System;
using FluentAssertions;

namespace Proofs;

public class BecauseCase
{
    public void Test(int x)
    {
        x.Should().BeGreaterThan(1, "it grew");
        x.Should().BeInRange(1, 5, "it is small");
    }
}
