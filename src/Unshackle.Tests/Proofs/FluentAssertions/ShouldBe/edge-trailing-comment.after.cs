using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int value)
    {
        // the answer
        value.ShouldBe(42); // keep me
    }
}
