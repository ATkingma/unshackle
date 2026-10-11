using FluentAssertions;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int value)
    {
#if DEBUG
        value.Should().Be(1);
#else
        value.ShouldBe(2);
#endif
    }
}
