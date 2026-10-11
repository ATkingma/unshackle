using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int[] result)
    {
        result.ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
    }
}
