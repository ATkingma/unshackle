using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int value)
    {
        // the answer
        value.Should().Be(42); // keep me
    }
}
