using FluentAssertions;

namespace Proofs;

public class BecauseArgs
{
    public void Test(bool flag, int y)
    {
        flag.Should().BeTrue("x {0}", y);
    }
}
