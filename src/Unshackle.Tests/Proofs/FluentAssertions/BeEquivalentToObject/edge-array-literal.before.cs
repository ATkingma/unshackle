using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int[] result)
    {
        result.Should().BeEquivalentTo(new[] { 1, 2 });
    }
}
