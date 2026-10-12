using Shouldly;
using System;
using System.Collections.Generic;

namespace Proofs;

public struct Point
{
    public int X;
    public int? Y { get; set; }
}

public class Case
{
    public void Test(List<Point> a, Point[] b)
    {
        a.ShouldBe(b);
    }
}
