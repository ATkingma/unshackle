using FluentAssertions;

namespace Proofs;

public class BecauseArgs
{
    public void Test(int total, int items)
    {
        total.Should().Be(25, "{1} items", items);
    }
}
