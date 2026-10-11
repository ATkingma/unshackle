using FluentAssertions;

namespace Proofs;

public class BecauseArgs
{
    public void Test(string text)
    {
        text.Should().BeEmpty("because {0}", 1);
    }
}
