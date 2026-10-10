using FluentAssertions;

namespace Proofs;

public class Because
{
    public void Test(string? text)
    {
        text.Should().BeNull("not set yet");
        text.Should().NotBeNull("must be set");
    }
}
