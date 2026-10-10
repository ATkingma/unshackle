using Shouldly;

namespace Proofs;

public class Because
{
    public void Test(int total)
    {
        total.ShouldBe(25, "two items of 12.50");
    }
}
