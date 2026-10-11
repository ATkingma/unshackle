using FluentAssertions;

namespace Proofs;

public class String
{
    public void Test(string text)
    {
        text.Should().BeEmpty();
        text.Should().NotBeEmpty();
    }
}
