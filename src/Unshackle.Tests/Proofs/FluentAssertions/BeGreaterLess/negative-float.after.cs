using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(float f, double a, double b)
    {
        (f < 0).ShouldBeTrue("it dropped");
        ((a - b) < 0).ShouldBeTrue();
    }
}
