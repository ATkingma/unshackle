using FluentAssertions;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(int x, int y)
    {
        x.ShouldBe(1);
        y
            .Should()
#if NET8_0_OR_GREATER
            .BeLessThanOrEqualTo(10);
#else
            .BeLessOrEqualTo(10);
#endif
    }
}
