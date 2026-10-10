using Shouldly;

namespace Proofs;

public enum Status
{
    Placed,
    Cancelled,
}

public class ConstantCast
{
    public void Test(byte count, Status status, long big)
    {
        count.ShouldBe((byte)1);
        status.ShouldBe((Status)0);
        big.ShouldBe(1);
    }
}
