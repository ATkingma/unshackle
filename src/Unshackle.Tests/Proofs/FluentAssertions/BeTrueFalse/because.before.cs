using FluentAssertions;

namespace Proofs;

public class Because
{
    public void Test(bool flag)
    {
        flag.Should().BeTrue("must be paid");
    }
}
