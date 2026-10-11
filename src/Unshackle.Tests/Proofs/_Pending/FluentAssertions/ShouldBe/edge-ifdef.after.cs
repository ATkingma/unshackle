using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(int value)
    {
#if DEBUG
        value.ShouldBe(1);
#else
        value.Should().Be(2);
#endif
    }
}
