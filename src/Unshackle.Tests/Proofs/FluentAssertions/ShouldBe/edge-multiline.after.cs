using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int value)
    {
        value.ShouldBe(42);
    }
}
