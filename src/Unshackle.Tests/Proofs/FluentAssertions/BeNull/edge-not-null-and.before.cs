using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(string? text)
    {
        text.Should().NotBeNull().And.Be("a");
    }
}
