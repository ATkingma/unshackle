using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int[] items)
    {
        items.Length.ShouldBe(3);
    }
}
