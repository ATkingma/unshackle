using FluentAssertions;

namespace Proofs;

public class BeTrue
{
    public void Test(bool flag)
    {
        flag.Should().BeTrue();
    }
}
