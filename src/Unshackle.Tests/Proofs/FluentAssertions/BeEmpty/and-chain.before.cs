using FluentAssertions;

namespace Proofs;

public class AndChain
{
    public void Test(string text)
    {
        text.Should().NotBeNull().And.NotBeEmpty();
    }
}
