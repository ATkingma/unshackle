using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int total)
    {
        total.ShouldBe(3, "three items were added");
    }
}
