using FluentAssertions;

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
        count.Should().Be(1);
        status.Should().Be(0);
        big.Should().Be(1);
    }
}
