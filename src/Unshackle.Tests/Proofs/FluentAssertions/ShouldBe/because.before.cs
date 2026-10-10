using FluentAssertions;

namespace Proofs;

public class Because
{
    public void Test(int total)
    {
        total.Should().Be(25, "two items of 12.50");
    }
}
