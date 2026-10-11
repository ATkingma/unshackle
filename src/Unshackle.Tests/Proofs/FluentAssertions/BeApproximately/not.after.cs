using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(double d, float f, decimal m, double a, double b)
    {
        System.Math.Abs(d - 1.5).ShouldBeGreaterThan(0.01);
        System.Math.Abs(f - 1.5f).ShouldBeGreaterThan(0.01f, "far");
        System.Math.Abs((a + b) - (a - b)).ShouldBeGreaterThan(0.1);
        System.Math.Abs(m - 25m).ShouldBeGreaterThan(0.01m);
    }
}
