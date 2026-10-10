using FluentAssertions;

namespace Proofs;

public class Chain
{
    public void Test(bool flag)
    {
        flag.Should().BeTrue().And.NotBe(false);
    }
}
