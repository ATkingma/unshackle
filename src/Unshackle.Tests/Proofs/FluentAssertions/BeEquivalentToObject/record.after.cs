using Shouldly;

namespace Proofs;

public record Point(int X, int Y);

public class Case
{
    public void Test(Point a, Point b)
    {
        a.ShouldBeEquivalentTo(b);
    }
}
