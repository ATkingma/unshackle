using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int total, int count)
    {
        total.ShouldBe(3, $"{count} items were added");
    }
}
