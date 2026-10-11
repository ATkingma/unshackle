using System;
using Shouldly;

namespace Proofs;

public class InRangeCase
{
    public void Test(int x)
    {
        x.ShouldBeInRange(1, 5);
        x.ShouldNotBeInRange(6, 9);
    }
}
