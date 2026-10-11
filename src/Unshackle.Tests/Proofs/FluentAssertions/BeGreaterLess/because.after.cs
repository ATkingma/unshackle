using System;
using Shouldly;

namespace Proofs;

public class BecauseCase
{
    public void Test(int x)
    {
        x.ShouldBeGreaterThan(1, "it grew");
        x.ShouldBeInRange(1, 5, "it is small");
    }
}
