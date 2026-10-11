using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int[] items)
    {
        items
            .Should()
            .HaveCount(3);
    }
}
