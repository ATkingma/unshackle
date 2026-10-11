using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(double ratio)
    {
        ratio.Should().BeGreaterThan(0.5);
    }
}
