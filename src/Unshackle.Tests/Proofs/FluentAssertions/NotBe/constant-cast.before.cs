using FluentAssertions;

namespace Proofs;

public class ConstantCast
{
    public void Test(byte count)
    {
        count.Should().NotBe(0);
    }
}
