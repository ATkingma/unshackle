using Shouldly;

namespace Proofs;

public class ConstantCast
{
    public void Test(byte count)
    {
        count.ShouldNotBe((byte)0);
    }
}
