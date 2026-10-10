using Shouldly;

namespace Proofs;

public class ArrayCase
{
    public void Test(int[] x)
    {
        x.Length.ShouldBe(3);
    }
}
