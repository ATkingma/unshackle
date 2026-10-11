using FluentAssertions;

namespace Proofs;

public class Chain
{
    public void Test(string text)
    {
        text.Should().NotBeEmpty().And.StartWith("a");
    }
}
