using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int total, int count)
    {
        total.Should().Be(3, $"{count} items were added");
    }
}
