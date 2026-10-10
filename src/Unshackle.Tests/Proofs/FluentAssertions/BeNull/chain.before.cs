using FluentAssertions;

namespace Proofs;

public class Chain
{
    public void Test(string? text)
    {
        text.Should().NotBeNull().And.NotBeEmpty();
    }
}
