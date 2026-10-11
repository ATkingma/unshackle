using FluentAssertions;

namespace Proofs;

public class BecauseArgs
{
    public void Test(int total, int items, string why)
    {
        total.Should().Be(25, why, items);
    }
}
