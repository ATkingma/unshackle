using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public record struct Point(int X, int Y);

public class Case
{
    public void Test(Point[] x)
    {
        x.Should().ContainEquivalentOf(new Point(1, 2));
    }
}
