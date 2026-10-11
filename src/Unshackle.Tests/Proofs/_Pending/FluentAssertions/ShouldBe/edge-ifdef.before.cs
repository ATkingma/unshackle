using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int value)
    {
#if DEBUG
        value.Should().Be(1);
#else
        value.Should().Be(2);
#endif
    }
}
