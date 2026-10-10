using FluentAssertions;

namespace Proofs;

public class BecauseArgs
{
    public void Test(string? text, int id)
    {
        text.Should().NotBeNull("missing {0}", id);
    }
}
