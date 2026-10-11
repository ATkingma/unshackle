using FluentAssertions;

namespace Proofs;

public class Chain
{
    public void Test(int result)
    {
        result.Should().NotBe(41).And.BePositive();
    }
}
