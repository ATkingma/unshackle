using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public record struct Point(int X, int Y);

public class Case
{
    public void Test(Point[] x)
    {
        x.ShouldContain(new Point(1, 2));
    }
}
