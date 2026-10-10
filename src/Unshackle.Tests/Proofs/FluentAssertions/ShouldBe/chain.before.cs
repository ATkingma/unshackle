using FluentAssertions;

namespace Proofs;

public class Chain
{
    public void Test(int result)
    {
        result.Should().Be(42).And.NotBe(41);
    }
}
