global using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int value)
    {
        value.Should().Be(1);
    }
}
